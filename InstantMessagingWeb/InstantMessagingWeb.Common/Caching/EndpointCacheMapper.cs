using System;
using InstantMessagingWeb.Common.Model;
using VideoApi.Contract.Responses;

namespace InstantMessagingWeb.Common.Caching
{
    public static class EndpointCacheMapper
    {
        public static Endpoint MapEndpointToCacheModel(EndpointResponse endpointResponse)
        {
            return new Endpoint
            {
                Id = endpointResponse.Id,
                DisplayName = endpointResponse.DisplayName,
                EndpointStatus =
                    (EndpointStatus)Enum.Parse(typeof(EndpointStatus), endpointResponse.Status.ToString()),
                DefenceAdvocateUsername = endpointResponse.DefenceAdvocate?.ToLower().Trim()
            };
        }
    }
}
