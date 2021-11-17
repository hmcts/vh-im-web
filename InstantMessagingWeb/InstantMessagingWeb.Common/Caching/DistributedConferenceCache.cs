using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using VideoApi.Contract.Responses;
using InstantMessagingWeb.Common.Model;

namespace InstantMessagingWeb.Common.Caching
{
    public class DistributedConferenceCache : RedisCacheBase<Guid, Conference>, IConferenceCache
    {
        public override DistributedCacheEntryOptions CacheEntryOptions { get; protected set; }

        public DistributedConferenceCache(IDistributedCache distributedCache) : base(distributedCache)
        {
            CacheEntryOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(4)
            };
        }

        public async Task AddConferenceAsync(ConferenceDetailsResponse conferenceResponse)
        {
            var conference = ConferenceCacheMapper.MapConferenceToCacheModel(conferenceResponse);
            await UpdateConferenceAsync(conference);
        }

        public async Task UpdateConferenceAsync(Conference conference)
        {
            await WriteToCache(conference.Id, conference);
        }

        public async Task<Conference> GetOrAddConferenceAsync(Guid id, Func<Task<ConferenceDetailsResponse>> addConferenceDetailsFactory)
        {
            var conference = await ReadFromCache(id);

            if (conference != null) return conference;
            conference = ConferenceCacheMapper.MapConferenceToCacheModel(await addConferenceDetailsFactory());

            await WriteToCache(id, conference);

            return conference;
        }
        
        public override string GetKey(Guid key)
        {
            return key.ToString();
        }
    }
}
