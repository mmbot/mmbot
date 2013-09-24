using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MMBot.Adapters;
using SpotiFire;

namespace MMBot.Spotify
{
    public class SpotifyPlayer
    {
        private const string CLIENT_NAME = "MMBotSpotifyPlayer";
        private readonly Regex _spotifyLinkRegex = new Regex(@"spotify:(album|track|user:[a-zA-Z0-9]+:playlist):[a-zA-Z0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private IPlayer _player = new NAudioPlayer();
        private bool _isShuffleOn;
        private static Random _random = new Random(DateTime.Now.Millisecond);
        private static byte[] key = new byte[]
        {
            0x01, 0xAE, 0x58, 0xF9, 0xD6, 0xA5, 0x00, 0x8D, 0x43, 0xE3, 0x80, 0xB0, 0x6B, 0x9F, 0xC4, 0xFC,
            0x01, 0x37, 0x83, 0x55, 0xC6, 0x67, 0x2C, 0xF6, 0x6D, 0x8B, 0x0A, 0xF8, 0x5D, 0x41, 0xBA, 0xED,
            0x41, 0x04, 0xC7, 0x7E, 0x46, 0x1D, 0x4A, 0x78, 0x2A, 0xB8, 0x6A, 0xA0, 0x95, 0x39, 0x89, 0x6E,
            0x42, 0x46, 0x1A, 0xB7, 0xE4, 0xA5, 0x90, 0xFC, 0x06, 0x33, 0xA9, 0xA9, 0x90, 0xB4, 0x30, 0x23,
            0x6A, 0x92, 0x90, 0xE8, 0xDF, 0x2F, 0x3E, 0x55, 0x5E, 0x2A, 0x37, 0xCA, 0x3A, 0x92, 0xCB, 0xD2,
            0x7C, 0xC6, 0x15, 0x75, 0x8A, 0x40, 0xDD, 0x76, 0x5C, 0x56, 0xF0, 0x04, 0xF1, 0x30, 0xED, 0xDD,
            0x32, 0xB7, 0x3C, 0x6A, 0x1B, 0xE3, 0xAB, 0x79, 0x41, 0xD1, 0xE8, 0x2D, 0xE8, 0x0B, 0x06, 0x00,
            0xC5, 0xEE, 0x4A, 0x5E, 0xAE, 0x0E, 0xBE, 0x82, 0x22, 0x73, 0x13, 0x5C, 0xA5, 0xA7, 0xDA, 0x50,
            0x9D, 0xC4, 0xDE, 0x8C, 0xF9, 0x70, 0xC7, 0x23, 0xA3, 0x9B, 0xD7, 0x42, 0x0E, 0xA8, 0xAF, 0x8E,
            0x89, 0xA9, 0x95, 0xF4, 0x05, 0x48, 0x99, 0x07, 0xA1, 0xB5, 0x46, 0xFB, 0xBC, 0x29, 0x09, 0x5A,
            0x5A, 0xBC, 0x02, 0x8B, 0xEA, 0x63, 0xF6, 0x2B, 0x5E, 0x0A, 0xA5, 0xDF, 0x82, 0x78, 0xFB, 0x16,
            0x12, 0xE7, 0x45, 0x7D, 0x6E, 0x33, 0xA5, 0xE8, 0x55, 0x10, 0x26, 0xEC, 0x98, 0x4C, 0x26, 0x0C,
            0x22, 0x50, 0xBD, 0x9D, 0x5C, 0x0D, 0x4C, 0x96, 0x1B, 0x76, 0x76, 0x0F, 0x1E, 0x75, 0x12, 0xEA,
            0xFE, 0x80, 0x9D, 0xC6, 0x5E, 0x30, 0xC8, 0xF8, 0xF1, 0xBD, 0x19, 0xB3, 0x96, 0x1D, 0x8C, 0x90,
            0x99, 0x9B, 0xDE, 0xF3, 0xD9, 0xB9, 0xDF, 0xFE, 0x24, 0xAD, 0x04, 0xEB, 0x3E, 0xDD, 0x04, 0x0B,
            0x6E, 0x68, 0x77, 0x0E, 0x15, 0x69, 0xA2, 0x35, 0x06, 0xDB, 0x05, 0x3A, 0x2E, 0xE4, 0xC7, 0xF9,
            0xA6, 0xCD, 0x64, 0xF2, 0xDA, 0x33, 0x6B, 0x7F, 0xB9, 0x6C, 0x60, 0x34, 0x6A, 0xFC, 0x2A, 0x43,
            0x2C, 0xCB, 0x76, 0x47, 0xFC, 0x1D, 0xEA, 0xF4, 0xCC, 0x54, 0x57, 0x6C, 0x92, 0xDD, 0x0D, 0xE7,
            0x1F, 0x07, 0x97, 0x2A, 0xEF, 0x01, 0xEF, 0x02, 0x7A, 0x42, 0xFC, 0x50, 0x36, 0x3A, 0xDB, 0xC7,
            0x18, 0xE1, 0xBF, 0x76, 0xD7, 0x18, 0x4E, 0x27, 0xDF, 0x67, 0xA6, 0x52, 0x82, 0xC7, 0x70, 0x2F,
            0x27
        };

        private static string cache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CLIENT_NAME, "cache");

