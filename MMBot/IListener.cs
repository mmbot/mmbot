namespace MMBot
{
    public interface IListener
    {
        bool Call(Message message);
    }
}