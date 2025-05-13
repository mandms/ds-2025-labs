using StackExchange.Redis;

namespace ShardManager
{
    public interface IShardManager
    {
        public IDatabase GetShard(string key);
        public void SetToMain(string id, string region);
        public void SetToRegion(string key, object value, string prefix);
        public RedisValue GetRank(string id);
        public RedisValue GetSimilarity(string id);
        public RedisValue GetText(string id);
        public bool IsDuplicateText(string text);
    }
}
