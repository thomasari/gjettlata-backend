namespace GjettLataBackend.Models;

public class Room(string id)
{
    public string Id { get; set; } = id;
    public List<Player> Players { get; set; } = [];
    public Player? Host { get; set; }
    public List<Song> Songs { get; set; } = [];
    
    public List<ChatMessage> ChatHistory { get; set; } = new(); // <-- new
    public DateTimeOffset? GameStarted { get; set; }
    public bool GameEnded { get; set; } = false;
    public GameMode GameMode { get; set; } = GameMode.AllTime;
    public int CurrentRound { get; set; } = 0;
    public int MaxRounds { get; set; } = 16;
}