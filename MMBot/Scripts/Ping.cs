using System;

namespace MMBot.Scripts
{
    public class Ping : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@"PING$",msg => msg.Send("PONG"));

            robot.Respond(@"ECHO (.*)$", msg => msg.Send(msg.Match[0].Value));

            robot.Respond(@"TIME$", msg => msg.Send(string.Format("Server time is: {0}", DateTime.Now.ToString("U"))));

            robot.Respond(@"DIE$", msg => msg.Send(msg.Match[0].Value));
        }
    }

    public interface IMMBotScript
    {
        void Register(Robot robot);
    }
}