namespace GjettLataBackend.Models.DTO;

public class DeezerSearchResponse
{
    public List<DeezerTrack> Data { get; set; } = [];
}

public class DeezerTrack
{
    public string Title { get; set; } = "";
    public string Preview { get; set; } = "";
    public DeezerArtist Artist { get; set; } = new();
}

public class DeezerArtist
{
    public string Name { get; set; } = "";
}