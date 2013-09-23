using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MMBot.Adapters;
using MMBot.Scripts;
using SpotiFire;

namespace MMBot.Spotify
{
    public class SpotifyPlayer : IMMBotScript
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
        private Robot _robot;
        private List<Track> _queue = new List<Track>();
        private Track _currentTrack = null;
        private string _loungeRoom;

        private async Task<bool> Login(Robot robot, IResponse<TextMessage> msg)
        {
            if (_session != null && _session.ConnectionState != ConnectionState.Disconnected &&
                _session.ConnectionState != ConnectionState.Undefined)
            {
                return true;
            }

            string errorMessage = null;
            try
            {
                if (string.IsNullOrEmpty(robot.GetConfigVariable("MMBOT_SPOTIFY_USERNAME")))
                {
                    await
                        msg.Send(
                            "Spotify is not configured. You must supply the MMBOT_SPOTIFY_USERNAME and MMBOT_SPOTIFY_PASSWORD environment variables");
                    return false;
                }

                _isShuffleOn = await _robot.Brain.Get<bool>("SPOTIFY_SHUFFLE");

                _session = await SpotiFire.Spotify.CreateSession(key, cache, settings, userAgent);

                _session.MusicDelivered += OnMusicDelivered;
                _session.EndOfTrack += OnTrackEnded;
                
                var loginResult = await _session.Login(robot.GetConfigVariable("MMBOT_SPOTIFY_USERNAME"),
                    robot.GetConfigVariable("MMBOT_SPOTIFY_PASSWORD"), false);

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
                await msg.Send(string.Format("Could not login to Spotify: {0}", errorMessage));
                return false;
            }
            return true;
        }


