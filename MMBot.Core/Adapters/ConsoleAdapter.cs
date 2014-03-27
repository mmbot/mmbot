using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;

namespace MMBot.Adapters
{
    public class ConsoleAdapter : Adapter
    {
        private User _user;
        private Task _listeningTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ConsoleAdapter(ILog logger, string adapterId)
            : base(logger, adapterId)
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async override Task Run()
        {
            _listeningTask = Task.Factory.StartNew(() => 
            {
                StartListening(_cancellationTokenSource.Token);    
            }, _cancellationTokenSource.Token);
        }

        private async Task StartListening(CancellationToken token)
        {
            _user = Robot.GetUser("ConsoleUser", "ConsoleUser", "Console", Id);
            
            while(true)
            {
                var message = Console.ReadLine();

                if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Environment.Exit(0);
                }
                Robot.Receive(new TextMessage(_user, message));

                if (token.IsCancellationRequested)
                {
                    return;
                }
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
            _cancellationTokenSource.Cancel();
            await _listeningTask;
        }
    }
}
