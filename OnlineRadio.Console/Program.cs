using System;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console.Cli;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text.Json;

namespace OnlineRadio.Console
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            if(args.Length == 0)
                args = new string[] {"play"};

            var app = new CommandApp();            
            app.SetDefaultCommand<Commands.PlayPlaylistCommand>();

            app.Configure(config =>
            {
                config.Settings.ApplicationName = "Steve's Online Radio Player";
                config.AddCommand<Commands.GeneratePlaylistCommand>("download")
                    .WithAlias("new")
                    .WithDescription("Download and generate Playlist.")
                    .WithExample(new[] { "download", "playlist.m3u" });
                config.AddCommand<Commands.PlayPlaylistCommand>("play")                    
                    .WithAlias("run")
                    .WithDescription("Play the given playlist.")
                    .WithExample(new[] { "play", "playlist.m3u" });                
            });

            await app.RunAsync(args);
        }
    }

    public class AppSettings
    {
        [JsonPropertyName("PlaylistIndexOnHour")]
        [JsonInclude]
        public Dictionary<int, int> _playlistIndexOnHour = new();

        [JsonIgnore]
        public int PlayListIndex { 
            get
            {
                var currentHour = ((int)DateTime.Now.DayOfWeek * 100) + DateTime.Now.Hour;
                if(_playlistIndexOnHour.TryGetValue(currentHour, out currentHour))
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
}
