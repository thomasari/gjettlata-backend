using GjettLataBackend.Models;
using GjettLataBackend.Models.DTO;

namespace GjettLataBackend.Services;

using System.Net.Http.Json;

public class DeezerService
{
    private readonly HttpClient _http;

    public DeezerService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.deezer.com/");
    }

    public async Task<Song?> GetPreview(string title, string artist)
    {
        var query = $"{title} {artist}";
        var url = $"search?q={Uri.EscapeDataString(query)}&limit=1";

        var response = await _http
            .GetFromJsonAsync<DeezerSearchResponse>(url);

        var track = response?.Data?
            .FirstOrDefault(t => !string.IsNullOrEmpty(t.Preview));

        if (track == null) return null;

        return new Song
        {
            Name = track.Title,
            Artists = new List<string> { track.Artist.Name },
            Url = track.Preview,
            StartAt = Random.Shared.Next(5, 20)
        };
    }

    public async Task<List<Song>> GetPreviews(
        IEnumerable<(string Title, string Artist)> tracks)
    {
        var tasks = tracks.Select(t =>
            GetPreview(t.Title, t.Artist));

        var results = await Task.WhenAll(tasks);

        return results
            .Where(s => s != null)!
            .ToList()!;
    }
}