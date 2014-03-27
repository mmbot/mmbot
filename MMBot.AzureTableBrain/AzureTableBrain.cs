using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using MMBot.Brains;
using Newtonsoft.Json;

namespace MMBot.AzureTableBrain
{
    public class AzureTableBrain : IBrain, IMustBeInitializedWithRobot
    {
        private Robot _robot;
        private CloudTable _table;
        private string _partition;

        public void Initialize(Robot robot)
        {
            _robot = robot;
            
            CloudStorageAccount storageAccount;

            string useDevelopmentStorage = _robot.GetConfigVariable("MMBOT_AZURETABLEBRAIN_USEDEVELOPMENTSTORAGE");
            string accountName = _robot.GetConfigVariable("MMBOT_AZURETABLEBRAIN_STORAGE_ACCOUNT_NAME");
            string accessKey = _robot.GetConfigVariable("MMBOT_AZURETABLEBRAIN_ACCESS_KEY");

            if ((!string.IsNullOrWhiteSpace(useDevelopmentStorage) && useDevelopmentStorage.Equals("true", StringComparison.InvariantCultureIgnoreCase)) || string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(accessKey))
            {
                _robot.Logger.Info("Using DevelopmentStorageAccount, Azure Storage Emulator must be running");
                _robot.Logger.Info("Configure STORAGE_ACCOUNT_NAME and ACCESS_KEY for production storage");
                
                storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            }
            else
            {
                storageAccount = CloudStorageAccount.Parse(string.Format(
                    "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                    accountName,
                    accessKey)
                    );
            }

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            _table = tableClient.GetTableReference("AzureTableBrain"); // Robot.Alias/Name is used as PartitionKey for separate robots using the same table
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
