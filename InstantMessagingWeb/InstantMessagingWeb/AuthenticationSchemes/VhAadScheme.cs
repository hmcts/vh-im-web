using InstantMessagingWeb.Common.Configuration;

namespace InstantMessagingWeb.AuthenticationSchemes
{
    public class VhAadScheme : AadSchemeBase
    {
        public VhAadScheme(AzureAdConfiguration azureAdConfiguration, string eventhubPath): base(eventhubPath, azureAdConfiguration)
        {
        }

        public override AuthProvider Provider => AuthProvider.VHAAD;
    }
}
