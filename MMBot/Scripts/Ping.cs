using System;
using System.Diagnostics;

namespace MMBot.Scripts
{
    public class Ping : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@"PING$",msg => msg.Send("PONG"));

            robot.Respond(@"ECHO (.*)$", msg => msg.Send(msg.Match[0].Groups[1].Value));

            robot.Respond(@"TIME$", msg => msg.Send(string.Format("Server time is: {0} {1}", DateTime.Now.ToString("U"), TimeZoneInfo.Local.DisplayName)));

            robot.Respond(@"DIE$", msg => Environment.Exit(0));
        }
    }

    public interface IMMBotScript
    {
        void Register(Robot robot);
    }
}