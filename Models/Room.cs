
namespace GjettLataBackend.Models;

public class Room
{
    public string Id { get; set; }

    public List<Player> Players { get; set; } = new();
    public int MaxPlayers { get; set; }
    public Player? Host { get; set; }

    public Game? CurrentGame { get; set; }

    public List<ChatMessage> ChatHistory { get; set; } = new();
    public DateTimeOffset LastActivity { get; set; }
}