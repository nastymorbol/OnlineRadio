using OnlineRadio.Core;
using System;
using System.Diagnostics;

namespace OnlineRadio
{
    public class RadioPlayer : IDisposable
    {
        private string _songArtist;
        private string _songTitle;
        private Stopwatch _stopWatch = new Stopwatch();

        public string PlaylistEntry { get; private set; }

        private Radio _radio;

        public bool Running => _radio?.Running ?? false;
        public string SongTitle => Running ? _radio?.CurrentSong?.Title ?? string.Empty : "Pause ...";
        public string SongArtist => Running ? _radio?.CurrentSong?.Artist ?? string.Empty : "Pause ...";
        public TimeSpan Elapsed => _stopWatch?.Elapsed ?? TimeSpan.Zero;

        public RadioPlayer()
        {
            // Load Assemply ...
            _ = new Plugins.Audio.AudioPlugin();
        }

        public void Play(string url, string playlistEntry = null)
        {
            if (_radio != null)
                Stop();

            PlaylistEntry = playlistEntry?.Trim();
            _radio = new Radio(url, true);
            _radio.OnCurrentSongChanged += _radio_OnCurrentSongChanged;

            _radio.Start();
            _stopWatch.Restart();            
        }

        public void Stop()
        {
            _radio?.Stop();
            if(_radio != default)
                _radio.OnCurrentSongChanged -= _radio_OnCurrentSongChanged;
            _radio?.Dispose();
            _radio = null;
        }

        public void Pause()
        {
            _radio?.Stop();
            _stopWatch.Stop();            
        }

        public void Resume()
        {
            _radio?.Start();
            _stopWatch.Start();
        }

        private void _radio_OnCurrentSongChanged(object sender, CurrentSongEventArgs eventArgs)
        {
            var songArtist = eventArgs.NewSong.Artist;
            var songTitle = eventArgs.NewSong.Title;

            if (_songTitle != songTitle)
                _stopWatch.Restart();

            _songArtist = songArtist;
            _songTitle = songTitle;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
