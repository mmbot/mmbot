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

            JabbrAdapter.Configure("https://jabbr.net/", "mmbot", password, "mmbottest");

            var robot = Robot.Create<JabbrAdapter>();

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
