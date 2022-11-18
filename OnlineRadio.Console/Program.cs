using Spectre.Console.Cli;


if(args.Length == 0)
    args = new string[] {"play"};

var app = new CommandApp();
app.SetDefaultCommand<OnlineRadio.Commands.PlayPlaylistCommand>();

app.Configure(config =>
{
    config.Settings.ApplicationName = "Steve's Online Radio Player";
    config.AddCommand<OnlineRadio.Commands.GeneratePlaylistCommand>("download")
        .WithAlias("new")
        .WithDescription("Download and generate Play list.")
        .WithExample(new[] { "download", "playlist.m3u" });
    config.AddCommand<OnlineRadio.Commands.PlayPlaylistCommand>("play")
        .WithAlias("run")
        .WithDescription("Play the given playlist.")
        .WithExample(new[] { "play", "playlist.m3u" });
});

await app.RunAsync(args);
