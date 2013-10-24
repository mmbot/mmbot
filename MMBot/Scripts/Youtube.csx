
var robot = Require<Robot>();

robot.Respond(@"(youtube|yt)( me)? (.*)", msg =>
{
    var query = msg.Match[3];
    msg.Http("http://gdata.youtube.com/feeds/api/videos")
        .Query(new Dictionary<string, string>
        {
            {"orderBy", "relevance"},
            {"max-results", "15"},
            {"alt", "json"},
            {"q", query}
        })
        .GetJson((err, res, json) => {

            var videos = json["feed"]["entry"];

            if (videos == null)
            {
                msg.Send(string.Format("No video results for \"{0}\"", query));
                return;
            }

            var video = msg.Random(videos);
            foreach (var link in video["link"])
            {
                if ((string) link["rel"] == "alternate" || (string) link["type"] == "text/html")
                {
                    msg.Send((string) link["href"]);
                }
            }
    });
});

robot.AddHelp(
    "mmbot youtube me <query> - Searches YouTube for the query and returns the video embed link."
);
