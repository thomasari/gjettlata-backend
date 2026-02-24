
namespace GjettLataBackend.Models.DTO;

public class RoomDto
{
    public string Id { get; set; }
    public List<Player> Players { get; set; } = [];
    public Player? Host { get; set; }
    public GameMode GameMode { get; set; }
    public int CurrentRound { get; set; }
    public int MaxRounds { get; set; }
    public bool GameEnded { get; set; }
    public DateTimeOffset? GameStarted { get; set; }
}