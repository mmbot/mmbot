using System.Collections.Generic;

namespace MMBot.Slack
{
    public class Response
    {
        public bool Ok { get; set; }

        public string Error { get; set; }
    }

    public class ImOpenResponse : Response
    {
        public string Channel { get; set; }
    }

    public class StartResponse : Response
    {
        public string Url { get; set; }

        public User Self { get; set; }

        public IEnumerable<Channel> Channels { get; set; }

        public IEnumerable<Im> Ims { get; set; }

        public IEnumerable<User> Users { get; set; }
    }

    public class User
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string RealName { get; set; }

        public bool IsBot { get; set; }
    }

    public class SendMessage
    {
        public SendMessage(string channel, string message, int id)
        {
            Id = id;
            Type = "message";
            Channel = channel;
            Text = message;
        }

        public int Id { get; set; }

        public string Type { get; set; }

        public string Channel { get; set; }

        public string Text { get; set; }
    }

    public class Channel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public bool IsMember { get; set; }
    }

    public class Im
    {
        public string Id { get; set; }

        // UserId
        public string User { get; set; }

        public bool IsOpen { get; set; }
    }
}