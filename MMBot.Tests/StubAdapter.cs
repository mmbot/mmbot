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
            Robot.Receive(new TextMessage(Robot.GetUser(user, user, "testRoom", Id), message, null));
        }

        public void SimulateEnter(string user)
        {
            Robot.Receive(new EnterMessage(Robot.GetUser(user, user, "testRoom", Id)));
        }

        public void SimulateLeave(string user)
        {
            Robot.Receive(new LeaveMessage(Robot.GetUser(user, user, "testRoom", Id)));
        }

        public void SimulateTopic(string user, string topic)
        {
            Robot.Receive(new TopicMessage(Robot.GetUser(user, user, "testRoom", Id), topic));
        }

        public override Task Send(Envelope envelope, params string[] messages)
        {
            _messages.Add(Tuple.Create(envelope, messages));
            return base.Send(envelope, messages);
        }
    }
}