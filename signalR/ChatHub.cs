using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    private static ConcurrentDictionary<string, ConcurrentQueue<string>> userConnections = new ConcurrentDictionary<string, ConcurrentQueue<string>>();

    public override async Task OnConnectedAsync()
    {
        var user = "some user";

        // Ensure the user exists in the dictionary
        if (!userConnections.ContainsKey(user))
        {
            userConnections[user] = new ConcurrentQueue<string>();
        }

        userConnections[user].Enqueue(Context.ConnectionId);
        

        // Check the number of connections
        while (userConnections[user].Count > 1)
        {
            if (userConnections[user].TryDequeue(out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ForceLogout");
            }
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        var user = "some user";

        if (userConnections.TryGetValue(user, out var connections))
        {
            connections = new ConcurrentQueue<string>(connections.Where(id => id != Context.ConnectionId));
            if (connections.IsEmpty)
            {
                userConnections.TryRemove(user, out _);
            }
            else
            {
                userConnections[user] = connections;
            }
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
