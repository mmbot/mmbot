using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;

namespace MMBot.Adapters
{
    public class ConsoleAdapter : Adapter
    {
        private User _user;

        public ConsoleAdapter(Robot robot, ILog logger, string adapterId)
            : base(robot, logger, adapterId)
        {
        }

        public async override Task Run()
        {
            Task.Run(() => StartListening());
        }

        private void StartListening()
        {
            _user = Robot.GetUser("ConsoleUser", "ConsoleUser", "Console", Id);
            
            while(true)
            {
                var message = Console.ReadLine();

                if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Environment.Exit(0);
                    return;
                }
                Robot.Receive(new TextMessage(_user, message));
            }
        }

        public override async Task Send(Envelope envelope, params string[] messages)
        {
            await base.Send(envelope, messages);

            foreach (var message in messages.Where(message => !string.IsNullOrWhiteSpace(message)))
            {
                Console.WriteLine(message);
            }
        }

        public override async Task Close()
        {
            //Something?
        }
    }
}
