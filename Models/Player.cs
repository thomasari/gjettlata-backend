namespace GjettLataBackend.Models;

public class Player
{
    public static readonly Player System = new Player
    {
        Id = "SYSTEM",
        Name = "",
        Color = "#b0b0b0",
        Score = 0
    };
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Color { get; set; }
    public int Score { get; set; }
}
