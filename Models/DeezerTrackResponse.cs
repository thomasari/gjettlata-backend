namespace GjettLataBackend.Models;

public class DeezerTrackResponse
{
    public long id { get; set; }

    public string title { get; set; }
    public string title_short { get; set; }

    public int duration { get; set; }   // FIXED (was string)
    public int rank { get; set; }       // FIXED (was string)

    public string preview { get; set; }

    public DeezerArtist artist { get; set; }
}

public class DeezerArtist
{
    public long id { get; set; }
    public string name { get; set; }
}