using System.Collections.Generic;
using System.Threading.Tasks;

namespace MMBot
{
    public interface IAdapter
    {
        string Id { get; }
        IList<string> Rooms { get; }
        IList<string> LogRooms { get; }
        Task Send(Envelope envelope, AdapterArguments adapterArgs, params string[] messages);
        Task Emote(Envelope envelope, AdapterArguments adapterArgs, params string[] messages);
        Task Reply(Envelope envelope, AdapterArguments adapterArgs, params string[] messages);
        Task Topic(Envelope envelope, AdapterArguments adapterArgs, params string[] messages);
        Task Topic(string roomName, AdapterArguments adapterArgs, params string[] messages);
        Task Play(Envelope envelope, AdapterArguments adapterArgs, params string[] messages);
        Task Run();
        Task Close();
        void Receive(Message message);
    }

    public class AdapterArguments
    {
        public string Color { get; set; }
    }
}