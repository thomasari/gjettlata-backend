namespace GjettLataBackend.Models;

public class Song
{
    public string Name { get; set; } 
    public List<string> Artists { get; set; }
    public string Url { get; set; }
    public int StartAt { get; set; }
}