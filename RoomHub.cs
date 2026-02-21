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
}