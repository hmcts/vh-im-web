using System;
using System.Collections.Generic;

namespace InstantMessagingWeb.Common.Model
{
    public class CivilianRoom
    {
        public CivilianRoom()
        {
            Participants = new List<Guid>();
        }
        
        public long Id { get; set; }
        public string RoomLabel { get; set; }
        public List<Guid> Participants { get; set; }
    }
}
