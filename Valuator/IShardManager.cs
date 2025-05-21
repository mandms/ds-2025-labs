using StackExchange.Redis;
using Valuator.Models;

namespace Valuator
{
    public interface IShardManager
    {
        public void SetShard(string key);
        public void SetToMain(string id, string region);
        public void SetToRegion(string key, object value, string prefix);
        public RedisValue GetRank(string id);
        public RedisValue GetSimilarity(string id);
        public RedisValue GetText(string id);
        public bool IsDuplicateText(string text);
        public RedisValue GetAuthor(string id);
        public Task AddUser(User user);
        public Task<bool> UserExists(string username);
        public Task<User?> GetUser(string username);
    }
}