        public void Register(Robot robot)
        {
            _robot = robot;
            _loungeRoom = _robot.GetConfigVariable("MMBOT_SPOTIFY_LOUNGE");
            

            robot.Respond(@"spotify shuffle( on| off)?", async msg =>
            {
                var off = msg.Match[1].ToLowerInvariant().Trim() == "off";
                _isShuffleOn = !off;
                await msg.Send(string.Format("Shuffle is {0}", off ? "OFF" : "ON"));

                await _robot.Brain.Set("SPOTIFY_SHUFFLE", true);
            });

            robot.Respond(@"spotify play( album)?( .*)?", async msg =>
            {
                if(!await Login(robot, msg)) return;

                bool isAlbum = !string.IsNullOrEmpty(msg.Match[1]);
                string query = msg.Match[2].Trim();

                if (string.IsNullOrWhiteSpace(query))
                {
                    if (_currentTrack != null)
                    {
                        _session.PlayerPlay();
                        await SetCurrentTrack(_currentTrack);
                    }
                    else
                    {
                        var next = await PlayNextInQueue();
                        if (next != null)
                        {
                            await msg.Send(string.Format("Playing {0}", next.GetDisplayName()));
                        }
                    }
                }
                else
                {
                    if (_spotifyLinkRegex.IsMatch(query))
                    {
                        // We have a link so process as such
                        var link = _session.ParseLink(query);
                        if (link.Type == LinkType.Album)
                        {
                            await PlayAlbum(await link.AsAlbum(), msg);
                        }
                        else if (link.Type == LinkType.Playlist)
                        {
                            await PlayPlaylist(await link.AsPlaylist(), msg);
                        }
                        else if (link.Type == LinkType.Track)
                        {
                            await Play(await link.AsTrack(), msg);
                        }
                    }
                    else
                    {
                        if (isAlbum)
                        {
                            var album = await SearchForAlbum(robot, msg, query);
                            if (album != null)
                            {
                                await PlayAlbum(album, msg);
                            }
                        }
                        else
                        {
                            var track = await SearchForTrack(robot, msg, query);
                            if (track != null)
                            {
                                await Play(track, msg);
                            }
                        }
                    }
                }
            });

            robot.Respond(@"spotify (en)?queue( album)? (.*)", async msg =>
            {
                if (!await Login(robot, msg)) return;
                bool isAlbum = !string.IsNullOrEmpty(msg.Match[1]);
                string query = msg.Match[2];

                if (_spotifyLinkRegex.IsMatch(query))
                {
                    // We have a link so process as such
                    var link = _session.ParseLink(query);
                    if (link.Type == LinkType.Album)
                    {
                        var album = await link.AsAlbum();
                        await QueueUpAlbum(album, msg);
                    }
                    else if (link.Type == LinkType.Playlist)
                    {
                        Playlist playlist = await link.AsPlaylist();
                        await QueueUpPlaylist(playlist, msg);
                    }
                    else if (link.Type == LinkType.Track)
                    {
                        await QueueUpTrack(await link.AsTrack(), msg, true);
                    }
                }
                else
                {
                    // We just have a query so search
                    if (isAlbum)
                    {
                        var album = await SearchForAlbum(robot, msg, query);
                        if (album != null)
                        {
                            await QueueUpAlbum(album, msg);
                        }
                    }
                    else
                    {
                        var track = await SearchForTrack(robot, msg, query);
                        await QueueUpTrack(track, msg, true);
                    }
                }
            });

            robot.Respond(@"spotify show( album| artist| playlist)? (.*)", async msg =>
            {
                if (!await Login(robot, msg)) return;

                bool isAlbum = msg.Match[1].Trim().ToLowerInvariant() == "album";
                bool isArtist = msg.Match[1].Trim().ToLowerInvariant() == "artist";
                bool isPlaylist = msg.Match[1].Trim().ToLowerInvariant() == "playlist";

                string query = msg.Match[2].Trim();


                if (_spotifyLinkRegex.IsMatch(query))
                {
                    // We have a link so process as such
                    var link = _session.ParseLink(query);
                    if (link.Type == LinkType.Album)
                    {
                        await ListAlbumTracks(await link.AsAlbum(), msg);
                    }
                    else if (link.Type == LinkType.Playlist)
                    {
                        await ListPlaylistTracks(await link.AsPlaylist(), msg);
                    }
                    else if (link.Type == LinkType.Track)
                    {
                        await msg.Send("Track: " + (await link.AsTrack()).GetDisplayName());
                    }
                }
                else
                {
                    if (isAlbum)
                    {
                        var album = await SearchForAlbum(robot, msg, query);
                        if (album != null)
                        {
                            await ListAlbumTracks(album, msg);
                        }
                    }
                    else if(isArtist)
                    {
                        var artist = await SearchForArtist(robot, msg, query);
                        if (artist != null)
                        {
                            await ListArtistAlbums(artist, msg);
                        }

                    }
                    else if (isPlaylist)
                    {
                        var playList = await SearchForPlaylist(robot, msg, query);
                        if (playList != null)
                        {
                            await ListPlaylistTracks(playList, msg);
                        }
                    }
                    else
                    {
                        await msg.Send("Please specify album/artist/playlist. e.g. spotify show artist Journey");
                    }

                }
            });

            robot.Respond(@"spotify clear queue", async msg =>
            {

                var count = _queue.Count;
                _queue.Clear();
                await SaveQueue();

                await msg.Send(string.Format("{0} items have been cleared from the queue.", count));
            });


            robot.Respond(@"spotify next", async msg =>
            {
                if (!await CheckForPlayingSession(msg)) return;
                
                if (!_queue.Any())
                {
                    await msg.Send("There is no next track");
                    return;
                }

                _session.PlayerPause();
                var next = await PlayNextInQueue();
                await msg.Send(string.Format("Playing {0}", next.GetDisplayName()));
                await SaveQueue();
            });

            robot.Respond(@"(spotify )?(stop|pause)", async msg =>
            {
                if (!await CheckForPlayingSession(msg)) return;
                _session.PlayerPause();
            });
            
            robot.Respond(@"mute", async msg =>
            {
                if (!await CheckForPlayingSession(msg)) return;
                _player.Mute();
            });

            robot.Respond(@"unmute", async msg =>
            {
                if (!await CheckForPlayingSession(msg)) return;
                _player.Unmute();
            });

            robot.Respond(@"spotify show queue", async msg =>
            {
                if (!await Login(robot, msg)) return;
                if (_queue == null || !_queue.Any())
                {
                    await msg.Send("There are no tracks in the queue");
                    return;
                }


                IEnumerable<string> queue = _queue.Take(20).Select(item => item.GetDisplayName()).ToArray();
                if (_queue.Count > 20)
                {
                    queue = queue.Concat(new[] {string.Format("+ {0} not listed", _queue.Count - 20)});
                }
                await msg.Send(string.Join(Environment.NewLine, queue.ToArray()));
                
            });
            
            robot.Respond(@"spotify remove (.*) from( the)? queue", async msg =>
            {
                if (!await Login(robot, msg)) return;
                if (_queue == null || !_queue.Any())
                {
                    await msg.Send("There are no tracks in the queue");
                    return;
                }

                int count = _queue.Count;
                
                _queue.RemoveAll(i => i.GetDisplayName().ToLowerInvariant().Contains(msg.Match[1]));
                await SaveQueue();

                if (_queue.Count == count)
                {
                    await msg.Send("There were no matching tracks in the queue");
                }
                else
                {
                    await msg.Send(string.Format("{0} tracks were removed from the queue", _queue.Count - count));
                }
            });

            robot.Respond(@"(turn|crank) it (up|down)( to (\d+))?", async msg =>
            {
                if (!await CheckForPlayingSession(msg)) return;
                
                string direction = msg.Match[2].Trim();
                string amount = msg.Match[4].Trim();

                if (!string.IsNullOrWhiteSpace(amount))
                {
                    _player.SetVolume(int.Parse(amount));
                }
                if (direction.ToLowerInvariant() == "up")
                {
                    _player.TurnUp(10);
                }
                else
                {
                    _player.TurnDown(10);
                }
                
            });

            robot.Respond(@"spotify ship it", async msg =>
            {
                if (!await Login(robot, msg)) return;

                Link link = _session.ParseLink("spotify:track:77NNZQSqzLNqh2A9JhLRkg");
                await Play(await link.AsTrack(), null);
                await Giphy.GifMe(_robot, "winning", msg);
            });

        }



