using System.Text.Json;
using System.Net.Http.Json;
using GjettLataBackend.Models;

using System.Text.Json;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

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

    public async Task<List<Song>> GetRandomSongs(GameMode mode, int count)
    {

        List<long> ids;
        var songs = new List<Song>();
        
        switch (mode)
        {
            case GameMode.Seventies:
            case GameMode.Eighties:
            case GameMode.Nineties:
            case GameMode.TwoThousands:
            case GameMode.TwentyTens:
            case GameMode.TwentyTwenties:
            case GameMode.AllTime:
                if (!_songCache.TryGetValue(mode.ToString(), out ids) ||
                    ids.Count == 0)
                {
                    return new();
                }
                var selectedIds = ids
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(count)
                    .ToList();


                foreach (var id in selectedIds)
                {
                    var track = await GetTrack(id);

                    if (track == null)
                        continue;

                    songs.Add(new Song
                    {
                        Name = CleanTitle(track.title_short ?? track.title),
                        ArtistName = track.artist.name,
                        DeezerId = id
                    });
                }
                break;
            case GameMode.TopNorway:
            {
                var id = GameModeExtensions.GetPlaylistIdForGamemode(mode);
                var tracks = await GetPlaylist(id);

                songs = tracks
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(count)
                    .Select(t => new Song
                    {
                        Name = CleanTitle(t.title_short ?? t.title),
                        ArtistName = t.artist.name,
                        DeezerId = t.id
                    })
                    .ToList();

                break;
            }

            case GameMode.TopWorld:
            {
                var tracks = await GetPlaylist("3155776842");

                songs = tracks
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(count)
                    .Select(t => new Song
                    {
                        Name = CleanTitle(t.title_short ?? t.title),
                        ArtistName = t.artist.name,
                        DeezerId = t.id
                    })
                    .ToList();

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
        


        return songs;
    }

    public async Task<List<DeezerTrackResponse>> GetPlaylist(string id)
    {
        var response = await _http.GetFromJsonAsync<DeezerPlaylistTracksResponse>(
            $"playlist/{id}/tracks?limit=100");

        if (response == null)
            return new();

        return response.data
            .Where(t => !string.IsNullOrEmpty(t.preview))
            .ToList();
    }
    
    private static string CleanTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return title;

        title = Regex.Replace(title, @"\s*[\(\[].*?[\)\]]", "");

        title = Regex.Replace(title,
            @"\s*-\s*.*\b(live|remaster(ed)?|acoustic|version)\b.*$",
            "", RegexOptions.IgnoreCase);

        title = Regex.Replace(title,
            @"\s*\b(feat\.?|ft\.?)\b.*$",
            "", RegexOptions.IgnoreCase);

        return title.Trim();
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