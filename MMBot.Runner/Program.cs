using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMBot.Jabbr;
using MMBot.Scripts;

namespace MMBot.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter the password for mmbot in jabbr");
            var password = ReadPassword();
            
            if (string.IsNullOrEmpty(password))
            {
                return;
            }
            
            var robot = Robot.Create<JabbrAdapter>();

            // If not configured via dictionary then matching environment vars will be used

            robot.Configure(
                new Dictionary<string, string>
                {
                    {"HUBOT_JABBR_HOST", "https://jabbr.net/"},
                    {"HUBOT_JABBR_NICK", "mmbot"},
                    {"HUBOT_JABBR_PASSWORD", password},
                    {"HUBOT_JABBR_ROOMS", "mmbottest"}
                });

            //TODO: Discover scripts
            new Ping().Register(robot);

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
