namespace GjettLataBackend.Models;

public enum RoundState
{
    Countdown,
    Playing,
    Intermission,
    Ended
}

public class Round
{
    public Song Song { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset? IntermissionEndsAt { get; set; }
    public bool IsFullyRevealed { get; set; }
    public RoundState State { get; set; }

    public Dictionary<string, int> PlayerScores { get; set; } = new();
    public HashSet<int> RevealedIndexes { get; set; } = new();
}