using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using MMBot.HipChat;
using MMBot.Jabbr;
using MMBot.Scripts;
using MMBot.Spotify;
using MMBot.XMPP;

namespace MMBot.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new Dictionary<string, string>();

            if (Environment.GetEnvironmentVariable("MMBOT_JABBR_HOST") == null && Environment.GetEnvironmentVariable("MMBOT_HIPCHAT_HOST") == null)
            {
                Console.WriteLine("Please enter the password for mmbot");
                var password = ReadPassword();

                if (string.IsNullOrEmpty(password))
                {
                    return;
                }

                config = new Dictionary<string, string>
                {
                    {"MMBOT_JABBR_HOST", "https://jabbr.net/"},
                    {"MMBOT_JABBR_NICK", "mmbot"},
                    {"MMBOT_JABBR_PASSWORD", password},
                    {"MMBOT_JABBR_ROOMS", "mmbottest,markermetro"},
                    {"MMBOT_XMPP_HOST", "myemaildomain.com"},
                    {"MMBOT_XMPP_CONNECT_HOST", "talk.google.com"},
                    {"MMBOT_XMPP_USERNAME", "myemailusername"},
                    {"MMBOT_XMPP_PASSWORD", password},
                    {"MMBOT_XMPP_RESOURCE", "Home"},
                    
                    //{"MMBOT_TEAMCITY_USERNAME", "buildadmin"},
                    //{"MMBOT_TEAMCITY_PASSWORD", "**********"},
                    //{"MMBOT_TEAMCITY_HOSTNAME", "buildserver"},
                    {"MMBOT_HIPCHAT_HOST", "chat.hipchat.com"},
                    {"MMBOT_HIPCHAT_CONFHOST", "conf.hipchat.com"},
                    {"MMBOT_HIPCHAT_NICK", "mmbot"},
                    {"MMBOT_HIPCHAT_ROOMNICK", "mmbot Bot"},
                    {"MMBOT_HIPCHAT_USERNAME", "70126_494082"},
                    {"MMBOT_HIPCHAT_PASSWORD", password},
                    {"MMBOT_HIPCHAT_ROOMS", "70126_mmbot"},
                };
            }

            // If not configured via dictionary then matching environment vars will be used

            // Uncomment the appropriate line below to use Jabbr or HipChat
            var robot = Robot.Create<JabbrAdapter>("mmbot", config, LoggerConfigurator.GetConsoleLogger(LogLevel.Info));
            //var robot = Robot.Create<XmppAdapter>("mmbot", config, LoggerConfigurator.GetConsoleLogger(LogLevel.Info));
            //var robot = Robot.Create<HipChatAdapter>("mmbot", config, LoggerConfigurator.GetConsoleLogger(LogLevel.Info));

            robot.LoadScripts(typeof(SpotifyPlayerScripts).Assembly);

            robot.Run();

            Console.ReadKey();
        }


        private static string ReadPassword()
        {
            var pass = new StringBuilder();
            char key;
            while ((key = Console.ReadKey(true).KeyChar) != '\r')
            {

                if (key == '\b' && pass.Length > 0)
                {

                    Console.Write(key + "" + key);

                    pass = pass.Remove(pass.Length - 1, 1);

                } else if (Char.IsLetterOrDigit(key))
                {

                    Console.Write("*");

                    pass = pass.Append(key);

                }

            }
            Console.WriteLine();
            return pass.ToString();
        }
    }
}
