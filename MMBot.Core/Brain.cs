using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Akavache;

namespace MMBot
{
    public class Brain
    {
        private readonly Robot _robot;
        
        public class BrainPersistentBlobCache : PersistentBlobCache
        {
            public BrainPersistentBlobCache(string cacheDirectory) : base(cacheDirectory)
            {

            }
        }

        public Brain(Robot robot)
        {
            _robot = robot;
            BlobCache.ApplicationName = "MMBotBrain";
        }

        public void Initialize()
        {
            var configVariable = _robot.GetConfigVariable("MMBOT_BRAIN_PATH");
            _cache = string.IsNullOrWhiteSpace(configVariable) ? BlobCache.LocalMachine : new BrainPersistentBlobCache(configVariable);
        }

        public async Task Close()
        {
            await _cache.Flush();
        }

        private IBlobCache _cache;

        public async Task<T> Get<T>(string key)
        {
            return await Get(key, default(T));
        }

        public async Task<T> Get<T>(string key, T defaultValue)
        {
            return await _cache.GetOrCreateObject<T>(GetKey(key), () => defaultValue);
        }

        private static string GetKey(string key)
        {
            return "MMBotBrain" + key;
        }

        public async Task Set<T>(string key, T value)
        {
            await _cache.InsertObject(GetKey(key), value);
        }

        public async Task Remove<T>(string key)
        {
            await _cache.InvalidateObject<T>(GetKey(key));
        }

    }
}