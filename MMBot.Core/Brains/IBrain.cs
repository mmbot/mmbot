using System.Threading.Tasks;

namespace MMBot.Brains
{
    public interface IBrain
    {
        string Name { get; }
        
        void Initialize();

        Task Close();

        Task<T> Get<T>(string key);

        Task Set<T>(string key, T value);

        Task Remove<T>(string key);
    }
}
