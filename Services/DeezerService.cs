using System.Text.Json;
using System.Net.Http.Json;
using GjettLataBackend.Models;

using System.Text.Json;
using System.Net.Http.Json;

public class DeezerService
{
    private readonly HttpClient _http;

    // Genre -> list of IDs
    private readonly Dictionary<string, List<long>> _songCache;

    // DeezerId -> Track metadata (cached)
    private readonly Dictionary<long, DeezerTrackResponse> _trackCache = new();

    private readonly SemaphoreSlim _lock = new(1, 1);

    public DeezerService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.deezer.com/");

        var json = File.ReadAllText("songCache.json");

        _songCache = JsonSerializer.Deserialize<
                         Dictionary<string, List<long>>>(json)
                     ?? new();
    }

    // ============================
    // PUBLIC API
    // ============================

    public async Task<List<Song>> GetRandomSongs(string? genre, int count)
    {
        if (genre == null ||
            !_songCache.TryGetValue(genre, out var ids) ||
            ids.Count == 0)
        {
            return new();
        }

        var selectedIds = ids
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToList();

        var songs = new List<Song>();

        foreach (var id in selectedIds)
        {
            var track = await GetTrack(id);

            if (track == null)
                continue;

            songs.Add(new Song
            {
                Name = track.title_short ?? track.title,
                DeezerId = id
            });
        }

        return songs;
    }

    public async Task<string?> GetPreviewUrlById(long id)
    {
        var track = await GetTrack(id);
        return track?.preview;
    }

    // ============================
    // INTERNAL CACHE LOGIC
    // ============================

    private async Task<DeezerTrackResponse?> GetTrack(long id)
    {
        if (_trackCache.TryGetValue(id, out var cached))
            return cached;

        await _lock.WaitAsync();

        try
        {
            // Double-check after lock
            if (_trackCache.TryGetValue(id, out cached))
                return cached;

            var track =
                await _http.GetFromJsonAsync<DeezerTrackResponse>(
                    $"track/{id}");

            if (track == null || string.IsNullOrEmpty(track.preview))
                return null;

            _trackCache[id] = track;

            return track;
        }
        finally
        {
            _lock.Release();
        }
    }
}