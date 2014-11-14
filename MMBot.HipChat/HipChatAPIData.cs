using System.Collections.Generic;

namespace MMBot.HipChat
{
    public class HipchatGetAllRoomsResponse
    {
        public HipchatGetAllRoomsResponse()
        {
            Items = new List<HipchatGetAllRoomsResponseItems>();
        }

        public List<HipchatGetAllRoomsResponseItems> Items { get; set; }
    }

    public class HipchatGetAllRoomsResponseItems
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class HipchatGetRoomResponse
    {
        public string XmppJid { get; set; }
    }

    public class HipchatViewUserResponse
    {
        public string XmppJid { get; set; }
        public string Name { get; set; }
        public string MentionName { get; set; }
    }

    public class HipchatSendRoomNotificationRequest
    {
        public string Message { get; set; }
    }

    public class HipchatPrivateMessageUserRequest
    {
        public HipchatPrivateMessageUserRequest()
        {
            Notify = true;
            MessageFormat = "html";
        }

        public string Message { get; set; }

        public bool Notify { get; set; }

        public string MessageFormat { get; set; }
    }
}