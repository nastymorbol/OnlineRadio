using Flurl.Http;
using OnlineRadio.Console;
using PlaylistsNET.Content;
using PlaylistsNET.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineRadio.Commands
{
    internal sealed class GeneratePlaylistCommand : AsyncCommand<GeneratePlaylistCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("File name for new playlist.")]
            [DefaultValue("playlist.m3u8")]
            [CommandArgument(0, "[FILENAME]")]
            public string FileName { get; init; }

        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            if (System.IO.File.Exists(settings.FileName))
            {
                var overwrite = AnsiConsole.Confirm($"The file {settings.FileName} already exist. Overwrite?", true);
                if(!overwrite)
                    return ValidationResult.Error($"File already exists");
            }

            return base.Validate(context, settings);
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),    // Task description
                        new ProgressBarColumn(),        // Progress bar
                        new PercentageColumn(),         // Percentage
                        new ElapsedTimeColumn(),      // Remaining time
                    })
                .StartAsync(async ctx =>
                {
                    var task1 = ctx.AddTask("[green]Download[/]");
                    var task2 = ctx.AddTask("[green]Parsing[/]", autoStart:false);

                    task1.Description = "Download from GitHub";
                    task1.Increment(20);
                    var r = await "https://gist.githubusercontent.com/beigbeider/66ffb4ef76e56b92314e55ecbf2a9b9d/raw/cec919919e0ce81f6f6a0df25e639ddc24fd671c/live_streams.sii".GetStringAsync();
                    task1.Increment(80);
                    task1.StopTask();  
                    task2.StartTask();
                    M3uPlaylist generated = new M3uPlaylist();
                    generated.IsExtended = true;
                    task2.Increment(5);
                    generated.PlaylistEntries.Add(new M3uPlaylistEntry()
                    {
                        Album = "WDR4",
                        AlbumArtist = "DE",
                        Duration = TimeSpan.Zero,
                        Path = "https://wdr-wdr4-live.icecastssl.wdr.de/wdr/wdr4/live/mp3/128/stream.mp3",
                        Title = "WDR4",
                        CustomProperties = { { "CountryCode", "DE" }, { "Genre", "Schlager" }, { "Stationname", "WDR 4" } }
                    });

                    task2.Increment(5);
                    generated.PlaylistEntries.Add(new M3uPlaylistEntry()
                    {
                        Album = "Sunshine Live",
                        AlbumArtist = "DE",
                        Duration = TimeSpan.Zero,
                        Path = "http://sunshinelive.hoerradar.de/sunshinelive-live-mp3-hq",
                        Title = "Sunshine Live",
                        CustomProperties = { { "CountryCode", "DE" }, { "Genre", "Schlager" }, { "Stationname", "Sunshine Live" } }
                    });

                    var data = r.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    var incr = 90d / data.Length;
                    var delay = 2500 / data.Length;
                    foreach (var line in data)
                    {
                        task2.Increment(incr);
                        await Task.Delay(delay);
                        if (!line.Contains("stream_data[")) continue;


                        var index = line.IndexOf("\"");
                        if (index < 0) continue;
                        var lineData = line.Substring(index + 1);
                        lineData = lineData.Substring(0, lineData.Length - 1);
                        var (path, title, genre, countryCode) = lineData.Split('|');
                        if (!generated.PlaylistEntries.Any(e => e.Path == path))
                        {
                            var entry = new M3uPlaylistEntry()
                            {
                                Duration = TimeSpan.Zero,
                                Path = path,
                                Title = title,
                                CustomProperties = { { "CountryCode", countryCode }, { "Genre", genre }, { "Stationname", title } }
                            };
                            if (title.Contains("sunshine", StringComparison.CurrentCultureIgnoreCase))
                            {
                                if (title.Contains("hard", StringComparison.CurrentCultureIgnoreCase))
                                { generated.PlaylistEntries.Insert(2, entry); continue; }
                                var lastSunshine = generated.PlaylistEntries.FindLastIndex(e => e.Path.Contains("sunshine"));
                                generated.PlaylistEntries.Insert(lastSunshine + 1, entry);
                            }
                            else
                            {
                                if (countryCode.Contains("DE", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    var lastSunshine = generated.PlaylistEntries.FindLastIndex(e => e.CustomProperties["CountryCode"] == countryCode);
                                    generated.PlaylistEntries.Insert(lastSunshine + 1, entry);
                                }
                                else
                                {
                                    generated.PlaylistEntries.Add(entry);
                                }
                            }
                        }
                    }

                    task2.Increment(20);
                    M3uContent content = new M3uContent();
                    string text = content.ToText(generated);

                    task2.Increment(20);
                    File.WriteAllText(settings.FileName, text);
                    task2.Increment(20);

                });

            return 0;
        }
    }
}
