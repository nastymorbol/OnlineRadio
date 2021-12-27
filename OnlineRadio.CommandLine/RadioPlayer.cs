﻿using OnlineRadio.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineRadio
{
    public class RadioPlayer : IDisposable
    {
        private string _songArtist;
        private string _songTitle;
        private Stopwatch _stopWatch = new Stopwatch();

        public string PlaylistEntry { get; private set; }

        private Radio _radio;

        public string SongTitle => _radio?.CurrentSong?.Title ?? string.Empty;
        public string SongArtist => _radio?.CurrentSong?.Artist ?? string.Empty;
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