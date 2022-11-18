using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text.Json;

public class AppSettings
{
    [JsonPropertyName("PlaylistIndexOnHour")]
    [JsonInclude]
    public Dictionary<int, int> _playlistIndexOnHour = new();

    [JsonIgnore]
    public int PlayListIndex
    {
        get
        {
            var currentHour = ((int)DateTime.Now.DayOfWeek * 100) + DateTime.Now.Hour;
            if (_playlistIndexOnHour.TryGetValue(currentHour, out currentHour))
                return currentHour;
            return 0;
        }
        set
        {
            _playlistIndexOnHour[((int)DateTime.Now.DayOfWeek * 100) + DateTime.Now.Hour] = value;
        }
    }

    private static JsonSerializerOptions options = new JsonSerializerOptions
    {
        IncludeFields = false
    };

    public static AppSettings Load(string path = "appsettings.json")
    {
        if (!File.Exists(path))
            return new();

        using var stream = File.Open(path, FileMode.Open);
        var appSettings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(stream, options);
        return appSettings;
    }
    public void Save(string path = "appsettings.json")
    {
        using var stream = File.Open(path, FileMode.Create);
        System.Text.Json.JsonSerializer.Serialize(stream, this, options);
    }
}
