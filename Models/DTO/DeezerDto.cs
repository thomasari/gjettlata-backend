namespace GjettLataBackend.Models.DTO;

    public class Artist
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class DeezerTrackResponse
    {
        public long id { get; set; }
        public string title { get; set; }
        public string title_short { get; set; }
        public string title_version { get; set; }
        public int duration { get; set; }
        public int rank { get; set; }
        public string release_date { get; set; }
        public string preview { get; set; }
        public Artist artist { get; set; }
    }

