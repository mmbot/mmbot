using System;
using System.Collections.Generic;
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

        public override Task Run()
        {
            _listeningTask = Task.Factory.StartNew(() =>
            {
                StartListening(_cancellationTokenSource.Token);
            }, _cancellationTokenSource.Token);
            return Task.FromResult(0);
        }

        private void StartListening(CancellationToken token)
        {
            _user = Robot.GetUser("ConsoleUser", "ConsoleUser", "Console", Id);

            while (!token.IsCancellationRequested)
            {
                var message = Console.ReadLine();

                if (message != null && message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Environment.Exit(0);
                }
                Robot.Receive(new TextMessage(_user, message));
            }
        }

        public override Task Send(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages)
        {
            foreach (var message in messages.Where(message => !string.IsNullOrWhiteSpace(message)))
            {
                Console.WriteLine(message);
            }

            return Task.FromResult(0);
        }

        public override Task Close()
        {
            _cancellationTokenSource.Cancel();
            if (_listeningTask != null) return _listeningTask;
            return Task.FromResult(0);
        }
    }
}