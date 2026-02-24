namespace GjettLataBackend.Models.DTO;

public class SongDto
{
    public string SensoredName { get; set; } 
    public DateTimeOffset StartAt { get; set; }
    public string PreviewUrl { get; set; }
}