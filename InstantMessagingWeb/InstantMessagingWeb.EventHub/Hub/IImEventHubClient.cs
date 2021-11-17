using System;
using System.Threading.Tasks;

namespace InstantMessagingWeb.EventHub.Hub
{
    public interface IImEventHubClient
    {
        Task ReceiveMessage(Guid conferenceId, string from, string to, string message, DateTime timestamp, Guid messageId);

        Task AdminAnsweredChat(Guid conferenceId, string username);
    }
}
