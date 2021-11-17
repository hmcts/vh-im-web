using System;

namespace InstantMessagingWeb.Common.Model
{
    public class LinkedParticipant
    {
        /// <summary>
        /// The id of the participant linked to
        /// </summary>
        public Guid LinkedId { get; set; }
        
        /// <summary>
        /// The type of link to participant
        /// </summary>
        public LinkType LinkType { get; set; }
    }
}
