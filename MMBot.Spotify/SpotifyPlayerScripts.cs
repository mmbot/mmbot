using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MMBot.Adapters;
using MMBot.Scripts;
using SpotiFire;

namespace MMBot.Spotify
{
    public class SpotifyPlayerScripts : IMMBotScript
    {
        private readonly Regex _spotifyLinkRegex = new Regex(@"spotify:(album|track|user:[a-zA-Z0-9]+:playlist):[a-zA-Z0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private SpotifyPlayer _player;
        private Robot _robot;

        public void Register(Robot robot)
        {
            _robot = robot;
            _player = new SpotifyPlayer(robot);

            Observable.FromEventPattern<Track>(e => _player.TrackChanged += e, e => _player.TrackChanged -= e)
                .Select(s => Unit.Default)
                .Merge(
                    Observable.FromEventPattern<SpotifyPlayer.PlayerState>(e => _player.StateChanged += e,
                        e => _player.StateChanged -= e).Select(s => Unit.Default))
                        .Throttle(TimeSpan.FromMilliseconds(300))
                .Subscribe(a => UpdateLoungeTopic());

            _player.LoungeRoom = robot.GetConfigVariable("MMBOT_SPOTIFY_LOUNGE");
            

            robot.Respond(@"spotify shuffle( on| off)?", async msg =>
            {
                var off = msg.Match[1].ToLowerInvariant().Trim() == "off";
                if (off)
                {
                    await _player.SetShuffleOff();
                }
                else
                {
                    await _player.SetShuffleOn();
                }

                await msg.Send(string.Format("Shuffle is {0}", off ? "OFF" : "ON"));
            });

            robot.Respond(@"spotify play( album)?( .*)?", async msg =>
            {
                if(!await Login(msg)) return;

                bool isAlbum = !string.IsNullOrEmpty(msg.Match[1]);
                string query = msg.Match[2].Trim();

                if (string.IsNullOrWhiteSpace(query))
                {
                    await _player.Play();
                }
                else
                {
                    string message = null;
                    if (_spotifyLinkRegex.IsMatch(query))
                    {
                        // We have a link so process as such
                        message = await _player.PlayLink(query);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(query))
                        {
                            await msg.Send("Nothing to search for");
                            return;
                        }

                        if (isAlbum)
                        {
                            var album = await _player.SearchForAlbum(query);
                            if (album != null)
                            {
                                message = await _player.PlayAlbum(album);
                            }
                            else
                            {
                                await msg.Send(string.Format("Could not find any albums matching '{0}'", query));
                                msg.Message.Done = true;
                            }
                        }
                        else
                        {
                            // Search for a matching track
                            var track = await _player.SearchForTrack(query);
                            if (track != null)
                            {
                                await _player.Play(track);
                            }
                            else
                            {
                                await msg.Send(string.Format("Could not find any tracks matching '{0}'", query));
                                msg.Message.Done = true;
                            }
                        }
                    }
                    // Output the user message from the play request, if any
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        await msg.Send(message);
                    }
                }

            });

