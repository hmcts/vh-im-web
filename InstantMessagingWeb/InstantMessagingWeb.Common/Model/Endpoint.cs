using System;

namespace InstantMessagingWeb.Common.Model
{
    public class Endpoint
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public EndpointStatus EndpointStatus { get; set; }
        public string DefenceAdvocateUsername { get; set; }
    }
}
