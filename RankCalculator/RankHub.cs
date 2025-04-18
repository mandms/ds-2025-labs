using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace RankCalculator
{
	public class RankHub: Hub
	{
        public static ConcurrentDictionary<string, string> _userConnections = new();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.Request.Cookies["textId"]; //var userId = Context.GetHttpContext()?.Request.Query["id"];

            if (userId != null)
            {
                _userConnections.TryAdd(userId, Context.ConnectionId);
            }

            Console.WriteLine("user: " + _userConnections[userId]);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _userConnections.TryRemove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
