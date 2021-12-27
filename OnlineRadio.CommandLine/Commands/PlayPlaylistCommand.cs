using Flurl.Http;
using Humanizer;
using OnlineRadio.Console;
using PlaylistsNET.Content;
using PlaylistsNET.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineRadio.Commands
{
    internal sealed class PlayPlaylistCommand : AsyncCommand<PlayPlaylistCommand.Settings>
    {
        private readonly RadioPlayer _radioPlayer;
        private readonly CancellationTokenSource _tokenSource;
        private Task _updateUiTask;

        public sealed class Settings : CommandSettings
        {
            [Description("File name for new playlist.")]
            [DefaultValue("playlist.m3u8")]
            [CommandArgument(0, "[FILENAME]")]
            public string FileName { get; init; }

            [CommandOption("-u|--url")]
            public string Url { get; init; }

        }

        public PlayPlaylistCommand()
        {
            var radioPlayer = new RadioPlayer();
            _radioPlayer = radioPlayer;
            _tokenSource = new CancellationTokenSource();
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            if (settings.Url == null && !System.IO.File.Exists(settings.FileName))
            {                             
                return ValidationResult.Error($"Playlist [{settings.FileName}] doesn't exist. You may download an playlist with [dowload] command");
            }


            _updateUiTask = Task.Run(UpdateUiTask);
            return base.Validate(context, settings);
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if(settings.Url != null)
            {
                _radioPlayer.Play(settings.Url);
                while (!_tokenSource.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
                return 0;
            }

            var content = File.ReadAllText(settings.FileName);
            try
            {
                var appSetting = CommandLine.AppSettings.Load();
                var parser = PlaylistParserFactory.GetPlaylistParser(PlaylistType.M3U8);
                var pl = parser.GetFromString(content) as M3uPlaylist;

                var length = pl.PlaylistEntries.Count;
                var currenPlaylistIndex = appSetting.PlayListIndex;
                var lastPlaylistIndex = currenPlaylistIndex-1;
                
                while (!_tokenSource.IsCancellationRequested)
                {
                    if (lastPlaylistIndex != currenPlaylistIndex)
                    {
                        lastPlaylistIndex = currenPlaylistIndex;
                        var entry = pl.PlaylistEntries.ElementAtOrDefault(currenPlaylistIndex);

                        if (entry == default && currenPlaylistIndex == 0)
                            throw new ArgumentException("The Playlist has no entrys at all. Genarate a playlist or download one.");

                        if (entry == default)
                        {
                            currenPlaylistIndex = 0;
                            continue;
                        }

                        var title = entry.Title;
                        if (string.IsNullOrWhiteSpace(title))
                            title = entry.Path;
                        _radioPlayer.Play(entry.Path, $"[{currenPlaylistIndex,2}] {title}");

                        appSetting.PlayListIndex = currenPlaylistIndex;
                        appSetting.Save();
                    }

                    await AnsiConsole.Console.Input.ReadKeyAsync(true, _tokenSource.Token)
                        .ContinueWith( task => 
                        {
                            if (task.Result == null) return;

                            var key = (ConsoleKeyInfo)task.Result;

                            if (char.IsNumber(key.KeyChar))
                            {
                                currenPlaylistIndex = int.Parse($"{key.KeyChar}");
                            }
                            else if (key.Key == ConsoleKey.N || key.Key == ConsoleKey.RightArrow)
                                currenPlaylistIndex++;
                            else if (key.Key == ConsoleKey.P || key.Key == ConsoleKey.LeftArrow)
                                currenPlaylistIndex--;
                            else if (key.Key == ConsoleKey.X || key.Key == ConsoleKey.Q)
                            {
                                _tokenSource.Cancel();
                                return;
                            }
                            if (currenPlaylistIndex < 0)
                                currenPlaylistIndex = length - 1;
                            if (currenPlaylistIndex >= length)
                                currenPlaylistIndex = 0;
                        });
                }

            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
            finally
            {
                _radioPlayer.Stop();
                _radioPlayer.Dispose();
            }

            return 0;
        }

        private void UpdateUiTask()
        {
            var lastConsoleWindowSize = new System.Drawing.Size(System.Console.WindowWidth, System.Console.WindowHeight);
            var table = new Table().LeftAligned().RoundedBorder().Width(200);
            table.HideHeaders();
            table.AddColumn("-DESCRP-", c => { c.Width(8).NoWrap(); });
            table.AddColumn("-VALUES-", c => { c.NoWrap(); });
            table.AddRow   ("Artist",   "-");
            table.AddRow   ("Song",     "-");
            table.AddRow   ("Duration", "-");
            table.AddRow   ("Playlist", "-");

            AnsiConsole.Clear();

            AnsiConsole.Live(table)
                .Overflow(VerticalOverflow.Ellipsis)
                .AutoClear(true)
                .Start(ctx =>
                {
                    ctx.Refresh();
                    var lastHumanized = _radioPlayer?.Elapsed.Humanize();
                    var lastSong = string.Empty;
                    while (!_tokenSource.IsCancellationRequested)
                    {
                        var consoleWindowSize = new System.Drawing.Size(System.Console.WindowWidth, System.Console.WindowHeight);
                        if (lastConsoleWindowSize != consoleWindowSize)
                            break;

                        var humanized = _radioPlayer?.Elapsed.Humanize(2, minUnit: Humanizer.Localisation.TimeUnit.Second);
                        if (lastHumanized == humanized && lastSong == _radioPlayer?.SongTitle)
                        {
                            Thread.Sleep(500);
                            continue;
                        }
                        lastSong = _radioPlayer?.SongTitle;
                        lastHumanized = humanized;
                        var cellWitdh = AnsiConsole.Console.Profile.Width - 8 - 6 /* Border elemts*/;
                        var artist = (_radioPlayer?.SongArtist ?? String.Empty).TruncateMid(cellWitdh);
                        var song = (_radioPlayer?.SongTitle ?? String.Empty).TruncateMid(cellWitdh);
                        var playListEntry = (_radioPlayer?.PlaylistEntry ?? String.Empty).TruncateMid(cellWitdh).EscapeMarkup(); 

                        table.UpdateCell(0, 1, artist);
                        table.UpdateCell(1, 1, song);
                        table.UpdateCell(2, 1, humanized);
                        table.UpdateCell(3, 1, playListEntry);
                        ctx.Refresh();
                        Task.Delay(100).Wait();
                    }
                });
            
            if(!_tokenSource.IsCancellationRequested)
                UpdateUiTask();
        }
    }
}
