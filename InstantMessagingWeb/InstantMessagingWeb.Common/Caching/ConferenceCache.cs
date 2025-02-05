﻿using System;
using System.Threading.Tasks;
using InstantMessagingWeb.Common.Model;
using Microsoft.Extensions.Caching.Memory;
using VideoApi.Contract.Responses;

namespace InstantMessagingWeb.Common.Caching
{
    public class ConferenceCache : IConferenceCache
    {
        private readonly IMemoryCache _memoryCache;

        public ConferenceCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task AddConferenceAsync(ConferenceDetailsResponse conferenceResponse)
        {
            var conference = ConferenceCacheMapper.MapConferenceToCacheModel(conferenceResponse);
            await UpdateConferenceAsync(conference);
        }

        public async Task UpdateConferenceAsync(Conference conference)
        {
            await _memoryCache.GetOrCreateAsync(conference.Id, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(4);
                return Task.FromResult(conference);
            });
        }

        public async Task<Conference> GetOrAddConferenceAsync(Guid id, Func<Task<ConferenceDetailsResponse>> addConferenceDetailsFactory)
        {
            var conference = await Task.FromResult(_memoryCache.Get<Conference>(id));

            if (conference != null) return conference;

            var conferenceDetails = await addConferenceDetailsFactory();
            await AddConferenceAsync(conferenceDetails);
            conference = await Task.FromResult(_memoryCache.Get<Conference>(id));

            return conference;
        }
    }
}
