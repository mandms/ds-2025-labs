using Microsoft.AspNetCore.Http.HttpResults;
using StackExchange.Redis;
using System.Collections.Concurrent;
using Valuator.Models;

namespace Valuator
{
    public class RedisShardManager : IShardManager
    {
        private readonly Dictionary<string, string> _connections = new();

        private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connectionCache = new();
        private ILogger<RedisShardManager> _logger;
        private IDatabase _shard;
        private IDatabase _mainShard;

        public RedisShardManager(ILogger<RedisShardManager> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connections.Add("MAIN", configuration["RedisConnections:MAIN"] ?? "redis_main:6379");
            _connections.Add("RU", configuration["RedisConnections:RU"] ?? "redis_ru:6379");
            _connections.Add("EU", configuration["RedisConnections:EU"] ?? "redis_eu:6379");
            _connections.Add("ASIA", configuration["RedisConnections:ASIA"] ?? "redis_asia:6379");
            _mainShard = GetMainShard();
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

            var mainShard = _mainShard;
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

        public RedisValue GetAuthor(string id)
        {
            return GetByKeyWithPrefix(id, "AUTHOR-");
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

                var parts = connectionString.Split(',');
                var hostAndPort = parts[0];

                var server = conn.GetServer(hostAndPort);
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

        public async Task<bool> UserExists(string username)
        {
            var db = _mainShard;

            var exists = await db.KeyExistsAsync($"USER-USERNAME-{username}");

            if (!exists)
            {
                return false;
            }

            return true;
        }

        public async Task<User?> GetUser(string username)
        {
            var db = _mainShard;
            var userId = await db.StringGetAsync("USER-USERNAME-" + username);

            var hash = await db.HashGetAllAsync("USER-" + userId);

            if (hash.Length == 0) return null;
            _logger.LogInformation("GET USER");
            return new User
            {
                Id = userId.ToString(),
                Username = hash.FirstOrDefault(x => x.Name == "username").Value.ToString(),
                Password = hash.FirstOrDefault(x => x.Name == "password").Value.ToString()
            };
        }


        public async Task AddUser(User user)
        {
            _logger.LogInformation("ADD USER 1 {id}, {username}, {pwd}", user.Id, user.Username, user.Password);
            var entries = new HashEntry[]
            {
                new("username", user.Username),
                new("password", user.Password)
            };

            var db = _mainShard;

            var tran = db.CreateTransaction();
            // Основной ключ с данными пользователя
            await db.HashSetAsync($"USER-{user.Id}", entries);

            // Индекс для поиска по username
            await db.StringSetAsync($"USER-USERNAME-{user.Username}", user.Id);

            bool committed = tran.Execute();

            if (!committed)
            {
                throw new Exception("Error while adding user");
            }

            _logger.LogInformation("ADD USER 2 {id}, {username}, {pwd}", user.Id, user.Username, user.Password);
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