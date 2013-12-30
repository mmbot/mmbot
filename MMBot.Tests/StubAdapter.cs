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

        public StubAdapter(Robot robot, ILog logger, string adapterId)
            : base(robot, logger, adapterId)
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
            Robot.Receive(new TextMessage(new User(user, user, new string[0], "testRoom", Id), message, null));
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