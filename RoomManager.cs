

using GjettLataBackend.Models;
using NanoidDotNet;

public class RoomManager
{
    private readonly Dictionary<string, Room> _rooms = new();

    public Room CreateRoom()
    {
        var id = Nanoid.Generate(
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890",
            5);
        
        var room = new Room
        {
            Id = id,
            Players = new List<Player>(),
            Host = null,
            CurrentGame = new Game(),
            ChatHistory = new List<ChatMessage>()
        };
        
        _rooms[id] = room;
        return room;
    }

    public Room? GetRoom(string id)
    {
        return _rooms.TryGetValue(id, out var room) ? room : null;
    }
}