using System.Drawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NanoidDotNet;
using GjettLataBackend.Models;
using GjettLataBackend.Models.DTO;
using GjettLataBackend.Services;
using Color = GjettLataBackend.Models.Color;

[ApiController]
[Route("[controller]")]
public class RoomController : ControllerBase
{
    private readonly RoomManager _roomManager;
    private readonly IHubContext<RoomHub> _hub;
    private readonly SpotifyService _spotifyService;
    private readonly DeezerService _deezerService;

    public RoomController(RoomManager roomManager, IHubContext<RoomHub> hub,  SpotifyService spotifyService,  DeezerService deezerService)
    {
        _roomManager = roomManager;
        _hub = hub;
        _spotifyService = spotifyService;
        _deezerService = deezerService;
    }
    
    [HttpGet("create")]
    public async Task<IActionResult> CreateRoom()
    {
        var roomId = await Nanoid.GenerateAsync("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890", 5);
        var room = _roomManager.CreateRoom(roomId);

        return Ok(room.ToSafeForClient());
    }
    
    [HttpGet("{id}")]
    public IActionResult GetRoom(string id)
    {
        var room = _roomManager.GetRoom(id);
        return room == null ? NotFound() : Ok(room.ToSafeForClient());
    }
    
    [HttpPost("{roomId}/join")]
    public async Task<IActionResult> JoinRoom(string roomId, [FromBody] PlayerDto player)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound();

        var newPlayer = new Player { Id = Guid.NewGuid().ToString(), Name = player.Name, Color = player.Color };
        if (room.Players.Count == 0)
        {
            room.Host = newPlayer;
        }
        room.Players.Add(newPlayer);

        var systemMessage = new ChatMessage($"{newPlayer.Name} har blitt med!");
        await _hub.Clients.Group(roomId).SendAsync("ReceiveChat", systemMessage.Sender, systemMessage.Message, systemMessage.IsSystemMessage);

        await _hub.Clients.Group(roomId).SendAsync("PlayerJoined", newPlayer); // Notify others

        return Ok(new { newPlayer, Room = room.ToSafeForClient() });
    }
    
    [HttpPost("{roomId}/gamemode")]
    public async Task<IActionResult> SetGamemode(string roomId, [FromBody] GameMode gameMode)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound();

        room.GameMode = gameMode;

        return Ok();
    }
    
    [HttpGet("{roomId}/start")]
    public async Task<IActionResult> StartGame(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound($"Room {roomId} not found");
        
        // Get songs
        _ = Task.Run(async () =>
        {
            try
            {
                // var tracks = await _spotifyService
                //     .GetRandomTracksFromPlaylist(
                //         PlaylistMap.GetPlaylistId(room.GameMode),
                //         1012);

                var songs = await _deezerService.GetRandomSongs(room.GameMode.ToString(), room.MaxRounds);

                room.Songs.AddRange(songs);
                
                await _hub.Clients.Group(roomId).SendAsync("SongUpdate", songs[room.CurrentRound].ToSafeForClient());

            }
            catch (Exception ex)
            {
                Console.WriteLine("Background song loading failed: " + ex);
            }
        });

        var countdownMessages = new[] { "Starter om 3...", "Starter om 2...", "Starter om 1..." };

        foreach (var msg in countdownMessages)
        {
            var countdownMessage = new ChatMessage(msg);

            room.ChatHistory.Add(countdownMessage);

            await _hub.Clients.Group(roomId)
                .SendAsync("ReceiveChat", countdownMessage.Sender, countdownMessage.Message, countdownMessage.IsSystemMessage);

            await Task.Delay(1000); // 1 second between messages
        }

        room.GameStarted = DateTimeOffset.Now;

        var startMessage = new ChatMessage("Spillet har startet!");

        room.ChatHistory.Add(startMessage);

        await _hub.Clients.Group(roomId).SendAsync("RoomUpdate", room.ToSafeForClient());

        await _hub.Clients.Group(roomId)
            .SendAsync("ReceiveChat", startMessage.Sender, startMessage.Message, startMessage.IsSystemMessage);

        return Ok(room.ToSafeForClient());
    }
    
    [HttpGet("{roomId}/restart")]
    public async Task<IActionResult> RestartGame(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound($"Room {roomId} not found");

        // Reset game
        room.GameStarted = null;
        room.GameEnded = false;


        var systemMessage = new ChatMessage("Lager nytt spill...");

        room.ChatHistory.Add(systemMessage);

        await _hub.Clients.Group(roomId).SendAsync("RoomUpdate", room.ToSafeForClient());
        await _hub.Clients.Group(roomId).SendAsync("ReceiveChat", systemMessage.Sender, systemMessage.Message, systemMessage.IsSystemMessage);

        return Ok(room.ToSafeForClient());
    }
    
    [HttpGet("{roomId}/end")]
    public async Task<IActionResult> EndGame(string roomId)
    {   
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound($"Room {roomId} not found");

        room.GameEnded = true;

        var leadingPlayer = room.Players.MaxBy(p => p.Score);

        string durationText = "";
        if (room.GameStarted != null)
        {
            var duration = DateTimeOffset.UtcNow - room.GameStarted.Value;
            durationText = $" after {FormatDuration(duration)}";
        }

        var systemMessage = new ChatMessage(leadingPlayer == null
            ? $"Det ble uavgjort{durationText}!"
            : $"Spillet er slutt! {leadingPlayer} vant{durationText}!");

        await _hub.Clients.Group(roomId).SendAsync("RoomUpdate", room.ToSafeForClient());

        room.ChatHistory.Add(systemMessage);

        await _hub.Clients.Group(roomId)
            .SendAsync("ReceiveChat", systemMessage.Sender, systemMessage.Message, systemMessage.IsSystemMessage);

        return Ok();
    }
    
    [HttpGet("{roomId}/chat")]
    public IActionResult GetChatHistory(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return NotFound();

        return Ok(room.ChatHistory.Select(msg => new {
            sender = new {
                name = msg.Sender.Name,
                color = msg.Sender.Color
            },
            message = msg.Message,
            isSystemMessage = msg.IsSystemMessage,
        }));
    }
    
    string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours} hour{(duration.TotalHours >= 2 ? "s" : "")}";
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes} minute{(duration.TotalMinutes >= 2 ? "s" : "")}";
        return $"{(int)duration.TotalSeconds} second{(duration.TotalSeconds >= 2 ? "s" : "")}";
    }
}