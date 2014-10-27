using System;
using System.Threading.Tasks;
using MMBot.Brains;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MMBot.RedisBrain
{
    public class RedisBrain : IBrain, IMustBeInitializedWithRobot
    {
        private Robot _robot;
        private string _redisConnect;
        private ConnectionMultiplexer _redis;

        public void Initialize(Robot robot)
        {
            try
            {
                _robot = robot;
                _redisConnect = _robot.GetConfigVariable("MMBOT_REDIS_CONNECT") ?? "localhost";
                _redis = ConnectionMultiplexer.Connect(_redisConnect);
            }
            catch (Exception e)
            {
                robot.Logger.Fatal("Could not initialize redis brain", e);
                throw;
            }
        }

        public Task Close()
        {
            _redis.Dispose();
            _redis = null;
            return Task.FromResult(true);
        }

        public async Task<T> Get<T>(string key)
        {
            var database = _redis.GetDatabase();
            var value = await database.StringGetAsync(key);
            if (typeof(T) == typeof(string))
            {
                // :(
                return (T)(object)(string)value;
            }
            if (typeof(T) == typeof(byte[]))
            {
                // :( :( :(
                return (T)(object)(byte[])value;
            }
            if (value.IsNull)
            {
                return default(T);
            }
            var serialized = value.ToString();

            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public async Task Set<T>(string key, T value)
        {
            var database = _redis.GetDatabase();
            if (value is string)
            {
                await database.StringSetAsync(key, value as string);
                return;
            }
            if (value is byte[])
            {
                await database.StringSetAsync(key, value as byte[]);
                return;
            }
            var serialized = JsonConvert.SerializeObject(value);
            await database.StringSetAsync(key, serialized);
        }

        public async Task Remove<T>(string key)
        {
            var database = _redis.GetDatabase();
            await database.KeyDeleteAsync(key);
        }
    }
}