using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MMBot
{
    public abstract class Adapter
    {
        protected readonly Robot _robot;

        protected Adapter(Robot robot)
        {
            _robot = robot;
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

        public virtual Task Play(Envelope envelope, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Run()
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Close()
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual void Receive(Message message)
        {
            _robot.Receive(message);
        }


    }
}