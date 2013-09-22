using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
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
            
            string configVariable = _robot.GetConfigVariable("MMBOT_BRAIN_PATH");
            _cache = string.IsNullOrWhiteSpace(configVariable) ? BlobCache.LocalMachine : new BrainPersistentBlobCache(configVariable);
        }

        public async Task Initialize()
        {
            
        }

        public async Task Close()
        {
            await _cache.Flush();
        }

        private IBlobCache _cache;

        public async Task<T> Get<T>(string key)
        {
            return await _cache.GetOrCreateObject<T>(GetKey(key), () => default(T));
        }

        private static string GetKey(string key)
        {
            return "MMBotBrain" + key;
        }

        public async Task Set<T>(string key, T value)
        {
            _cache.InsertObject(GetKey(key), value);
        }

        public async Task Remove(string key)
        {
            await _cache.Invalidate(GetKey(key));
        }

    }
}