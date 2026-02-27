
namespace GjettLataBackend.Models;

public class Game
{
    public GameMode GameMode { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public bool Ended { get; set; }
    public int CurrentRoundIndex { get; set; }
    public int TotalRounds { get; set; }
    public List<Round> Rounds { get; set; } = new();

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
        Rounds = new List<Round>();
        TotalRounds = 15;
    }
}