using System;

namespace MMBot.Jabbr.JabbrClient.Models
{
    public class Message
    {
        public bool HtmlEncoded { get; set; }
        public string Id { get; set; }
        public string Content { get; set; }
        public DateTimeOffset When { get; set; }
        public User User { get; set; }
    }
}