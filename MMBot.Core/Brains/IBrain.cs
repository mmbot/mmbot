using System.Threading.Tasks;

namespace MMBot.Brains
{
    public interface IBrain
    {
        void Initialize(Robot robot);

        Task Close();

        Task<T> Get<T>(string key);

        Task Set<T>(string key, T value);

        Task Remove<T>(string key);
    }
}