            robot.Respond(@"spotify (en)?queue( album)? (.*)", async msg =>
            {
                if (!await Login(msg)) return;
                bool isAlbum = !string.IsNullOrEmpty(msg.Match[2]);
                string query = msg.Match[3];
                string message = null;
                if (_spotifyLinkRegex.IsMatch(query))
                {
                    // We have a link so process as such
                    message = await _player.QueueLink(query);
                }
                else
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        await msg.Send("Nothing to search for");
                        return;
                    }

                    // We just have a query so search
                    if (isAlbum)
                    {
                        var album = await _player.SearchForAlbum(query);
                        if (album != null)
                        {
                            message = await _player.QueueUpAlbum(album);
                        }
                        else
                        {
                            await msg.Send(string.Format("Could not find any albums matching '{0}'", query));
                            msg.Message.Done = true;
                        }
                    }
                    else
                    {
                        var track = await _player.SearchForTrack(query);
                        if (track != null)
                        {
                            message = await _player.QueueUpTrack(track, true);
                        }
                        else
                        {                             
                            await msg.Send(string.Format("Could not find any tracks matching '{0}'", query));
                            msg.Message.Done = true;
                        }
                    }
                }
                // Output the user message from the play request, if any
                if (!string.IsNullOrWhiteSpace(message))
                {
                    await msg.Send(message);
                }
            });

            robot.Respond(@"spotify show( album| artist| playlist)? (.*)", async msg =>
            {
                if (!await Login(msg)) return;

                if (msg.Match[2].Trim().ToLowerInvariant() == "queue")
                {
                    return;
                }

                bool isAlbum = msg.Match[1].Trim().ToLowerInvariant() == "album";
                bool isArtist = msg.Match[1].Trim().ToLowerInvariant() == "artist";
                bool isPlaylist = msg.Match[1].Trim().ToLowerInvariant() == "playlist";

                string query = msg.Match[2].Trim();


                if (_spotifyLinkRegex.IsMatch(query))
                {
                    // We have a link so process as such
                    var link = await _player.ParseLink(query);
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
                    if (string.IsNullOrEmpty(query))
                    {
                        await msg.Send("Nothing to search for");
                        return;
                    }
                    if (isAlbum)
                    {
                        var album = await _player.SearchForAlbum(query);
                        if (album != null)
                        {
                            await ListAlbumTracks(album, msg);
                        }
                        else
                        {
                            await msg.Send(string.Format("Could not find any albums matching '{0}'", query));
                            msg.Message.Done = true;
                        }
                    }
                    else if(isArtist)
                    {
                        var artist = await _player.SearchForArtist(query);
                        if (artist != null)
                        {
                            await ListArtistAlbums(artist, msg);
                        }
                        else
                        {
                            await msg.Send(string.Format("Could not find any artists matching '{0}'", query));
                            msg.Message.Done = true;
                        }

                    }
                    else if (isPlaylist)
                    {
                        var playList = await _player.SearchForPlaylist(query);
                        if (playList != null)
                        {
                            await ListPlaylistTracks(playList, msg);
                        }
                        else
                        {
                            await msg.Send(string.Format("Could not find any playlists matching '{0}'", query));
                            msg.Message.Done = true;
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

                var count = await _player.ClearQueue();

                await msg.Send(string.Format("{0} items have been cleared from the queue.", count));
            });


            robot.Respond(@"spotify next", async msg =>
            {
                if (!await CheckForPlayingSession(msg)) return;
                
                if (!_player.Queue.Any())
                {
                    await msg.Send("There is no next track");
                    return;
                }

                await _player.Pause();
                var next = await _player.PlayNextInQueue();
                await msg.Send(string.Format("Playing {0}", next.GetDisplayName()));
            });

            robot.Respond(@"(spotify )?(stop|pause)", async msg =>
            {
                if (!await CheckForPlayingSession(msg)) return;
                await _player.Pause();
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
                if (!await Login(msg)) return;
                if (!_player.Queue.Any())
                {
                    await msg.Send("There are no tracks in the queue");
                    return;
                }


                IEnumerable<string> queue = _player.Queue.Take(20).Select(item => item.GetDisplayName()).ToArray();
                if (_player.Queue.Count() > 20)
                {
                    queue = queue.Concat(new[] { string.Format("+ {0} not listed", _player.Queue.Count() - 20) });
                }
                await msg.Send(string.Join(Environment.NewLine, queue.ToArray()));
                
            });
            
            robot.Respond(@"spotify remove (.*) from( the)? queue", async msg =>
            {
                if (!await Login(msg)) return;
                if (_player.Queue == null || !_player.Queue.Any())
                {
                    await msg.Send("There are no tracks in the queue");
                    return;
                }

                int count = await _player.RemoveFromQueue(msg.Match[1]);
                
                
                if (count == 0)
                {
                    await msg.Send("There were no matching tracks in the queue");
                }
                else
                {
                    await msg.Send(string.Format("{0} tracks were removed from the queue", count));
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
                    _player.TurnUpVolume(10);
                }
                else
                {
                    _player.TurnUpVolume(10);
                }
                
            });

            robot.Respond(@"spotify ship it", async msg =>
            {
                if (!await Login(msg)) return;

                await _player.PlayLink("spotify:track:77NNZQSqzLNqh2A9JhLRkg");
                
                await Giphy.GifMe(_robot, "winning", msg);
            });

        }

        private async Task UpdateLoungeTopic()
        {
            var stateDisplayText = "Disconnected";
            switch (_player.State)
            {
                case SpotifyPlayer.PlayerState.Disconnected:
                    break;
                case SpotifyPlayer.PlayerState.Stopped:
                    stateDisplayText = "Stopped";
                    break;
                case SpotifyPlayer.PlayerState.Paused:
                    stateDisplayText = "Paused";
                    break;
                case SpotifyPlayer.PlayerState.Playing:
                    stateDisplayText = "Now Playing";
                    break;
            }
            string message = _player.CurrentTrack != null && (_player.State == SpotifyPlayer.PlayerState.Playing || _player.State == SpotifyPlayer.PlayerState.Paused)
                ? string.Concat(stateDisplayText, " - ", _player.CurrentTrack.GetDisplayName())
                : stateDisplayText;

            await _robot.Adapter.Topic(_player.LoungeRoom, message);
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


        private async Task<bool> Login(IResponse<TextMessage> msg)
        {
            string errorMessage;
            try
            {
                await _player.Login();
                return true;
            }
            catch(Exception ex)
            {
                errorMessage = ex.Message;
            }
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                await msg.Send(errorMessage);
            }
            return false;
        }


        private async Task ListAlbumTracks(Album album, IResponse<TextMessage> msg)
        {
            var browse = await album.Browse();
            var sb = new StringBuilder();
            sb.AppendFormat("Artist - {0}\r\n", album.Artist.Name);
            sb.AppendFormat("Album - {0}\r\n", album.Name);
            foreach (var track in browse.Tracks)
            {
                sb.AppendFormat("#{0}: {1}\r\n", track.Index, track.Name);
            }
            await msg.Send(sb.ToString());
        }

        private async Task ListPlaylistTracks(Playlist playlist, IResponse<TextMessage> msg)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Playlist Owner - {0}\r\n", playlist.Owner.DisplayName);
            sb.AppendFormat("Playlist Name - {0}\r\n", playlist.Name);
            foreach (var track in playlist.Tracks)
            {
                sb.AppendFormat(" #{0}: {1}\r\n", track.Index, track.GetDisplayName());
            }
            await msg.Send(sb.ToString());
        }


        private async Task ListArtistAlbums(Artist artist, IResponse<TextMessage> msg)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Artist - {0}\r\n", artist.Name);
            var browse = await artist.Browse(ArtistBrowseType.NoTracks);
            sb.AppendFormat("Albums: \r\n");
            var albums = browse.Albums.Where(a => a.Type == AlbumType.Album && a.IsAvailable ).Distinct(new GenericEqualityComparer<Album>((x, y) => x.Name == y.Name && x.Year == y.Year, x => x.Name.GetHashCode())).ToArray();
            foreach (var album in albums.Take(30))
            {
                sb.AppendFormat("  {0} ({1})\r\n", album.Name, album.Year);
            }
            int totalAlbums = albums.Count();
            if(totalAlbums > 30)
            { 
                sb.AppendFormat(" (+{0} unlisted) \r\n", totalAlbums - 10);
            }
            await msg.Send(sb.ToString());
        }

        private async Task<bool> CheckForPlayingSession(IResponse<TextMessage> msg)
        {
            if (_player == null || _player.CurrentTrack == null)
            {
                await msg.Send("Not playing anything right now");
                return false;
            }
            return true;
        }
    }
}
