using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using MMBot.Brains;
using Newtonsoft.Json;

namespace MMBot.AzureTableBrain
{
    public class AzureTableBrain : IBrain
    {
        private readonly Robot _robot;
        private CloudTable _table;
        private string _partition;

        public string Name
        {
            get { return "AzureTableBrain"; }
        }

        public AzureTableBrain(Robot robot)
        {
            _robot = robot;
        }

        public void Initialize()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(string.Format(
                "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                _robot.GetConfigVariable("MMBOT_AZURETABLEBRAIN_STORAGE_ACCOUNT_NAME"),
                _robot.GetConfigVariable("MMBOT_AZURETABLEBRAIN_ACCESS_KEY"))
                );

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            _table = tableClient.GetTableReference(Name); // AzureTableBrain, Robot.Alias/Name is used as PartitionKey for separate robots using the same table
            _table.CreateIfNotExists();

            _partition = _robot.Alias ?? _robot.Name;
        }

        public Task Close()
        {
            // No cleanup required
            return TaskAsyncHelper.Empty;
        }

        public async Task<T> Get<T>(string key)
        {
            return await Task.Run(() =>
            {
                BrainEntity brainEntity = _table.Execute(TableOperation.Retrieve<BrainEntity>(_partition, key)).Result as BrainEntity;

                if (brainEntity != null)
                {
                    return JsonConvert.DeserializeObject<T>(brainEntity.Value);
                }

                return default(T);
            });
        }

        public async Task Set<T>(string key, T value)
        {
            await _table.ExecuteAsync(TableOperation.InsertOrReplace(new BrainEntity
            {
                PartitionKey = _partition,
                RowKey = key,
                Timestamp = DateTime.UtcNow,
                Value = JsonConvert.SerializeObject(value)
            }));
        }

        public async Task Remove<T>(string key)
        {
            await _table.ExecuteAsync(TableOperation.Delete(new BrainEntity
            {
                PartitionKey = _partition,
                RowKey = key
            }));
        }

        public class BrainEntity : TableEntity
        {
            public string Value { get; set; }
        }
    }
}
