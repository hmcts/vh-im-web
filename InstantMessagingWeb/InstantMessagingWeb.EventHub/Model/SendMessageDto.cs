using InstantMessagingWeb.Common.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace InstantMessagingWeb.EventHub.Model
{
    public class SendMessageDto
    {
        public Guid MessageUuid { get; set; }
        public Conference Conference { get; set; }
        public string Message { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string ParticipantUsername { get; set; }
        public DateTime Timestamp { get; set; }

    }
}
