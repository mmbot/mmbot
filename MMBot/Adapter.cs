namespace MMBot
{
    public abstract class Adapter
    {
        private readonly Robot _robot;

        protected Adapter(Robot robot)
        {
            _robot = robot;
        }

        public virtual void Send(Envelope envelope, params string[] messages)
        {

        }

        public virtual void Emote(Envelope envelope, params string[] messages)
        {

        }

        public virtual void Reply(Envelope envelope, params string[] messages)
        {

        }

        public virtual void Topic(Envelope envelope, params string[] messages)
        {

        }

        public virtual void Play(Envelope envelope, params string[] messages)
        {

        }

        public virtual void Run()
        {

        }

        public virtual void Close()
        {

        }

        public virtual void Receive(Message message)
        {
            _robot.Receive(message);
        }


    }


    public class Envelope
    {
        public User User { get; set; }
    }
}