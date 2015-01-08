using ServiceStack;
using ServiceStack.Text;
using WebSocket4Net;

namespace MMBot.Slack
{
    public class SlackAPI
    {
        private const string rtm_start = "https://slack.com/api/rtm.start";
        private const string channels_join = "https://slack.com/api/channels.join";
        private const string im_open = "https://slack.com/api/im.open";

        private readonly string token;

        public SlackAPI(string token)
        {
            this.token = token;
        }

        public StartResponse RtmStart()
        {
            using (GetJsConfigScope())
            {
                return rtm_start
                    .AddQueryParam("token", token)
                    .GetJsonFromUrl()
                    .FromJson<StartResponse>();
            }
        }

        public Response ChannelsJoin(string channelName)
        {
            using (GetJsConfigScope())
            {
                return channels_join
                    .AddQueryParam("token", token)
                    .AddQueryParam("name", channelName)
                    .GetJsonFromUrl()
                    .FromJson<Response>();
            }
        }

        public ImOpenResponse ImOpen(string userId)
        {
            using (GetJsConfigScope())
            {
                return im_open
                    .AddQueryParam("token", token)
                    .AddQueryParam("user", userId)
                    .GetJsonFromUrl()
                    .FromJson<ImOpenResponse>();
            }
        }

        public static void Send(WebSocket ws, string channel, string message, int replyId = 1)
        {
            using (GetJsConfigScope())
            {
                var data = StringExtensions.ToJson(new SendMessage(channel, message, replyId));

                ws.Send(data);
            }
        }

        private static JsConfigScope GetJsConfigScope()
        {
            return JsConfig.With(
                emitLowercaseUnderscoreNames: true,
                emitCamelCaseNames: false,
                propertyConvention: PropertyConvention.Lenient);
        }
    }
}