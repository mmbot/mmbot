namespace MMBot
{
    public interface IListener
    {
        ScriptSource Source { get; set; }
        bool Call(Message message);
    }
}