
namespace GjettLataBackend.Models;

public class Game
{
    public GameMode GameMode { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public bool Ended { get; set; }
    public int CurrentRoundIndex { get; set; }
    public List<Round> Rounds { get; set; } = new(10);

    public Round? CurrentRound =>
        CurrentRoundIndex < Rounds.Count
            ? Rounds[CurrentRoundIndex]
            : null;

    public Game()
    {
        GameMode = GameMode.AllTime;
        StartedAt = null;
        Ended = false;
        CurrentRoundIndex = 0;
        Rounds = new List<Round>(10);
    }
}