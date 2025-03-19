using StackExchange.Redis;

namespace Valuator.Repositories
{
    public class ValuatorRepository: IValuatorRepository
    {
        private readonly IConnectionMultiplexer _redis;
        public ValuatorRepository(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public RedisValue GetValue(string key)
        {
            var db = _redis.GetDatabase();
            return db.StringGet(key);
        }

        public bool IsDuplicateText(string text)
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer("redis", 6379);

            var keys = server.Keys(pattern: "TEXT-*");
            foreach (var key in keys)
            {
                var value = db.StringGet(key);
                if (value.ToString() == text)
                {
                    return true;
                }
            }
            return false;
        }

        public void SetRank(string key, double value)
        {
            var db = _redis.GetDatabase();
            db.StringSet(key, value);
        }

        public void SetSimilarity(string key, bool value)
        {
            var db = _redis.GetDatabase();
            db.StringSet(key, value);
        }

        public void SetText(string key, string value)
        {
            var db = _redis.GetDatabase();
            db.StringSet(key, value);
        }
    }
}
