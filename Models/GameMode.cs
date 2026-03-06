using System.Reflection;

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
    TopWorld,
    
    [Display(Name = "Russelåter", Description = "7281487184")]
    Russ
}



public static class GameModeExtensions
{
    public static string? GetPlaylistId(this GameMode mode)
    {
        var member = typeof(GameMode).GetMember(mode.ToString()).FirstOrDefault();
        var attr = member?.GetCustomAttribute<DisplayAttribute>();
        return attr?.Description ?? "";
    }
}