        private async Task QueueUpAlbum(Album album, IResponse<TextMessage> msg)
        {
            AlbumBrowse albumBrowse = album.Browse();
            albumBrowse.Tracks.ForEach(t => _queue.Add(t));
            await
                msg.Send(string.Format("Queued up {0} tracks from album {1} by {2}",
                    albumBrowse.Tracks.Count, albumBrowse.Album.Name, albumBrowse.Artist.Name));
            await SaveQueue();
        }

        private async Task QueueUpPlaylist(Playlist playlist, IResponse<TextMessage> msg)
        {
            playlist.Tracks.ForEach(t => _queue.Add(t));
            await
                msg.Send(string.Format("Queued up {0} tracks from playlist {1}", playlist.Tracks.Count,
                    playlist.Name));
            await SaveQueue();
        }

        private async Task PlayAlbum(Album album, IResponse<TextMessage> msg)
        {
            AlbumBrowse albumBrowse = await album.Browse();
            await Play(albumBrowse.Tracks[0], msg);
            await PrependToQueue(albumBrowse.Tracks.Skip(1));
            await
                msg.Send(string.Format("Queued up {0} tracks from album {1} by {2}",
                    albumBrowse.Tracks.Count, albumBrowse.Album.Name, albumBrowse.Artist.Name));
            await SaveQueue();
        }

        private async Task PlayPlaylist(Playlist playlist, IResponse<TextMessage> msg)
        {
            await Play(playlist.Tracks[0], msg);
            await PrependToQueue(playlist.Tracks.Skip(1));
            await
                msg.Send(string.Format("Queued up {0} tracks from playlist {1}", playlist.Tracks.Count,
                    playlist.Name));
            await SaveQueue();
        }

        private async Task PrependToQueue(IEnumerable<Track> tracks)
        {
            _queue.InsertRange(0, tracks);
            await SaveQueue();
        }

        private async Task QueueUpTrack(Track track, IResponse<TextMessage> msg, bool showMessage)
        {
            if (track != null)
            {
                if(showMessage)
                {
                    await msg.Send(string.Format("Queued up {0}. It is currently number #{1} in the queue",
                        track.GetDisplayName(), _queue.Count + 1));
                }
                _queue.Add(track);
                await SaveQueue();
            }
        }

        private async Task<bool> CheckForPlayingSession(IResponse<TextMessage> msg)
        {
            if (_session == null)
            {
                await msg.Send("Not playing anything right now");
                return false;
            }
            return true;
        }

        private async Task<Track> SearchForTrack(Robot robot, IResponse<TextMessage> msg, string query)
        {
            var tracks = await _session.SearchTracks(query, 0, 1);
            
            if (!tracks.Tracks.Any())
            {
                await msg.Send(string.Format("Could not find any tracks matching '{0}'", query));
                msg.Message.Done = true;
                return null;
            }

            return tracks.Tracks[0];
        }

        private async Task<Album> SearchForAlbum(Robot robot, IResponse<TextMessage> msg, string query)
        {
            var albums = await _session.SearchAlbums(query, 0, 1);

            if (!albums.Albums.Any())
            {
                await msg.Send(string.Format("Could not find any tracks matching '{0}'", query));
                msg.Message.Done = true;
                return null;
            }

            return albums.Albums[0];
        }

        private async Task<Playlist> SearchForPlaylist(Robot robot, IResponse<TextMessage> msg, string query)
        {
            var playlists = await _session.SearchPlaylist(query, 0, 1);

            if (!playlists.Albums.Any())
            {
                await msg.Send(string.Format("Could not find any playlists matching '{0}'", query));
                msg.Message.Done = true;
                return null;
            }

            return playlists.Playlists[0];
        }

