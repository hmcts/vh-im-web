using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using InstantMessagingWeb.Common.Configuration;

namespace InstantMessagingWeb.Common.Security
{
    public class VideoApiTokenHandler : BaseServiceTokenHandler
    {
        public VideoApiTokenHandler(IOptions<AzureAdConfiguration> azureAdConfiguration,
            IOptions<HearingServicesConfiguration> hearingServicesConfiguration, IMemoryCache memoryCache,
            ITokenProvider tokenProvider) : base(azureAdConfiguration, hearingServicesConfiguration, memoryCache,
            tokenProvider)
        {
        }
        
        protected override string TokenCacheKey => "VideoApiServiceToken";
        protected override string ClientResource => HearingServicesConfiguration.VideoApiResourceId;
    }
}