        private static string settings =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CLIENT_NAME, "settings");

        private static string userAgent = CLIENT_NAME;
        private Session _session;
        private readonly Robot _robot;
        private readonly List<Track> _queue = new List<Track>();
        private Track _currentTrack = null;
        private string _loungeRoom;

        public event EventHandler<Track> TrackChanged;


        public SpotifyPlayer(Robot robot)
        {
            _robot = robot;
        }

        public string LoungeRoom
        {
            get { return _loungeRoom; }
            set { _loungeRoom = value; }
        }

        public Track CurrentTrack
        {
            get { return _currentTrack; }
        }

        public IEnumerable<Track> Queue
        {
            get { return _queue; }
        }

        public async Task SetShuffleOn()
        {
            _isShuffleOn = true;
            await _robot.Brain.Set("SPOTIFY_SHUFFLE", _isShuffleOn);
        }

        public async Task SetShuffleOff()
        {
            _isShuffleOn = false;
            await _robot.Brain.Set("SPOTIFY_SHUFFLE", _isShuffleOn);
        }

        public async Task Login()
        {
            if (_session != null && _session.ConnectionState != ConnectionState.Disconnected &&
                _session.ConnectionState != ConnectionState.Undefined)
            {
                return;
            }

            string errorMessage = null;
            try
            {
                if (string.IsNullOrEmpty(_robot.GetConfigVariable("MMBOT_SPOTIFY_USERNAME")))
                {
                    throw new Exception(string.Format("Could not login to Spotify - Spotify is not configured. You must supply the MMBOT_SPOTIFY_USERNAME and MMBOT_SPOTIFY_PASSWORD environment variables"));
                }

                _isShuffleOn = await _robot.Brain.Get<bool>("SPOTIFY_SHUFFLE");

                _session = await SpotiFire.Spotify.CreateSession(key, cache, settings, userAgent);

                _session.MusicDelivered += OnMusicDelivered;
                _session.EndOfTrack += OnTrackEnded;

                var loginResult = await _session.Login(_robot.GetConfigVariable("MMBOT_SPOTIFY_USERNAME"),
                    _robot.GetConfigVariable("MMBOT_SPOTIFY_PASSWORD"), false);

                if (loginResult != Error.OK)
                {
                    throw new Exception(string.Format("Could not login to Spotify - {0}", loginResult));
                }
                _session.PreferredBitrate = BitRate.Bitrate160k;

                await LoadQueue();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                errorMessage = e.Message;
            }
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new Exception(string.Format("Could not login to Spotify - {0}", errorMessage));
            }
        }

        public async Task<string> QueueUpAlbum(Album album)
        {
            AlbumBrowse albumBrowse = await album.Browse();
            albumBrowse.Tracks.ForEach(t => AddToQueue(t));
            await SaveQueue();
            return string.Format("Queued up {0} tracks from album {1} by {2}",
                    albumBrowse.Tracks.Count, albumBrowse.Album.Name, albumBrowse.Artist.Name);
        }

        public async Task<string> QueueUpPlaylist(Playlist playlist)
        {
            playlist.Tracks.ForEach(t => AddToQueue(t));
            await SaveQueue();
            return string.Format("Queued up {0} tracks from playlist {1}", playlist.Tracks.Count,
                    playlist.Name);
        }

        private void AddToQueue(Track track, int position = -1)
        {
            if (Queue.All(t => t.GetLink().ToString() != track.GetLink().ToString()))
            {
                if (position > -1)
                {
                    _queue.Insert(position, track);
                }
                else
                {
                    AddToQueue(track);
                }
            }
        }

        public async Task<string> PlayAlbum(Album album)
        {
            AlbumBrowse albumBrowse = await album.Browse();
            await Play(albumBrowse.Tracks[0]);
            await PrependToQueue(albumBrowse.Tracks.Skip(1));
            await SaveQueue();

            return string.Format("Queued up {0} tracks from album {1} by {2}",
                albumBrowse.Tracks.Count, albumBrowse.Album.Name, albumBrowse.Artist.Name);
        }

        private async Task<string> PlayPlaylist(Playlist playlist)
        {
            await Play(playlist.Tracks[0]);
            await PrependToQueue(playlist.Tracks.Skip(1));
            
            await SaveQueue();
            return string.Format("Queued up {0} tracks from playlist {1}", playlist.Tracks.Count,
                playlist.Name);
        }

        private async Task PrependToQueue(IEnumerable<Track> tracks)
        {
            tracks.Reverse().ForEach(t => AddToQueue(t, 0));
            await SaveQueue();
        }

        public async Task<string> QueueUpTrack(Track track, bool showMessage)
        {
            string message = null;
            if (track != null)
            {
                if (showMessage)
                {
                    message = (string.Format("Queued up {0}. It is currently number #{1} in the queue",
                        track.GetDisplayName(), _queue.Count + 1));
                }
                AddToQueue(track);
                await SaveQueue();
            }
            return message;
        }



        public async Task<Track> SearchForTrack(string query)
        {
            var tracks = await _session.SearchTracks(query, 0, 1);

            if (!tracks.Tracks.Any())
            {
                return null;
            }

            return tracks.Tracks[0];
        }

        public async Task<Album> SearchForAlbum(string query)
        {
            var albums = await _session.SearchAlbums(query, 0, 1);

            if (!albums.Albums.Any())
            {
                return null;
            }

            return albums.Albums[0];
        }

        public async Task<Playlist> SearchForPlaylist(string query)
        {
            var playlists = await _session.SearchPlaylist(query, 0, 1);

            if (!playlists.Albums.Any())
            {
                return null;
            }

            return playlists.Playlists[0];
        }

        public async Task<Artist> SearchForArtist(string query)
        {
            var artists = await _session.SearchArtists(query, 0, 1);

            if (!artists.Artists.Any())
            {
                return null;
            }

            return artists.Artists[0];
        }

        public async Task Play(Track track)
        {
            _session.PlayerUnload();
            _session.PlayerLoad(track);
            await Play();
        }


        private void OnMusicDelivered(Session sender, MusicDeliveryEventArgs e)
        {
            if (e.Samples.Length > 0)
            {
                e.ConsumedFrames = _player.EnqueueSamples(e.Channels, e.Rate, e.Samples, e.Frames);
            }
            else
            {
                e.ConsumedFrames = 0;
            }
        }

        private void OnTrackEnded(Session sender, SessionEventArgs e)
        {
            _currentTrack = null;
            PlayNextInQueue();
        }

        public void Mute()
        {
            _player.Mute();
        }

        public void Unmute()
        {
            _player.Mute();
        }

        public async Task<Track> PlayNextInQueue()
        {
            if (_queue.Any())
            {
                Track next = _isShuffleOn ? _queue[_random.Next(0, _queue.Count - 1)] : Queue.First();
                _queue.Remove(next);
                _session.PlayerLoad(next);
                await Play();
                await SaveQueue();

                return next;
            }
            else
            {
                await SetCurrentTrack(null);
            }
            return null;
        }

        public async Task<int> ClearQueue()
        {
            int count = _queue.Count;
            if(count > 0)
            {
                _queue.Clear();
                await SaveQueue();
            }
            return count;
        }

        public async Task<int> RemoveFromQueue(string pattern)
        {
            int count = Queue.Count();

            _queue.RemoveAll(i => i.GetDisplayName().ToLowerInvariant().Contains(pattern));
            await SaveQueue();

            return _queue.Count - count;
        }


        private async Task SaveQueue()
        {
            await _robot.Brain.Set("SpotifyQueue", Queue.Select(t => t.GetLink().ToString()).ToArray());
        }

        private async Task LoadQueue()
        {
            var queue = await _robot.Brain.Get<string[]>("SpotifyQueue");
            if (queue == null)
            {
                return;
            }
            _queue.Clear();
            await Login();
            foreach (var trackUrl in queue)
            {
                Link link = _session.ParseLink(trackUrl);
                AddToQueue(await link.AsTrack());
            }
        }

        public async Task<bool> Play()
        {
            if (CurrentTrack != null)
            {
                _session.PlayerPlay();
                OnTrackChanged(CurrentTrack);
            }
            else
            {
                return (await PlayNextInQueue()) != null;
            }
            return false;
        }

        public async Task<string> PlayLink(string spotifyLink)
        {
            var link = _session.ParseLink(spotifyLink);
            if (link.Type == LinkType.Album)
            {
                return await PlayAlbum(await link.AsAlbum());
            }
            else if (link.Type == LinkType.Playlist)
            {
                return await PlayPlaylist(await link.AsPlaylist());
            }
            else if (link.Type == LinkType.Track)
            {
                await Play(await link.AsTrack());
            }
            return null;
        }

        public async Task<string> QueueLink(string spotifyLink)
        {
            // We have a link so process as such
            var link = _session.ParseLink(spotifyLink);
            if (link.Type == LinkType.Album)
            {
                return await QueueUpAlbum(await link.AsAlbum());
            }
            if (link.Type == LinkType.Playlist)
            {
                Playlist playlist = await link.AsPlaylist();
                return await QueueUpPlaylist(playlist);
            }
            if (link.Type == LinkType.Track)
            {
                return await QueueUpTrack(await link.AsTrack(), true);
            }
            return null;
        }

        public async Task Pause()
        {
            _session.PlayerPause();
            if (CurrentTrack != null)
            {
                await _robot.Adapter.Topic(LoungeRoom, string.Concat("Paused - ", CurrentTrack.GetDisplayName()));
            }
        }

        private async Task SetCurrentTrack(Track track)
        {
            _currentTrack = track;
            if (LoungeRoom != null)
            {
                if (CurrentTrack == null)
                {
                    await _robot.Adapter.Topic(LoungeRoom, string.Concat("Stopped - {0} items in queue", _queue.Count));
                }
                else
                {
                    await _robot.Adapter.Topic(LoungeRoom, string.Concat("Now playing - ", CurrentTrack.GetDisplayName()));
                }
            }
        }

        public async Task<Link> ParseLink(string query)
        {
            await Login();
            return _session.ParseLink(query);
        }


        public void SetVolume(int level)
        {
            _player.SetVolume(level);
        }

        public void TurnUpVolume(int amount)
        {
            _player.TurnUp(amount);
        }

        public void TurnDownVolume(int amount)
        {
            _player.TurnDown(amount);
        }


        protected virtual void OnTrackChanged(Track e)
        {
            EventHandler<Track> handler = TrackChanged;
            if (handler != null) handler(this, e);
        }
    }

}