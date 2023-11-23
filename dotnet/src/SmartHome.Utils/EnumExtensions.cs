namespace SmartHome.Utils;

public static class EnumExtensions
{
    public static T Next<T>(this T currentValue)
        where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var currentIndex = Array.IndexOf(values, currentValue);
        var nextIndex = (currentIndex + 1) % values.Length;
        return values[nextIndex];
    }
}