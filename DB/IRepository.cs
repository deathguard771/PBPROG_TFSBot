using System.Collections.Generic;
using System.Threading.Tasks;
using TfsBot.Common.Entities;

namespace TfsBot.Common.Db
{
    public interface IRepository
    {
        Task SaveServiceClient(ServerClient serverClient);
        Task<ICollection<ServerClient>> GetServerClients(string serverId);
        Task SaveClient(Client client);
        Task<Client> GetClientAsync(string userId, string userName);
        Task<IEnumerable<string>> GetServersOfClient(string userId);
        //Task RemoveServerClientAsync(ServerClient client);
    }
}