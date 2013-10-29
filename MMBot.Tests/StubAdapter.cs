using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Logging;

namespace MMBot.Tests
{
    public class StubAdapter : Adapter
    {
        private readonly List<Tuple<Envelope, string[]>> _messages = new List<Tuple<Envelope, string[]>>();

        public StubAdapter(Robot robot, ILog logger, string adapterId)
            : base(robot, logger, adapterId)
        {
        }

        public IEnumerable<Tuple<Envelope, string[]>> Messages
        {
            get { return _messages; }
        }

        public override async Task Run()
        {
            
        }

        public override async Task Close()
        {
            
        }

        public void SimulateMessage(string user, string message)
        {
            Robot.Receive(new TextMessage(new User(user, user, new string[0], "testRoom", Id), message, null));
        }

        public override Task Send(Envelope envelope, params string[] messages)
        {
            _messages.Add(Tuple.Create(envelope, messages));
            return base.Send(envelope, messages);
        }
    }
}