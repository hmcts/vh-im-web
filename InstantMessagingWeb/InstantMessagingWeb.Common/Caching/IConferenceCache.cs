using InstantMessagingWeb.Common.Model;
using System;
using System.Threading.Tasks;
using VideoApi.Contract.Responses;

namespace InstantMessagingWeb.Common.Caching
{
    public interface IConferenceCache
    {
        Task AddConferenceAsync(ConferenceDetailsResponse conferenceResponse);
        Task UpdateConferenceAsync(Conference conference);
        Task<Conference> GetOrAddConferenceAsync(Guid id, Func<Task<ConferenceDetailsResponse>> addConferenceDetailsFactory);
    }
}
