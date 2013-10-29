using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Tester
{
    public class ConsoleAdapter : Adapter
    {
        private User _user;

        public ConsoleAdapter(Robot robot, ILog logger, string adapterId)
            : base(robot, logger, adapterId)
        {
            _user = new User("test", "test", new string[0], "testRoom", Id);
        }

        public async override Task Run()
        {
            Task.Run(() => StartListening());
        }

        private void StartListening()
        {
            while(true)
            {
                var message = Console.ReadLine();

                if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Environment.Exit(0);
                    return;
                }
                Robot.Receive(new TextMessage(_user, message, null));
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
