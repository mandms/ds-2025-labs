using StackExchange.Redis;
using System.Collections.Concurrent;

namespace Valuator
{
    public class RedisShardManager : IShardManager
    {
        private readonly Dictionary<string, string> _connections = new();

        private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connectionCache = new();
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
            if (!_connections.TryGetValue("MAIN", out var mainConnectionString))
            {
                throw new Exception("Main shard connection string not found");
            }

            var connection = _connectionCache.GetOrAdd(mainConnectionString,
                connStr => ConnectionMultiplexer.Connect(connStr + ",abortConnect=false"));

            return connection.GetDatabase();
        }

        public void SetShard(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            var mainShard = GetMainShard();
            var region = mainShard.StringGet(key);

            _logger.LogInformation("LOOKUP: {key}, {region}", key, region.ToString());

            if (region.IsNullOrEmpty)
            {
                throw new Exception($"Region not found for key: {key}");
            }

            if (!_connections.TryGetValue(region.ToString(), out var connectionString))
            {
                throw new Exception($"{region} shard connection string not found");
            }

            var connection = _connectionCache.GetOrAdd(connectionString,
                connStr => ConnectionMultiplexer.Connect(connStr + ",abortConnect=false"));

            _shard = connection.GetDatabase();
        }

        public void SetToMain(string id, string region)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id cannot be null or empty", nameof(id));
            }

            if (string.IsNullOrEmpty(region))
            {
                throw new ArgumentException("Region cannot be null or empty", nameof(region));
            }

            var mainShard = GetMainShard();
            mainShard.StringSet(id, region);
        }

        public void SetToRegion(string key, object value, string prefix)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            _shard.StringSet(prefix + key, value.ToString());
        }

        public RedisValue GetRank(string id)
        {
            return GetByKeyWithPrefix(id, "RANK-");
        }

        public RedisValue GetSimilarity(string id)
        {
            return GetByKeyWithPrefix(id, "SIMILARITY-");
        }

        public RedisValue GetText(string id)
        {
            return GetByKeyWithPrefix(id, "TEXT-");
        }

        private RedisValue GetByKeyWithPrefix(string id, string prefix)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id cannot be null or empty", nameof(id));
            }

            return _shard.StringGet(prefix + id);
        }

        public bool IsDuplicateText(string text)
        {
            foreach (var connectionString in _connections.Values)
            {
                if (connectionString == null) throw new Exception($"Shard connection string not found");
                var conn = ConnectionMultiplexer.Connect(connectionString + ",abortConnect=false");
                var db = conn.GetDatabase();

                var server = conn.GetServer(connectionString);
                var keys = server.Keys(pattern: "TEXT-*");
                foreach (var key in keys)
                {
                    var value = db.StringGet(key);
                    if (value.ToString() == text)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    

        public void Dispose()
        {
            foreach (var connection in _connectionCache.Values)
            {
                connection?.Dispose();
            }
            _connectionCache.Clear();
        }
    }
}