using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using UserApi.Contract.Responses;

namespace InstantMessagingWeb.Common.Caching
{
    public class DistributedUserCache : RedisCacheBase<string, UserProfile>, IUserCache
    {
        public override DistributedCacheEntryOptions CacheEntryOptions { get; protected set; }

        public DistributedUserCache(IDistributedCache distributedCache) : base(distributedCache)
        {
            CacheEntryOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(4)
            };
        }

        public async Task<UserProfile> GetOrAddAsync(string key, Func<string, Task<UserProfile>> valueFactory)
        {
            var profile = await ReadFromCache(key);

            if (profile != null) return profile;            
            profile = await valueFactory(key);

            await WriteToCache(key, profile);

            return profile;
        }

        public override string GetKey(string key)
        {
            return key;
        }
    }
}
