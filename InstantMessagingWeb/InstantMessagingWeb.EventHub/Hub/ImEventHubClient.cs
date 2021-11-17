using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using InstantMessagingAPI.Client;
using InstantMessagingWeb.Common.Model;
using InstantMessagingWeb.Common.SignalR;
using VideoApi.Client;
using InstantMessagingWeb.EventHub.Model;
using InstantMessagingWeb.Common.Caching;
using InstantMessagingWeb.Common.Configuration;
using InstantMessagingWeb.EventHub.Exceptions;
using Microsoft.Extensions.Options;
using InstantMessagingAPI.Contract.Requests;

namespace InstantMessagingWeb.EventHub.Hub
{
    public class ImEventHub : Hub<IImEventHubClient>
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IConferenceCache _conferenceCache;
        private readonly IInstantMessagingApiClient _instantMessagingApiClient;
        private readonly IVideoApiClient _videoApiClient;
        private readonly ILogger<ImEventHub> _logger;
        private readonly HearingServicesConfiguration _servicesConfiguration;

        public static string VhOfficersGroupName => "VhOfficers";
        public static string DefaultAdminName => "Admin";

        public ImEventHub(IUserProfileService userProfileService, IInstantMessagingApiClient instantMessagingApiClient,
            IVideoApiClient videoApiClient, IConferenceCache conferenceCache,
            IOptions<HearingServicesConfiguration> servicesConfiguration, ILogger<ImEventHub> logger)
        {
            _userProfileService = userProfileService;
            _conferenceCache = conferenceCache;
            _instantMessagingApiClient = instantMessagingApiClient;
            _videoApiClient = videoApiClient;
            _servicesConfiguration = servicesConfiguration.Value;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userName = await GetObfuscatedUsernameAsync(Context.User.Identity.Name);
            _logger.LogTrace("Connected to event hub server-side: {Username}", userName);
            var isAdmin = IsSenderAdmin();

            //await AddUserToUserGroup(isAdmin);
            //await AddUserToConferenceGroups(isAdmin);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userName = await GetObfuscatedUsernameAsync(Context.User.Identity.Name.ToLowerInvariant());
            if (exception == null)
            {
                _logger.LogInformation("Disconnected from chat hub server-side: {Username}", userName);
            }
            else
            {
                _logger.LogWarning(exception,
                    "There was an error when disconnecting from chat hub server-side: {Username}", userName);
            }

            var isAdmin = IsSenderAdmin();
            //await RemoveUserFromUserGroup(isAdmin);
            //await RemoveUserFromConferenceGroups(isAdmin);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="conferenceId">The conference Id</param>
        /// <param name="message">The body message</param>
        /// <param name="to">The participant Id or admin username</param>
        /// <param name="messageUuid">The message Id</param>
        /// <returns></returns>
        public async Task SendMessage(Guid conferenceId, string message, string to, Guid messageUuid)
        {
            var from = "18A87C83-8B3B-4AD6-9863-50ADE883033E";
            var dto = new SendMessageDto
            {
                Conference = new Conference { Id = conferenceId },
                From = from,
                To = to,
                Message = message,
                ParticipantUsername = "TestVHO",   // participantUsername,
                Timestamp = DateTime.UtcNow,
                MessageUuid = messageUuid
            };

            await _instantMessagingApiClient.AddInstantMessageToConferenceAsync(conferenceId, new AddInstantMessageRequest
            {
                From = from,
                To = to,
                MessageText = message
            });
        }

        private async Task<string> GetObfuscatedUsernameAsync(string username)
        {
            return await _userProfileService.GetObfuscatedUsernameAsync(username);
        }

        private async Task AddUserToUserGroup(bool isAdmin)
        {
            if (isAdmin)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, VhOfficersGroupName);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, Context.User.Identity.Name.ToLowerInvariant());
        }

        private async Task AddUserToConferenceGroups(bool isAdmin)
        {
            var conferenceIds = await GetConferenceIds(isAdmin);
            var tasks = conferenceIds.Select(c => Groups.AddToGroupAsync(Context.ConnectionId, c.ToString())).ToArray();

            await Task.WhenAll(tasks);
        }

        private bool IsSenderAdmin()
        {
            return Context.User.IsInRole(AppRoles.VhOfficerRole);
        }

        private async Task<IEnumerable<Guid>> GetConferenceIds(bool isAdmin)
        {
            if (isAdmin)
            {
                var conferences = await _videoApiClient.GetConferencesTodayForAdminByHearingVenueNameAsync(null);
                return conferences.Select(x => x.Id);
            }

            return new Guid[0];
        }

        private async Task RemoveUserFromUserGroup(bool isAdmin)
        {
            if (isAdmin)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, VhOfficersGroupName);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Context.User.Identity.Name.ToLowerInvariant());
        }

        private async Task RemoveUserFromConferenceGroups(bool isAdmin)
        {
            var conferenceIds = await GetConferenceIds(isAdmin);
            var tasks = conferenceIds.Select(c => Groups.RemoveFromGroupAsync(Context.ConnectionId, c.ToString()))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        private async Task<string> GetParticipantUsernameByIdAsync(Guid conferenceId, string participantId)
        {
            var username = string.Empty;
            try
            {
                var participantGuidId = Guid.Parse(participantId);
                var conference = await GetConference(conferenceId);
                var participant = conference.Participants.Single(x => x.Id == participantGuidId);

                return participant.Username;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occured to find the participant in conference {ConferenceId} by participant Id {ParticipantId}",
                    conferenceId, participantId);
                return username;
            }
        }

        private async Task<string> GetParticipantIdByUsernameAsync(Guid conferenceId, string participantUsername)
        {
            var particiantId = string.Empty;
            try
            {
                var conference = await GetConference(conferenceId);
                var participant = conference.Participants.Single(x =>
                    x.Username.Equals(participantUsername, StringComparison.InvariantCultureIgnoreCase));

                return participant.Id.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured to find the participant in conference {ConferenceId} by username",
                    conferenceId);
                return particiantId;
            }
        }

        private async Task<Conference> GetConference(Guid conferenceId)
        {
            var conference = await _conferenceCache.GetOrAddConferenceAsync
            (
                conferenceId,
                () => _videoApiClient.GetConferenceDetailsByIdAsync(conferenceId)
            );
            return conference;
        }

        private async Task<bool> IsRecipientAdmin(string recipientUsername)
        {
            if (recipientUsername.Equals(DefaultAdminName, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (!recipientUsername.ToLower().EndsWith(_servicesConfiguration.EmailReformDomain))
            {
                return false;
            }


            var user = await _userProfileService.GetUserAsync(recipientUsername);
            return user != null && user.UserRole.Equals("VHOfficer", StringComparison.InvariantCultureIgnoreCase);
        }

        private async Task<bool> IsAllowedToSendMessageAsync(Guid conferenceId, bool isSenderAdmin,
         bool isRecipientAdmin, string participantUsername)
        {
            var username = await _userProfileService.GetObfuscatedUsernameAsync(participantUsername);
            if (!IsConversationBetweenAdminAndParticipant(isSenderAdmin, isRecipientAdmin))
            {
                return false;
            }

            // participant check first belongs to conference
            try
            {
                var conference = await GetConference(conferenceId);
                var participant = conference.Participants.SingleOrDefault(x =>
                    x.Username.Equals(participantUsername, StringComparison.InvariantCultureIgnoreCase));

                if (participant == null)
                {

                    _logger.LogDebug("Participant {Username} does not exist in conversation", username);
                    throw new ParticipantNotFoundException(conferenceId, Context.User.Identity.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured when validating send message");
                return false;
            }

            _logger.LogDebug("Participant {Username} exists in conversation", username);
            return true;
        }

        private async Task SendToAdmin(SendMessageDto dto, string fromId)
        {
            var groupName = dto.Conference.Id.ToString();
            _logger.LogDebug("Sending message {MessageUuid} to group {GroupName}", dto.MessageUuid, groupName);
            var from = string.IsNullOrEmpty(fromId) ? dto.From : fromId;
            await Clients.Group(groupName)
                .ReceiveMessage(dto.Conference.Id, from, dto.To, dto.Message, dto.Timestamp, dto.MessageUuid);
        }

        private async Task SendToParticipant(SendMessageDto dto)
        {
            var participant = dto.Conference.Participants.Single(x =>
                x.Username.Equals(dto.ParticipantUsername, StringComparison.InvariantCultureIgnoreCase));

            var username = await _userProfileService.GetObfuscatedUsernameAsync(participant.Username);
            _logger.LogDebug("Sending message {MessageUuid} to group {Username}", dto.MessageUuid, username);

            var from = participant.Id.ToString() == dto.To ? dto.From : participant.Id.ToString();

            await Clients.Group(participant.Username.ToLowerInvariant())
                .ReceiveMessage(dto.Conference.Id, from, dto.To, dto.Message, dto.Timestamp, dto.MessageUuid);
        }

        private bool IsConversationBetweenAdminAndParticipant(bool isSenderAdmin, bool isRecipientAdmin)
        {
            try
            {
                if (isSenderAdmin && isRecipientAdmin)
                {

                    _logger.LogDebug("Sender and recipient are admins");
                    throw new InvalidInstantMessageException("Admins are not allowed to IM each other");
                }

                if (!isSenderAdmin && !isRecipientAdmin)
                {
                    _logger.LogDebug("Sender and recipient are participants");
                    throw new InvalidInstantMessageException("Participants are not allowed to IM each other");
                }
            }
            catch (InvalidInstantMessageException e)
            {
                _logger.LogError(e, "IM rules violated. Communication attempted between participants");
                return false;
            }

            _logger.LogDebug("Sender and recipient are allowed to converse");
            return true;
        }
    }
}
