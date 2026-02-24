using GjettLataBackend.Models;

namespace GjettLataBackend.Services;

public static class PlaylistMap
{
    public static string GetPlaylistId(GameMode mode) => mode switch
    {
        GameMode.Seventies     => "2hmwxgzLszBO6SFatfN3Ov",
        GameMode.Eighties      => "07imPCgtRvBuMXZyn81QUy",
        GameMode.Nineties      => "7GZKFmBLInTBCETDzFTvI7",
        GameMode.TwoThousands  => "59UwpOFYzIOYSBNooTz4d8",
        GameMode.TwentyTens    => "3xHhDtwA99QrNGKW32yMcz",
        GameMode.TwentyTwenties=> "4SZngxtqgc72dvd40HsYJn",
        GameMode.AllTime       => "5VPzwKRTAMZ2OKOpoPxoHr",
        _ => throw new ArgumentOutOfRangeException()
    };
}