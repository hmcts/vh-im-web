using InstantMessagingWeb.Common.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using InstantMessagingWeb.Common.Configuration;

namespace InstantMessagingWeb.Common.Security
{
    public class InstantMessagingApiTokenHandler : BaseServiceTokenHandler
    {
        public InstantMessagingApiTokenHandler(IOptions<AzureAdConfiguration> azureAdConfiguration,
            IOptions<HearingServicesConfiguration> hearingServicesConfiguration, IMemoryCache memoryCache,
            ITokenProvider tokenProvider) : base(azureAdConfiguration, hearingServicesConfiguration, memoryCache,
            tokenProvider)
        {
        }
        
        protected override string TokenCacheKey => "InstantMessagingApiServiceToken";
        protected override string ClientResource => HearingServicesConfiguration.InstantMessagingApiResourceId;
    }
}