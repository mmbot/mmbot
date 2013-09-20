using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace MMBot.Scripts
{
    public class Map : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond("(?:(satellite|terrain|hybrid)[- ])?map me (.+)", msg =>
            {
                var mapType = msg.Match[0].Groups.Count > 1 ? "roadmap" : msg.Match[0].Groups[1].Value;
                var location = msg.Match[0].Groups[2].Value;
                var mapUrl = "http://maps.google.com/maps/api/staticmap?markers=" +
                             WebUtility.UrlEncode(location) +
                             "&size=400x400&maptype=" +
                             mapType +
                             "&sensor=false" +
                             "&format=png"; // So campfire knows it's an image

                var url = "http://maps.google.com/maps?q=" +
                          WebUtility.UrlEncode(location) +
                          "&hl=en&sll=37.0625,-95.677068&sspn=73.579623,100.371094&vpsrc=0&hnear=" +
                          WebUtility.UrlEncode(location) +
                          "&t=m&z=11";

                msg.Send(mapUrl);
                msg.Send(url);
            });
        }

        public IEnumerable<string> GetHelp()
        {
            return new[] {"mmbot map me <query> - Returns a map view of the area returned by `query`."};
        }
    }
}
