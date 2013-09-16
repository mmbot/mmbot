using System;
using System.Collections;
using System.Linq;

namespace MMBot.Adapters
{
    public class Shell : Adapter
    {
        public Shell(Robot robot) : base(robot)
        {
        }

        public override void Send(Envelope envelope, params string[] messages)
        {
            base.Send(envelope, messages);

            messages.ForEach(Console.WriteLine);
        }

        public override void Emote(Envelope envelope, params string[] messages)
        {
            base.Emote(envelope, messages);
            Send(envelope, messages.Select(m => string.Format("* {0}", m)).ToArray());
        }

        public override void Reply(Envelope envelope, params string[] messages)
        {
            base.Reply(envelope, messages);
            Send(envelope, messages.Select(m => string.Format("{0}: {1}", envelope.User.Name, m)).ToArray());
        }

        public override void Run()
        {
            base.Run();
        }
    }
}