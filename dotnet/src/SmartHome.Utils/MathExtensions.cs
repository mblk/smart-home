using System.Diagnostics;

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
        Debug.Assert(min < max);

        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static double Normalize(this double value, double min, double max)
    {
        Debug.Assert(min < max);

        double range = max - min;

        value = (value - min) % range;

        if (value < 0) value += range;

        return value + min;
    }

    public static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t.Clamp(0d, 1d);
    }

    public static double NormalizedAngleDiffDeg(double angle1, double angle2)
    {
        double diff = Math.Abs(angle1 - angle2) % 360;
        return diff > 180 ? 360 - diff : diff;

        // Returns -180...180 i think
    }

    public static double NormalizedAngleDiffRad(double angle1, double angle2)
    {
        double diff = Math.Abs(angle1 - angle2) % (2.0 * Math.PI);
        return diff > Math.PI ? (2.0 * Math.PI) - diff : diff;

        // Returns -PI...PI i think
    }
}
