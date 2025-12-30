using System.ComponentModel;
using System.Reflection;

namespace MediaServerNotification.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        return field?
            .GetCustomAttribute<DescriptionAttribute>()?
            .Description
            ?? value.ToString();
    }
}
