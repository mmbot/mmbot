
var robot = Require<Robot>();

robot.Respond(@"pug me", msg =>
{
    msg.Http("http://pugme.herokuapp.com/random").GetJson((err, res, body) => {
        msg.Send((string)body["pug"]);
    });
});

robot.Respond(@"pug bomb( (\d+))?", msg =>
{
    var count = msg.Match.Count() > 2 ? msg.Match[2] : "5";
    msg.Http("http://pugme.herokuapp.com/bomb?count=" + count).GetJson((err, res, body) => {
        foreach(var pug in body["pugs"])
        {
            msg.Send((string)pug);
        }
    });
});

robot.Respond(@"how many pugs are there", msg =>
{
    msg.Http("http://pugme.herokuapp.com/count").GetJson((err, res, body) => {
        msg.Send(string.Format("There are {0} pugs.", body["pug_count"]));
    });
});

robot.AddHelp(
    "mmbot pug me - Receive a pug",
    "mmbot pug bomb N - get N pugs"
);