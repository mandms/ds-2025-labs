using StackExchange.Redis;

namespace Valuator.Repositories
{
    public interface IValuatorRepository
    {
        bool IsDuplicateText(string text);
        void SetText(string key, string value);
        void SetRank(string key, double value);
        void SetSimilarity(string key, bool value);
        RedisValue GetValue(string key);
    }
}
