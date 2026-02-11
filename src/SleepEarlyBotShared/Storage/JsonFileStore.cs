using System.Text.Json;
using System.IO;

namespace SleepEarlyBot.Storage;

public static class JsonFileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static T? Load<T>(string path)
    {
        if (!File.Exists(path)) return default;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    // Atomic-ish save: write temp then replace.
    public static void SaveAtomic<T>(string path, T value)
    {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        var tmp = path + ".tmp";
        var json = JsonSerializer.Serialize(value, Options);

        File.WriteAllText(tmp, json);

        // Replace existing file if present
        if (File.Exists(path))
            File.Replace(tmp, path, null);
        else
            File.Move(tmp, path);
    }
}

