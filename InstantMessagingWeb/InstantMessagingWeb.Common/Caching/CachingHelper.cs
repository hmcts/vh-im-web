using Newtonsoft.Json;

namespace InstantMessagingWeb.Common.Caching
{
    public static class CachingHelper
    {
        public static JsonSerializerSettings SerializerSettings => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects, Formatting = Formatting.None
        };
    }
}
