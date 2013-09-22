using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

namespace MMBot.Adapters
{
    public class Shell : Adapter
    {
        private readonly Robot _robot;

        public Shell(Robot robot) : base(robot)
        {
            _robot = robot;
        }

        public override async Task Send(Envelope envelope, params string[] messages)
        {
            await base.Send(envelope, messages);

            messages.ForEach(Console.WriteLine);
        }

        public override async Task Emote(Envelope envelope, params string[] messages)
        {
            await base.Emote(envelope, messages);
            await Send(envelope, messages.Select(m => string.Format("* {0}", m)).ToArray());
        }

        public override async Task Reply(Envelope envelope, params string[] messages)
        {
            await base.Reply(envelope, messages);
            await Send(envelope, messages.Select(m => string.Format("{0}: {1}", envelope.User.Name, m)).ToArray());
        }

        public override Task Run()
        {
            return TaskAsyncHelper.Empty;
        }

        public override Task Close()
        {
            return TaskAsyncHelper.Empty;
        }
    }
}