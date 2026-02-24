using GjettLataBackend.Models.DTO;

namespace GjettLataBackend.Models;

public class Song
{
    public string Name { get; set; } 
    public List<string> Artists { get; set; }
    public long DeezerId { get; set; }
    public string PreviewUrl { get; set; }

    public SongDto ToSafeForClient()
    {
        return new SongDto
        {
            SensoredName = SensorName(),
            StartAt = DateTime.Now.AddSeconds(3),
            PreviewUrl = PreviewUrl
        };
    }
    
    

    private string SensorName()
    {
        if (string.IsNullOrEmpty(Name))
            return string.Empty;

        return new string(Name
            .Select(c => char.IsLetterOrDigit(c) ? '_' : c)
            .ToArray());
    }
}