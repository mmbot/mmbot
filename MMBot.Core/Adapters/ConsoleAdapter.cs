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
        private string _closeStatus = "running";

        public ConsoleAdapter(Robot robot, ILog logger, string adapterId)
            : base(robot, logger, adapterId)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }

        private Task _listeningTask;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        public async override Task Run()
        {
            _listeningTask = Task.Factory.StartNew(() => 
            {
                try
                {
                    StartListening(_cancellationToken);
                }
                catch (TaskCanceledException e)
                {
                                                                 
                }
            }, _cancellationToken);
            //Task.Run(() =>
            //{ _listeningTask = StartListening(); }, _cancellationToken);
        }

        private async Task StartListening(CancellationToken token)
        {
            _user = Robot.GetUser("ConsoleUser", "ConsoleUser", "Console", Id);
            
            while(true)// && _closeStatus != "closing")
            {
                var message = Console.ReadLine();

                if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Environment.Exit(0);
                    return;
                }
                Robot.Receive(new TextMessage(_user, message));

                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("---> ConsoleApater is cancelling.");
                    return;
                }
            }
            //_closeStatus = "closed";
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
            _closeStatus = "closing";

            try
            {
                Console.WriteLine("---> Waiting on ConsoleAdapter to cancel");
                _cancellationTokenSource.Cancel();
                await _listeningTask;
                Console.WriteLine("---> Console Adapter has been cancelled");
            }
            catch (TaskCanceledException ex)
            {
                //We asked to cancel it--just stop the even from bubbling.
            }
            catch (OperationCanceledException ex)
            {
                
            }
        }
    }
}
