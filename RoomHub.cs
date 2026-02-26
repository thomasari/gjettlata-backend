using GjettLataBackend.Models;
using GjettLataBackend.Services;
using Microsoft.AspNetCore.SignalR;

namespace GjettLataBackend;

public class RoomHub : Hub
{
    private readonly RoomManager _roomManager;
    private readonly GameEngineService _engine;

    public RoomHub(RoomManager manager, GameEngineService engine)
    {
        _roomManager = manager;
        _engine = engine;
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

    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        var room = _roomManager.GetRoom(roomId);
        if (room != null)
        {
            await Clients.Caller.SendAsync("RoomUpdate",
                _engine.ToDto(room));
        }
    }

    public async Task SendGuess(string roomId, string playerId, string guess)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return;

        await _engine.ProcessGuess(room, roomId, playerId, guess, Context.ConnectionId);
    }
    
    public async Task SendChat(string roomId, string playerId, string message)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return;

        var player = room.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null) return;

        var chatMessage = new ChatMessage
        {
            Sender = player,
            Message = message,
            IsSystemMessage = false
        };
        
        room.ChatHistory.Add(chatMessage);

        await Clients.Group(roomId)
            .SendAsync("ReceiveChat", player, message, false);
    }
}