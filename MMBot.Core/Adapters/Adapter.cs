using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Common.Logging;

namespace MMBot
{
    public abstract class Adapter : IAdapter, IMustBeInitializedWithRobot
    {
        protected Robot Robot { get; private set; }

        protected ILog Logger { get; private set; }
        public string Id { get; private set; }

        protected Adapter(ILog logger, string adapterId)
        {
            Logger = logger;
            Id = adapterId;
            Rooms = new Collection<string>();
            LogRooms = new Collection<string>();
        }

        public virtual void Initialize(Robot robot)
        {
            Robot = robot;
        }

        public virtual Task Send(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Emote(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Reply(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Topic(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Topic(string roomName, IDictionary<string, string> adapterArgs, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Play(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages)
        {
            return TaskAsyncHelper.Empty;
        }

        public abstract Task Run();

        public abstract Task Close();

        public virtual void Receive(Message message)
        {
            Robot.Receive(message);
        }

        public IList<string> Rooms
        {
            get;
            protected set;
        }

        public IList<string> LogRooms
        {
            get;
            protected set;
        }
    }
}