using GjettLataBackend.Models;

namespace GjettLataBackend.Services;

public static class PlaylistMap
{
    public static string GetPlaylistId(GameMode mode) => mode switch
    {
        GameMode.Seventies     => "37i9dQZF1DWTJ7xPn4vNaz",
        GameMode.Eighties      => "37i9dQZF1DX4UtSsGT1Sbe",
        GameMode.Nineties      => "37i9dQZF1DXbTxeAdrVG2l",
        GameMode.TwoThousands  => "37i9dQZF1DX4o1oenSJRJd",
        GameMode.TwentyTens    => "37i9dQZF1DX5Ejj0EkURtP",
        GameMode.TwentyTwenties=> "37i9dQZF1DX2M1RktxUUHG",
        GameMode.AllTime       => "6mxngtbunDsKkoDSeE0tIh",
        _ => throw new ArgumentOutOfRangeException()
    };
}