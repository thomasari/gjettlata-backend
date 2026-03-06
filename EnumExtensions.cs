using GjettLataBackend.Models;

namespace GjettLataBackend;

using System.Reflection;
using System.ComponentModel.DataAnnotations;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).First();
        var attr = member.GetCustomAttribute<DisplayAttribute>();
        return attr?.Name ?? value.ToString();
    }
}