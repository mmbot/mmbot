using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Common.Logging;

namespace MMBot.Tests
{
    public class StubAdapter : Adapter
    {
        private readonly List<Tuple<Envelope, string[]>> _messages = new List<Tuple<Envelope, string[]>>();
        private ReplaySubject<Tuple<Envelope, string[]>> _messagesObservable = new ReplaySubject<Tuple<Envelope, string[]>>();

        public StubAdapter(ILog logger, string adapterId)
            : base(logger, adapterId)
        {
        }

        public IEnumerable<Tuple<Envelope, string[]>> Messages
        {
            get { return _messages; }
        }

        public IObservable<Tuple<Envelope, string[]>> MessagesObservable
        {
            get { return _messagesObservable; }
        }

        public override async Task Run()
        {
            
        }

        public override async Task Close()
        {
            
        }

        public void SimulateMessage(string user, string message)
        {
            Robot.Receive(new TextMessage(Robot.GetUser(user, user, "testRoom", Id), message));
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
            var payload = Tuple.Create(envelope, messages);
            _messages.Add(payload);
            _messagesObservable.OnNext(payload);
            return base.Send(envelope, messages);
        }

        public async Task<IEnumerable<Tuple<Envelope, string[]>>> GetEmittedMessages(int count)
        {
            return await GetEmittedMessages(count, TimeSpan.FromMilliseconds(200));
        }

        public async Task<IEnumerable<Tuple<Envelope, string[]>>> GetEmittedMessages(int count, TimeSpan timeout)
        {
            return await MessagesObservable.Buffer(timeout, count).FirstAsync();
        }
    }
}