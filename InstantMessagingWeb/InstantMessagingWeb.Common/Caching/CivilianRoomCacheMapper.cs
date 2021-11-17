using InstantMessagingWeb.Common.Model;
using VideoApi.Contract.Responses;

namespace InstantMessagingWeb.Common.Caching
{
    public static class CivilianRoomCacheMapper
    {
        public static CivilianRoom MapCivilianRoomToCacheModel(CivilianRoomResponse civilianRoom)
        {
            return new CivilianRoom
            {
                Id = civilianRoom.Id,
                RoomLabel = civilianRoom.Label,
                Participants = civilianRoom.Participants
            };
        }
    }
}
