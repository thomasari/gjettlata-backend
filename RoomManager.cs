

using System.Collections.Concurrent;
using GjettLataBackend.Models;
using NanoidDotNet;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();
    private readonly TimeSpan _roomTimeout = TimeSpan.FromMinutes(60);

    public Room CreateRoom()
    {
        var id = Nanoid.Generate("BCDFGHJKLMNPQRSTVWXZ123456789", 5);

        var room = new Room
        {
            Id = id,
            Players = new List<Player>(),
            MaxPlayers = 16,
            Host = null,
            CurrentGame = new Game(),
            ChatHistory = new List<ChatMessage>(),
            LastActivity = DateTimeOffset.UtcNow
        };

        _rooms[id] = room;
        return room;
    }

    public Room? GetRoom(string id)
    {
        if (_rooms.TryGetValue(id, out var room))
        {
            room.LastActivity = DateTimeOffset.UtcNow;
            return room;
        }

        return null;
    }

    public void RemoveRoom(string id)
    {
        _rooms.TryRemove(id, out _);
    }

    public void RemoveEmptyRoom(string id)
    {
        if (_rooms.TryGetValue(id, out var room) && room.Players.Count == 0)
        {
            _rooms.TryRemove(id, out _);
        }
    }

    public void CleanupInactiveRooms()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var kv in _rooms)
        {
            if (now - kv.Value.LastActivity > _roomTimeout)
            {
                _rooms.TryRemove(kv.Key, out _);
            }
        }
    }
}