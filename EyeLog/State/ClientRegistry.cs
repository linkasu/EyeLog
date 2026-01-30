using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EyeLog.State
{
    internal class ClientRegistry
    {
        private readonly ConcurrentDictionary<string, ClientState> clients = new ConcurrentDictionary<string, ClientState>(StringComparer.OrdinalIgnoreCase);

        public ClientState GetOrCreate(string clientId)
        {
            return clients.GetOrAdd(clientId, id => new ClientState(id));
        }

        public bool TryGet(string clientId, out ClientState client)
        {
            return clients.TryGetValue(clientId, out client);
        }

        public IEnumerable<ClientState> AllClients()
        {
            return clients.Values;
        }

        public void CloseAll()
        {
            foreach (var client in clients.Values)
            {
                client.CloseAll();
            }
        }
    }
}
