﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TfsBot.Common.Entities;

namespace TfsBot.Common.Db
{
    public class InMemoryRepository: IRepository
    {
        private readonly List<ServerClient> _serverClients = new List<ServerClient>();
        private readonly List<Client> _clients = new List<Client>();

        public Task SaveServiceClient(ServerClient serverClient)
        {
            _serverClients.Add(serverClient);
            return Task.FromResult(0);
        }

        public Task<ICollection<ServerClient>> GetServerClients(string serverId)
        {
            var result = _serverClients
                .Where(i => i.ServiceId == serverId)
                .ToList();
            return Task.FromResult<ICollection<ServerClient>>(result);
        }

        public Task SaveClient(Client client)
        {
            _clients.Add(client);
            return Task.FromResult(0);
        }

        public Task<Client> GetClientAsync(string userId, string userName)
        {
            return Task.FromResult(
                    _clients.FirstOrDefault(i =>
                        i.PartitionKey == Client.GetPartitionKey(userId) &&
                        i.RowKey == Client.GetRowKey(userId, userName)));
        }

        public Task<IEnumerable<string>> GetServersOfClient(string userId)
        {
            return Task.FromResult(_serverClients.Where(x => x.ConversationId == userId).Select(x => x.ServiceId));
        }
    }
}