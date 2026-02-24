using GjettLataBackend.Models;
using SpotifyAPI.Web;

namespace GjettLataBackend.Services;

public class SpotifyService
{
    private readonly IConfiguration _config;
    private SpotifyClient? _client;
    private DateTime _tokenExpiresAt = DateTime.MinValue;

    public SpotifyService(IConfiguration config)
    {
        _config = config;
    }

    private async Task<SpotifyClient> GetClient()
    {
        if (_client != null && DateTime.UtcNow < _tokenExpiresAt)
            return _client;

        var clientId = _config["SPOTIFY_CLIENT_ID"];
        var clientSecret = _config["SPOTIFY_CLIENT_SECRET"];

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
            throw new Exception("Spotify credentials missing");

        var oauth = new OAuthClient();
        var tokenResponse = await oauth.RequestToken(
            new ClientCredentialsRequest(clientId, clientSecret));

        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30);

        _client = new SpotifyClient(tokenResponse.AccessToken);

        return _client;
    }

    public async Task<List<(string Title, string Artist)>> 
        GetRandomTracksFromPlaylist(string playlistId, int limit)
    {
        var client = await GetClient();

        var tracks = new List<FullTrack>();

        var request = new PlaylistGetItemsRequest
        {
            Market = "NO"
        };
        
        var page = await client.Playlists.GetPlaylistItems(playlistId, request);

        while (true)
        {
            tracks.AddRange(
                page.Items
                    .Select(i => i.Track as FullTrack)
                    .Where(t => t != null)!);

            if (page.Next == null)
                break;

            page = await client.NextPage(page);
        }

        return tracks
            .OrderBy(_ => Random.Shared.Next())
            .Take(limit)
            .Select(t => (t.Name, t.Artists.First().Name))
            .ToList();
    }
}