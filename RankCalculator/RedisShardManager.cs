using StackExchange.Redis;

namespace RankCalculator
{
    public class RedisShardManager : IShardManager
    {
        private Dictionary<string, string> _connections = new();

        private ILogger<RedisShardManager> _logger;

        private IDatabase _shard;

        public RedisShardManager(ILogger<RedisShardManager> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connections.Add("MAIN", configuration["RedisConnections:MAIN"] ?? "redis_main:6379");
            _connections.Add("RU", configuration["RedisConnections:RU"] ?? "redis_ru:6379");
            _connections.Add("EU", configuration["RedisConnections:EU"] ?? "redis_eu:6379");
            _connections.Add("ASIA", configuration["RedisConnections:ASIA"] ?? "redis_asia:6379");
        }

        private IDatabase GetMainShard()
        {
            _connections.TryGetValue("MAIN", out var mainConnectionString);
            if (mainConnectionString == null) throw new Exception("Main shard connection string not found");
            return ConnectionMultiplexer.Connect(mainConnectionString).GetDatabase();
        }

        public void SetShard(string key)
        {
            var mainShard = GetMainShard();

            var region = mainShard.StringGet(key);
            
            _logger.LogInformation("LOOKUP: {key}, {region}", key, region.ToString());

            _connections.TryGetValue(region.ToString(), out var connectionString);
            if (connectionString == null) throw new Exception($"{region.ToString()} shard connection string not found");
            _shard = ConnectionMultiplexer.Connect(connectionString).GetDatabase();
        }

        public void SetToMain(string id, string region)
        {
            var mainShard = GetMainShard();
            mainShard.StringSet(id, region);
        }

        public void SetToRegion(string key, object value, string prefix)
        {
            _shard.StringSet(prefix + key, value.ToString());
        }

        public RedisValue GetRank(string id)
        {
            var value = _shard.StringGet("RANK-" + id);
            return value;
        }

        public RedisValue GetSimilarity(string id)
        {
            var value = _shard.StringGet("SIMILARITY-" + id);
            return value;
        }

        public RedisValue GetText(string id)
        {
            var value = _shard.StringGet("TEXT-" + id);
            return value;
        }
    }
}
