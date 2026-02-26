using FuzzySharp;
using GjettLataBackend.Models;
using Microsoft.AspNetCore.SignalR;

namespace GjettLataBackend.Services;

using Microsoft.AspNetCore.SignalR;

public class GameEngineService
{
    private readonly IHubContext<RoomHub> _hub;
    private readonly DeezerService _deezer;

    public GameEngineService(IHubContext<RoomHub> hub,
                             DeezerService deezer)
    {
        _hub = hub;
        _deezer = deezer;
    }

    public async Task StartGame(Room room, string roomId, int rounds)
    {
        var countdown = RunCountdown(room, roomId);
        
        var songs = await _deezer.GetRandomSongs(
            room.CurrentGame!.GameMode.ToString(), rounds);

        await countdown;
        
        room.CurrentGame.StartedAt = DateTimeOffset.UtcNow;
        room.CurrentGame.Rounds = songs.Select(s => new Round
        {
            Song = s,
            State = RoundState.Countdown
        }).ToList();

        await StartRound(room, roomId);
    }

    private async Task StartRound(Room room, string roomId)
    {
        var round = room.CurrentGame!.CurrentRound!;
        round.StartedAt = DateTimeOffset.UtcNow.AddSeconds(3);
        round.EndsAt = round.StartedAt.Value.AddSeconds(30);
        round.State = RoundState.Playing;

        var preview = await _deezer.GetPreviewUrlById(round.Song.DeezerId);

        await _hub.Clients.Group(roomId)
            .SendAsync("SongStarted", new SongDto
            {
                DisplayName = BuildMaskedName(round),
                PreviewUrl = preview,
                StartAt = round.StartedAt,
                EndsAt = round.EndsAt
            });

        await BroadcastState(room, roomId);

        _ = RunRoundLoop(room, roomId);
    }
    
    private async Task RunCountdown(Room room, string roomId)
    {
        var messages = new[]
        {
            "Spillet starter om 3...",
            "Spillet starter om 2...",
            "Spillet starter om 1..."
        };

        foreach (var msg in messages)
        {
            var chat = new ChatMessage(msg);
            room.ChatHistory.Add(chat);

            await _hub.Clients.Group(roomId)
                .SendAsync("ReceiveChat", chat.Sender, chat.Message, chat.IsSystemMessage);

            await Task.Delay(1000);
        }
    }

    private async Task RunRoundLoop(Room room, string roomId)
    {
        var round = room.CurrentGame!.CurrentRound!;

        while (DateTimeOffset.UtcNow < round.EndsAt &&
               round.State == RoundState.Playing)
        {
            await Task.Delay(5000);

            if (round.State != RoundState.Playing)
                return;

            RevealLetter(round);
            await BroadcastState(room, roomId);
        }

// Only timeout ends round
        if (round.State == RoundState.Playing)
        {
            await EndRound(room, roomId);
        }
    }

    private void RevealLetter(Round round)
    {
        if (round.IsFullyRevealed) return;

        var name = round.Song.Name;

        var hidden = Enumerable.Range(0, name.Length)
            .Where(i =>
                char.IsLetterOrDigit(name[i]) &&
                !round.RevealedIndexes.Contains(i))
            .ToList();

        if (!hidden.Any()) return;

        var index = hidden[Random.Shared.Next(hidden.Count)];
        round.RevealedIndexes.Add(index);
    }

    private async Task EndRound(Room room, string roomId)
    {
        var round = room.CurrentGame!.CurrentRound!;

        round.State = RoundState.Intermission;
        round.IsFullyRevealed = true;
        round.IntermissionEndsAt = DateTimeOffset.UtcNow.AddSeconds(5);

        // Broadcast full reveal state
        await BroadcastState(room, roomId);

        // Notify clients explicitly
        await _hub.Clients.Group(roomId)
            .SendAsync("RoundEnded", new
            {
                correctAnswer = round.Song.Name,
                intermissionEndsAt = round.IntermissionEndsAt
            });

        _ = RunIntermission(room, roomId);
    }
    
    private async Task RunIntermission(Room room, string roomId)
    {
        var messages = new[]
        {
            $"Sangen var {room.CurrentGame.CurrentRound.Song.Name}!",
            "Neste runde starter om 5...",
            "4...",
            "3...",
            "2...",
            "1..."
        };

        foreach (var msg in messages)
        {
            var chat = new ChatMessage(msg);
            room.ChatHistory.Add(chat);

            await _hub.Clients.Group(roomId)
                .SendAsync("ReceiveChat", chat.Sender, chat.Message, true);

            await Task.Delay(1000);
        }

        room.CurrentGame!.CurrentRoundIndex++;

        if (room.CurrentGame.CurrentRoundIndex >= room.CurrentGame.Rounds.Count)
        {
            room.CurrentGame.Ended = true;

            await _hub.Clients.Group(roomId)
                .SendAsync("GameEnded", ToDto(room));

            return;
        }

        await StartRound(room, roomId);
    }

