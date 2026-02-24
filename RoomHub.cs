using System.Text.RegularExpressions;
using FuzzySharp;
using GjettLataBackend.Models;
using Microsoft.AspNetCore.SignalR;

public class RoomHub : Hub
{
    private readonly RoomManager _roomManager;

    public RoomHub(RoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public async Task SendChat(string roomId, string playerId, string message)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room != null)
        {
            var player = room.Players.Find(p => p.Id == playerId);
            if (player != null)
            {
                room.ChatHistory.Add(new ChatMessage { Sender = player, Message = message });
                await Clients.Group(roomId).SendAsync("ReceiveChat", player, message, false);
            }
        }
    }
    
    public async Task SendGuess(string roomId, string playerId, string message)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return;

        var player = room.Players.Find(p => p.Id == playerId);
        if (player == null) return;

        var guessNorm = Normalize(message);
        var answerNorm = Normalize(room.Songs[room.CurrentRound].Name);
        
        Console.WriteLine(guessNorm);
        Console.WriteLine(answerNorm);

        var score = Fuzz.TokenSetRatio(guessNorm, answerNorm);
        Console.WriteLine(score);
        Console.WriteLine("\n");

        if (score >= 91)
        {
            player.Score++;
            await Clients.Group(roomId).SendAsync("CorrectGuess", player.Id, player.Score);
            await Clients.Group(roomId)
                .SendAsync(
                    "ReceiveChat",
                    new Player
                    {
                        Id = Player.System.Id,
                        Name = Player.System.Name,
                        Score = Player.System.Score,
                        Color = player.Color
                    },
                    $"{player.Name} gjettet riktig!",
                    true
                );
            return;
        }
        if (score >= 70) // tolerance threshold
        {
            await Clients.Client(Context.ConnectionId)
                .SendAsync("ReceiveChat", Player.System, $"\"{message}\" er nesten riktig!", true);
            return;
        }

        await Clients.Group(roomId)
            .SendAsync("ReceiveChat", player, message, false);
    }
    
    public async Task SendPlayerUpdate(string roomId, Player? playerUpdate)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return;
        
        if(playerUpdate != null)
        {
            _roomManager.UpdatePlayerName(roomId, playerUpdate);
            await Clients.Group(roomId).SendAsync("PlayerUpdate", playerUpdate);
        }
    }
    
    public override async Task OnConnectedAsync()
    {
        var roomId = Context.GetHttpContext()?.Request.Query["roomId"];
        if (!string.IsNullOrEmpty(roomId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }

        await base.OnConnectedAsync();
    }

    private static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Lowercase
        input = input.ToLowerInvariant();

        // Remove punctuation (keep letters, digits, spaces)
        input = Regex.Replace(input, @"[^\p{L}\p{Nd}\s]", "");

        // Collapse multiple spaces
        input = Regex.Replace(input, @"\s+", " ").Trim();

        return input;
    }
}