using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMBot.Jabbr;
using MMBot.Scripts;
using MMBot.Spotify;

namespace MMBot.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new Dictionary<string, string>();

            if (Environment.GetEnvironmentVariable("HUBOT_JABBR_HOST") == null)
            {
                Console.WriteLine("Please enter the password for mmbot in jabbr");
                var password = ReadPassword();

                if (string.IsNullOrEmpty(password))
                {
                    return;
                }

                config = new Dictionary<string, string>
                {
                    {"HUBOT_JABBR_HOST", "https://jabbr.net/"},
                    {"HUBOT_JABBR_NICK", "mmbot"},
                    {"HUBOT_JABBR_PASSWORD", password},
                    {"HUBOT_JABBR_ROOMS", "mmbottest,markermetro"},
                    //{"HUBOT_TEAMCITY_USERNAME", "buildadmin"},
                    //{"HUBOT_TEAMCITY_PASSWORD", "**********"},
                    //{"HUBOT_TEAMCITY_HOSTNAME", "buildserver"},
                };
            }

            // If not configured via dictionary then matching environment vars will be used
            
            var robot = Robot.Create<JabbrAdapter>("mmbot", config);
            
            //TODO: Discover scripts
            
            robot.LoadScripts(typeof (Robot).Assembly);
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
