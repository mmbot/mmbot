using System.Collections.Generic;
using System.Threading.Tasks;
using MMBot.Brains;
using Newtonsoft.Json;

namespace MMBot.Tests
{
    public class StubBrain : IBrain
    {
        private Dictionary<string, string> _store;
        private Robot _robot;

        public void Initialize(Robot robot)
        {
            _robot = robot;
            _store = new Dictionary<string, string>();
        }

        public async Task Close()
        {
            await Task.Run(() =>
            {
                _store = null;
            });
        }

        public async Task<T> Get<T>(string key)
        {
            return await Task.Run(() =>
            {
                if (!_store.ContainsKey(key))
                {
                    return default(T);
                }
                
                return JsonConvert.DeserializeObject<T>(_store[key]);
            });
        }

        public async Task Set<T>(string key, T value)
        {
            await Task.Run(() =>
            {
                if (_store.ContainsKey(key))
                {
                    _store[key] = JsonConvert.SerializeObject(value);
                }
                else
                {
                    _store.Add(key, JsonConvert.SerializeObject(value));
                }
            });
        }

        public async Task Remove<T>(string key)
        {
            await Task.Run(() =>
            {
                if (_store.ContainsKey(key))
                {
                    _store.Remove(key);
                }
            });
        }
    }
}
