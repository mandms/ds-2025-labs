using StackExchange.Redis;

namespace ShardManager
{
    public class RedisShardManager: IShardManager
    {
        private static Dictionary<string, string> _connections = new()
        {
            {"MAIN", "redis:6379" },
            {"EN", "redis:6380"},
            {"RU", "redis:6381" },
            {"ASIA", "redis:6382" }
        };

        public RedisShardManager()
        {
           // var connetionMain = Environment.GetEnvironmentVariable("DB_MAIN");
            //Console.WriteLine(connetionMain);
            

            //_shards = connectionStrings
            //    .Select(cs => ConnectionMultiplexer.Connect(cs).GetDatabase())
            //    .ToArray();
        }

        private IDatabase GetMainShard()
        {
            _connections.TryGetValue("MAIN", out var mainConnectionString);
            if (mainConnectionString == null) throw new Exception("Main shard connection string not found");
            return ConnectionMultiplexer.Connect(mainConnectionString).GetDatabase();
        }

        public IDatabase GetShard(string key)
        {
            var mainShard = GetMainShard();

            var region = mainShard.StringGet(key);
            _connections.TryGetValue(region.ToString(), out var connectionString);
            if (connectionString == null) throw new Exception($"{region.ToString()} shard connection string not found");
            return ConnectionMultiplexer.Connect(connectionString).GetDatabase();
        }

        public void SetToMain(string id, string region)
        {
            var mainShard = GetMainShard();
            mainShard.StringSet(id, region);
        }

        public void SetToRegion(string key, object value, string prefix)
        {
            var shard = GetShard(key);
            shard.StringSet(prefix + key, value.ToString());
        }

        public RedisValue GetRank(string id) //переделать на hash: {rank, similarity, text}
        {
            var shard = GetShard(id);
            var value = shard.StringGet("RANK-" + id);
            return value;
        }

        public RedisValue GetSimilarity(string id) //переделать на hash: {rank, similarity, text}
        {
            var shard = GetShard(id);
            var value = shard.StringGet("SIMILARITY-" + id);
            return value;
        }

        public RedisValue GetText(string id) //переделать на hash: {rank, similarity, text}
        {
            var shard = GetShard(id);
            var value = shard.StringGet("TEXT-" + id);
            return value;
        }

        public bool IsDuplicateText(string text)
        {
            foreach (var connectionString in _connections.Values)
            {
                if (connectionString == null) throw new Exception($"Shard connection string not found");
                var conn = ConnectionMultiplexer.Connect(connectionString);
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
    }
}
