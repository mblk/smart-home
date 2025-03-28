namespace SmartHome.Utils;

public static class MathExtensions
{
    public static double Rad2Deg(this double value)
    {
        return value * 180d / Math.PI;
    }

    public static double Deg2Rad(this double value)
    {
        return value * Math.PI / 180d;
    }

    public static double Clamp(this double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t.Clamp(0d, 1d);
    }
}
