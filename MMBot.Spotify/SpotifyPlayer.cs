using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MMBot.Scripts;
using SpotiFire;

namespace MMBot.Spotify
{
    public class SpotifyPlayer : IMMBotScript
    {
        private const string CLIENT_NAME = "MMBotSpotifyPlayer";
        private IPlayer _player = new NAudioPlayer();
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
        private Queue<Track> _queue = new Queue<Track>();

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
            robot.Respond(@"spotify play( .*)?", async msg =>
            {
                if(!await Login(robot, msg)) return;
                string query = msg.Match[1].Trim();
                if (string.IsNullOrWhiteSpace(query))
                {
                    _session.PlayerPlay();
                }
                else
                {
                    var track = await Search(robot, msg, query);
                    if (track != null)
                    {
                        await Play(track, msg);
                    }
                }
            });

            robot.Respond(@"spotify (en)?queue (.*)", async msg =>
            {
                if (!await Login(robot, msg)) return;
                string query = msg.Match[2];
                var track = await Search(robot, msg, query);
                if (track != null)
                {
                    await msg.Send(string.Format("Queued up {0}. It is currently number #{1} in the queue",
                        track.GetDisplayName(), _queue.Count + 1));
                    _queue.Enqueue(track);
                    await SaveQueue();
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
                await Play(_queue.Dequeue(), msg);
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

        private async Task<Track> Search(Robot robot, IResponse<TextMessage> msg, string query)
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

        private async Task Play(Track track, IResponse<TextMessage> msg)
        {

            if(msg != null)
            {
                await msg.Send(string.Format("Playing {0}", track.GetDisplayName()));
            }

            _session.PlayerUnload();
            _session.PlayerLoad(track);
            _session.PlayerPlay();
        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot spotify play <query> -  Plays the first matching track from spotify.",
                "mmbot spotify pause - Pauses playback",
                "mmbot spotify queue <query> -  Queues the first matching track from spotify.",
                "mmbot spotify next - Skips to the next track in the queue.",
                "mmbot turn it up [to 66] - crank it baby, optionally provide the volume out of 100",
                "mmbot turn it down [to 11] - shhhh I'm thinking, optionally provide the volume out of 100",
                "mmbot mute/unmute - turn the volume on/off",
                "mmbot spotify clear queue - clears the play queue"
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
            if (_queue.Any())
            {
                _session.PlayerLoad(_queue.Dequeue());
                _session.PlayerPlay();
                SaveQueue();
            }
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
                _queue.Enqueue(link.AsTrack());
            }
        }

    }
}
