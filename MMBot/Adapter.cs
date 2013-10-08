using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Logging;

namespace MMBot
{
    public abstract class Adapter
    {
        protected Robot Robot { get; private set; }

        protected ILog Logger { get; private set; }

        protected Adapter(Robot robot, ILog logger)
        {
            Robot = robot;
            Logger = logger;
        }

        public virtual Task Send(Envelope envelope, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Emote(Envelope envelope, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Reply(Envelope envelope, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Topic(Envelope envelope, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Topic(string roomName, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Play(Envelope envelope, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public abstract Task Run();

        public abstract Task Close();
        

        public virtual void Receive(Message message)
        {
            Robot.Receive(message);
        }


    }
}