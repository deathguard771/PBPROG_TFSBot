using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.Bot.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using TfsBot.Common.Entities;

namespace TfsBot.Common.Db
{
    public class Repository : IRepository
    {
        private readonly CloudTable _serviceClientsTable;
        private readonly CloudTable _clientsTable;

        public Repository(BotConfiguration botConfiguration)
        {
            var storageService = botConfiguration.Services.FirstOrDefault(x => typeof(BlobStorageService).Equals(x.GetType())) as BlobStorageService;
            var storageConnectionString = storageService.ConnectionString;

            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _serviceClientsTable = tableClient.GetTableReference("serviceclients");
            _serviceClientsTable.CreateIfNotExistsAsync().GetAwaiter().GetResult();
            _clientsTable = tableClient.GetTableReference("clients");
            _clientsTable.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }

        public async Task SaveServiceClient(ServerClient serverClient)
        {
            await _serviceClientsTable.ExecuteAsync(TableOperation.InsertOrReplace(serverClient));
        }

        public async Task<ICollection<ServerClient>> GetServerClients(string serverId)
        {
            var query = new TableQuery<ServerClient>()
                .Where(TableQuery.GenerateFilterCondition(nameof(ServerClient.PartitionKey), QueryComparisons.Equal, serverId));

            var collection = await _serviceClientsTable.ExecuteQuerySegmentedAsync(query, null);

            return collection.ToList();
        }

        public async Task SaveClient(Client client)
        {
            await _clientsTable.ExecuteAsync(TableOperation.InsertOrReplace(client));
        }

        public async Task<Client> GetClientAsync(string userId, string userName)
        {
            var partitionKey = Client.GetPartitionKey(userId);
            var rowKey = Client.GetRowKey(userId, userName);

            var retrieveOperation = TableOperation.Retrieve<Client>(partitionKey, rowKey);
            var retrievedResult = await _clientsTable.ExecuteAsync(retrieveOperation);
            return (Client)retrievedResult.Result;
        }

        public async Task RemoveServerClientAsync(ServerClient client)
        {
            var deleteOperation = TableOperation.Delete(client);
            await _serviceClientsTable.ExecuteAsync(deleteOperation);
        }
    }
}