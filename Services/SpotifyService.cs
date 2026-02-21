using GjettLataBackend.Models;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GjettLataBackend.Services;

using SpotifyAPI.Web;

public class SpotifyService
{
    private readonly SpotifyClient _client;

    public SpotifyService(IConfiguration config)
    {
        var clientId = config["SPOTIFY_CLIENT_ID"];
        var clientSecret = config["SPOTIFY_CLIENT_SECRET"];

        var request = new ClientCredentialsRequest(clientId, clientSecret);
        var response = new OAuthClient().RequestToken(request).Result;

        _client = new SpotifyClient(response.AccessToken);
    }

    public async Task<List<(string Title, string Artist)>> 
        GetRandomTracksFromPlaylist(string playlistId, int limit)
    {
        var tracks = new List<FullTrack>();

        var page = await _client.Playlists.GetItems(playlistId);

        while (page != null)
        {
            tracks.AddRange(
                page.Items
                    .Select(i => i.Track as FullTrack)
                    .Where(t => t != null)!);

            if (page.Next == null) break;
            page = await _client.NextPage(page);
        }

        return tracks
            .OrderBy(_ => Random.Shared.Next())
            .Take(limit)
            .Select(t => (t.Name, t.Artists.First().Name))
            .ToList();
    }
}