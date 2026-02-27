namespace GjettLataBackend.Models;

public class RoomDto
{
    public string Id { get; set; }
    public List<Player> Players { get; set; }
    public Player? Host { get; set; }
    public GameDto? Game { get; set; }
}

public class GameDto
{
    public GameMode GameMode { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public bool Ended { get; set; }
    public int CurrentRoundIndex { get; set; }
    public RoundDto? CurrentRound { get; set; }
    public int TotalRounds { get; set; }
}

public class RoundDto
{
    public RoundState State { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset? IntermissionEndsAt { get; set; }
    public string MaskedName { get; set; }
    public Dictionary<string, int> PlayerScores { get; set; }
    public bool IsFullyRevealed { get; set; }
}

public class PlayerDto
{
    public string Name { get; set; }
    public string Color { get; set; }
}

public class SongDto
{
    public string DisplayName { get; set; } = "";
    public string? PreviewUrl { get; set; }
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}