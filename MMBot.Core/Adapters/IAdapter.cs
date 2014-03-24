using System.Collections.Generic;
using System.Threading.Tasks;

namespace MMBot
{
    public interface IAdapter
    {
        string Id { get; }
        IList<string> Rooms { get; }
        IList<string> LogRooms { get; }
        Task Send(Envelope envelope, params string[] messages);
        Task Emote(Envelope envelope, params string[] messages);
        Task Reply(Envelope envelope, params string[] messages);
        Task Topic(Envelope envelope, params string[] messages);
        Task Topic(string roomName, params string[] messages);
        Task Play(Envelope envelope, params string[] messages);
        Task Run();
        Task Close();
        void Receive(Message message);
    }
}