    public async Task ProcessGuess(Room room,
        string roomId,
        string playerId,
        string guess,
        string connectionId
        )
    {
        var round = room.CurrentGame?.CurrentRound;

        if (round is not { State: RoundState.Playing })
            return;

        if (round.PlayerScores.ContainsKey(playerId))
            return; // already scored
        
        var guessScore = CalculateGuessScore(guess, round.Song.Name);
        var player = room.Players.First(p => p.Id == playerId);
        
        
        Console.WriteLine(guess);
        Console.WriteLine(round.Song.Name);
        Console.WriteLine(guessScore);

        if (guessScore > 93) // Correct
        {
            var score = CalculateScore(round);

            round.PlayerScores[playerId] = score;

            player.Score += score;
            
            var correctGuessMessage = new ChatMessage($"{player.Score} gjettet riktig!", player.Color);
            room.ChatHistory.Add(correctGuessMessage);
            
            await _hub.Clients.Group(roomId)
                .SendAsync(
                    "ReceiveChat",
                    correctGuessMessage.Sender,
                    correctGuessMessage.Message,
                    correctGuessMessage.IsSystemMessage
                );
            
            await BroadcastState(room, roomId);

            if (round.PlayerScores.Count != room.Players.Count ||
                round.State != RoundState.Playing) return;
            round.State = RoundState.Intermission;
            await EndRound(room, roomId);

            return;
        }

        if (guessScore > 70)
        {
            var msg = new ChatMessage($"{guess} er nesten riktig!");
            
            await _hub.Clients.Client(connectionId)
                .SendAsync("ReceiveChat",
                    msg.Sender,
                    msg.Message,
                    msg.IsSystemMessage);

            return;
        }

        var guessMessage = new ChatMessage
        {
            Sender = player,
            Message = guess,
            IsSystemMessage = false
        };
            
        await _hub.Clients.Group(roomId)
            .SendAsync(
                "ReceiveChat",
                guessMessage.Sender,
                guessMessage.Message,
                guessMessage.IsSystemMessage
            );
        room.ChatHistory.Add(guessMessage);
    }
    
    private int CalculateScore(Round round)
    {
        var now = DateTimeOffset.UtcNow;

        var totalSeconds = 30.0;
        var remainingSeconds =
            Math.Max(0, (round.EndsAt!.Value - now).TotalSeconds);

        var timeFactor = remainingSeconds / totalSeconds;

        var totalLetters = round.Song.Name.Count(char.IsLetterOrDigit);
        var revealed = round.RevealedIndexes.Count;

        var revealFactor = 1.0 - ((double)revealed / totalLetters);

        var rawScore = 100 * timeFactor * revealFactor;

        return Math.Max(10, (int)Math.Round(rawScore));
    }

    private int CalculateGuessScore(string guess, string answer)
    {
        var guessNorm = Normalize(guess);
        var answerNorm = Normalize(answer);
        var score = new[]
        {
            Fuzz.TokenSetRatio(guessNorm, answerNorm),
            Fuzz.PartialRatio(guessNorm, answerNorm)
        }.Max();
        
        return score;
    }

    private string Normalize(string s)
    {
        return new string(s
            .ToLower()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private async Task BroadcastState(Room room, string roomId)
    {
        await _hub.Clients.Group(roomId)
            .SendAsync("RoomUpdate", ToDto(room));
    }
    
    private string BuildMaskedName(Round round)
    {
        if (round.IsFullyRevealed)
            return round.Song.Name;

        var name = round.Song.Name;
        var chars = name.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsLetterOrDigit(chars[i]) &&
                !round.RevealedIndexes.Contains(i))
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }

    public RoomDto ToDto(Room room)
    {
        return new RoomDto
        {
            Id = room.Id,
            Players = room.Players,
            Host = room.Host,
            Game = room.CurrentGame == null ? null : new GameDto
            {
                GameMode = room.CurrentGame.GameMode,
                StartedAt = room.CurrentGame.StartedAt,
                Ended = room.CurrentGame.Ended,
                CurrentRoundIndex = room.CurrentGame.CurrentRoundIndex,
                CurrentRound = room.CurrentGame.CurrentRound == null
                    ? null
                    : new RoundDto
                    {
                        State = room.CurrentGame.CurrentRound.State,
                        StartedAt = room.CurrentGame.CurrentRound.StartedAt,
                        EndsAt = room.CurrentGame.CurrentRound.EndsAt,
                        IntermissionEndsAt =
                            room.CurrentGame.CurrentRound.IntermissionEndsAt,
                        MaskedName =
                                BuildMaskedName(
                                    room.CurrentGame.CurrentRound),
                        PlayerScores = room.CurrentGame.CurrentRound.PlayerScores,
                    }
            }
        };
    }
}