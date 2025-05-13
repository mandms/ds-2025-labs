using StackExchange.Redis;

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
    }
}
