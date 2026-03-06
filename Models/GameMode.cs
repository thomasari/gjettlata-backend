namespace GjettLataBackend.Models;

using System.ComponentModel.DataAnnotations;

public enum GameMode
{
    [Display(Name = "70-tallet")]
    Seventies,

    [Display(Name = "80-tallet")]
    Eighties,

    [Display(Name = "90-tallet")]
    Nineties,

    [Display(Name = "2000-tallet")]
    TwoThousands,

    [Display(Name = "2010-tallet")]
    TwentyTens,

    [Display(Name = "2020-tallet")]
    TwentyTwenties,

    [Display(Name = "Alle tidsaldre")]
    AllTime,

    [Display(Name = "Topp 100 Norge", Description = "1313619885")]
    TopNorway,

    [Display(Name = "Topp 100 verden", Description = "3155776842")]
    TopWorld
}

public static class GameModeExtensions
{
    public static string GetPlaylistIdForGamemode(GameMode gameMode)
    {
        return gameMode switch
        {
            GameMode.Seventies or GameMode.Eighties or GameMode.Nineties or GameMode.TwoThousands or GameMode.TwentyTens
                or GameMode.TwentyTwenties or GameMode.AllTime => "",
            GameMode.TopNorway => "1313619885",
            GameMode.TopWorld => "3155776842",
            _ => ""
        };
    }
}