        private async Task<Artist> SearchForArtist(Robot robot, IResponse<TextMessage> msg, string query)
        {
            var artists = await _session.SearchArtists(query, 0, 1);

            if (!artists.Artists.Any())
            {
                await msg.Send(string.Format("Could not find any artists matching '{0}'", query));
                msg.Message.Done = true;
                return null;
            }

            return artists.Artists[0];
        }

        private async Task Play(Track track, IResponse<TextMessage> msg)
        {

            if(msg != null)
            {
                await msg.Send(string.Format("Playing {0}", track.GetDisplayName()));
            }

            _session.PlayerUnload();
            _session.PlayerLoad(track);
            _session.PlayerPlay();

            await SetCurrentTrack(track);
        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot spotify play <query> -  Plays the first matching track from spotify.",
                "mmbot spotify play album <query> -  Plays the first matching album from spotify.",
                "mmbot spotify play <spotifyUri> -  Plays the track(s) from the spotify URI (supports tracks, albums and playlists).",
                "mmbot spotify pause - Pauses playback",
                "mmbot spotify queue <query> -  Queues the first matching track from spotify.",
                "mmbot spotify queue album <query> -  Queues the first matching album from spotify.",
                "mmbot spotify queue <spotifyUri> -  Queues the track(s) from the spotify URI (supports tracks, albums and playlists).",
                "mmbot spotify show queue",
                "mmbot spotify show artist|album|playlist <name> - Shows the details of the first matching artist, album or playlist",
                "mmbot spotify shuffle on|off - turn on or off shuffle mode",
                "mmbot spotify remove <query> from queue - Removes matching tracks from the queue",
                "mmbot spotify clear queue - clears the play queue",
                "mmbot spotify next - Skips to the next track in the queue.",
                "mmbot turn it up [to 66] - crank it baby, optionally provide the volume out of 100",
                "mmbot turn it down [to 11] - shhhh I'm thinking, optionally provide the volume out of 100",
                "mmbot mute/unmute - turn the volume on/off"
            };
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

        private async Task<Track> PlayNextInQueue()
        {
            if (_queue.Any())
            {
                Track next = _isShuffleOn ? _queue[_random.Next(0, _queue.Count - 1)] : _queue.First();
                _queue.Remove(next);
                _session.PlayerLoad(next);
                _session.PlayerPlay();
                await SetCurrentTrack(next);
                await SaveQueue();

                return next;
            }
            return null;
        }

        private async Task SaveQueue()
        {
            await _robot.Brain.Set("SpotifyQueue", _queue.Select(t => t.GetLink().ToString()).ToArray());
        }

        private async Task LoadQueue()
        {
            var queue = await _robot.Brain.Get<string[]>("SpotifyQueue");
            if (queue == null)
            {
                return;
            }
            _queue.Clear();
            await Login(_robot, null);
            foreach (var trackUrl in queue)
            {
                Link link = _session.ParseLink(trackUrl);
                _queue.Add(await link.AsTrack());
            }
        }

        private async Task SetCurrentTrack(Track track)
        {
            _currentTrack = track;
            if (_loungeRoom != null)
            {
                await _robot.Adapter.Topic(_loungeRoom, string.Concat("Now playing - ", _currentTrack.GetDisplayName()));
            }
        }
        
        private async Task ListAlbumTracks(Album album, IResponse<TextMessage> msg)
        {
            var browse = await album.Browse();
            var sb = new StringBuilder();
            sb.AppendFormat("Artist - {0}", album.Artist.Name);
            sb.AppendFormat("Album - {0}", album.Name);
            foreach (var track in browse.Tracks)
            {
                sb.AppendFormat("#{0}: {1}", track.Index, track.Name);
            }
        }

        private async Task ListPlaylistTracks(Playlist playlist, IResponse<TextMessage> msg)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Playlist Owner - {0}", playlist.Owner.DisplayName);
            sb.AppendFormat("Playlist Name - {0}", playlist.Name);
            foreach (var track in playlist.Tracks)
            {
                sb.AppendFormat("#{0}: {1}", track.Index, track.GetDisplayName());
            }
        }


        private async Task ListArtistAlbums(Artist artist, IResponse<TextMessage> msg)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Artist - {0}", artist.Name);
            var browse = await artist.Browse(ArtistBrowseType.NoTracks);
            foreach (var album in browse.Albums)
            {
                sb.AppendFormat("{0} ({1})", album.Name, album.Year);
            }
        }
    }
}
