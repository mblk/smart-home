using System.Text.Json;

namespace SmartHome.Utils;

public static class ObjectExtensions
{
    public static T DumpToConsole<T>(this T @object)
    {
        var output = "NULL";
        if (@object != null)
        {
            output = JsonSerializer.Serialize(@object, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
            });
        }

        Console.WriteLine($"[{@object?.GetType().Name}]:\r\n{output}");
        return @object;
    }

    public static void PrintDiffTo<T>(this T oldValue, T newValue, string name)
    {
        var type = typeof(T);
        var changes = new List<string>();

        foreach (var field in type.GetFields())
        {
            var v1 = field.GetValue(oldValue);
            var v2 = field.GetValue(newValue);
            if (EqualsSafe(v1, v2)) continue;
            changes.Add($"{field.Name}: '{v1}' -> '{v2}'");
        }

        foreach (var property in type.GetProperties())
        {
            var v1 = property.GetValue(oldValue);
            var v2 = property.GetValue(newValue);
            if (EqualsSafe(v1, v2)) continue;
            changes.Add($"{property.Name}: '{v1}' -> '{v2}'");
        }

        if (!changes.Any()) return;

        if (changes.Count == 1)
        {
            Console.WriteLine($"{name}: {changes.Single()}");
        }
        else
        {
            Console.WriteLine($"{name}:");
            foreach (var change in changes.Order())
                Console.WriteLine($"  {change}");
        }
    }

    private static bool EqualsSafe(object? v1, object? v2)
    {
        if (v1 is null && v2 is null) return true;
        if (v1 is null && v2 is not null) return false;
        if (v1 is not null && v2 is null) return false;
        return v1.Equals(v2);
    }
}