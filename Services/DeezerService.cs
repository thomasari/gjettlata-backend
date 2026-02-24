using System.Text.Json;
using System.Net.Http.Json;
using GjettLataBackend.Models;
using GjettLataBackend.Models.DTO;

public class DeezerService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, List<long>> _songCache;

    public DeezerService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.deezer.com/");

        var json = File.ReadAllText("songCache.json");
        _songCache = JsonSerializer.Deserialize<
                         Dictionary<string, List<long>>>(json)
                     ?? new();
    }

    public async Task<List<Song>> GetRandomSongs(string genre, int count)
    {
        if (!_songCache.TryGetValue(genre, out var ids) || ids.Count == 0)
            return new List<Song>();

        var selectedIds = ids
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToList();

        var tasks = selectedIds.Select(GetSongById);
        var results = await Task.WhenAll(tasks);

        return results
            .Where(s => s != null)
            .ToList()!;
    }

    private async Task<Song?> GetSongById(long id)
    {
        var track = await _http.GetFromJsonAsync<DeezerTrackResponse>($"track/{id}");

        if (track == null || string.IsNullOrEmpty(track.preview))
            return null;

        return new Song
        {
            Name = track.title_short,
            Artists = new() { track.artist.name },
            DeezerId = id,
            PreviewUrl = track.preview,
        };
    }
}