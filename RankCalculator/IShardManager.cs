using StackExchange.Redis;

namespace RankCalculator
{
    public interface IShardManager
    {
        public void SetShard(string key);
        public void SetToRegion(string key, object value, string prefix);
        public void SetToMain(string id, string region);
        public RedisValue GetRank(string id);
        public RedisValue GetSimilarity(string id);
        public RedisValue GetText(string id);
    }
}
