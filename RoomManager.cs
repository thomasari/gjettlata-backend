

using GjettLataBackend.Models;

public class RoomManager
{
    private readonly Dictionary<string, Room> _rooms = new();

    public Room CreateRoom(string id)
    {
        var room = new Room(id);
        _rooms[room.Id] = room;
        return room;
    }    
    
    public Player CreatePlayer()
    {
        var player = new Player();
        return player;
    }

    public bool UpdatePlayerName(string roomId, Player player)
    {
        var room = GetRoom(roomId);

        var existing = room?.Players.FirstOrDefault(p => p.Id == player.Id);
        if (existing == null) return false;

        existing.Name = player.Name;
        return true;
    }

    public Room? GetRoom(string id) => _rooms.TryGetValue(id, out var room) ? room : null;

    public void DeleteRoom(string roomId) => _rooms.Remove(roomId);
    
    public Player? AddPlayer(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            var newPlayer = CreatePlayer();
            if (!room.Players.Contains(newPlayer))
                room.Players.Add(newPlayer);
            return newPlayer;
        }
        return null;
    }

    public IEnumerable<Room> GetAllRooms() => _rooms.Values;
}