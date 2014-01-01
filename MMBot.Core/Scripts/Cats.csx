/**
* <description>
*     Brings cats
* </description>
*
* <commands>
*     mmbot cat me &lt;number&gt; - Returns a number of cat pictures.;
*     mmbot cat me - Returns a cat picture.;
*     mmbot cat gif &lt;number&gt; - Returns a number of cat gifs.;
*     mmbot cat gif - Returns a cat gif.;
* </commands>
* 
* <author>
*     dkarzon
* </author>
*/

var robot = Require<Robot>();

private const string Url = "http://thecatapi.com/api/images/get?format=xml&results_per_page={0}&api_key=MTAzNjQ";


robot.Respond(@"(cat|cats)( gif)( \d+)?$", msg =>
{
    int number = 1;
    try
    {
        number = Int32.Parse(msg.Match[3]);
    }
    catch (Exception) { }
    if (number == 0)
    {
        number = 1;
    }

    CatMeGifCore(msg, number);
});

robot.Respond(@"(cat|cats)( me)?( \d+)?$", msg =>
{
    int number = 1;
    try
    {
        number = Int32.Parse(msg.Match[3]);
    }
    catch (Exception) { }
    if (number == 0)
    {
        number = 1;
    }

    CatMeCore(msg, number);
});


private static void CatMeCore(IResponse<TextMessage> msg, int number)
{
    msg.Http(string.Format(Url, number)).GetXml((err, res, xDoc) =>
    {
        try
        {
            var urls = xDoc.SelectNodes("//url");
            foreach (XmlNode url in urls)
            {
                msg.Send(url.InnerText);
            }
        }
        catch (Exception)
        {
            msg.Send("erm....issues, move along");
        }
    });
}

private static void CatMeGifCore(IResponse<TextMessage> msg, int number)
{
    msg.Http(string.Format(Url, number) + "&type=gif").GetXml((err, res, xDoc) =>{

        try
        {
            var urls = xDoc.SelectNodes("//url");
            foreach (XmlNode url in urls)
            {
                msg.Send(url.InnerText);
            }
        }
        catch (Exception)
        {
            msg.Send("erm....issues, move along");
        }
    });
}
