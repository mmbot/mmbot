using System.Collections.Generic;
using System.Threading.Tasks;

namespace MMBot
{
    public interface IAdapter
    {
        string Id { get; }
        IList<string> Rooms { get; }
        IList<string> LogRooms { get; }
        Task Send(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages);
        Task Emote(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages);
        Task Reply(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages);
        Task Topic(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages);
        Task Topic(string roomName, IDictionary<string, string> adapterArgs, params string[] messages);
        Task Play(Envelope envelope, IDictionary<string, string> adapterArgs, params string[] messages);
        Task Run();
        Task Close();
        void Receive(Message message);
    }
}