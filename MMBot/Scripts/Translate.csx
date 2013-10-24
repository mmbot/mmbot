
var robot = Require<Robot>();


private Dictionary<string, string> _languages = new Dictionary<string, string>
{
    {"af", "Afrikaans"},
    {"sq", "Albanian"},
    {"ar", "Arabic"},
    {"az", "Azerbaijani"},
    {"eu", "Basque"},
    {"bn", "Bengali"},
    {"be", "Belarusian"},
    {"bg", "Bulgarian"},
    {"ca", "Catalan"},
    {"zh-CN", "Simplified Chinese"},
    {"zh-TW", "Traditional Chinese"},
    {"hr", "Croatian"},
    {"cs", "Czech"},
    {"da", "Danish"},
    {"nl", "Dutch"},
    {"en", "English"},
    {"eo", "Esperanto"},
    {"et", "Estonian"},
    {"tl", "Filipino"},
    {"fi", "Finnish"},
    {"fr", "French"},
    {"gl", "Galician"},
    {"ka", "Georgian"},
    {"de", "German"},
    {"el", "Greek"},
    {"gu", "Gujarati"},
    {"ht", "Haitian Creole"},
    {"iw", "Hebrew"},
    {"hi", "Hindi"},
    {"hu", "Hungarian"},
    {"is", "Icelandic"},
    {"id", "Indonesian"},
    {"ga", "Irish"},
    {"it", "Italian"},
    {"ja", "Japanese"},
    {"kn", "Kannada"},
    {"ko", "Korean"},
    {"la", "Latin"},
    {"lv", "Latvian"},
    {"lt", "Lithuanian"},
    {"mk", "Macedonian"},
    {"ms", "Malay"},
    {"mt", "Maltese"},
    {"no", "Norwegian"},
    {"fa", "Persian"},
    {"pl", "Polish"},
    {"pt", "Portuguese"},
    {"ro", "Romanian"},
    {"ru", "Russian"},
    {"sr", "Serbian"},
    {"sk", "Slovak"},
    {"sl", "Slovenian"},
    {"es", "Spanish"},
    {"sw", "Swahili"},
    {"sv", "Swedish"},
    {"ta", "Tamil"},
    {"te", "Telugu"},
    {"th", "Thai"},
    {"tr", "Turkish"},
    {"uk", "Ukrainian"},
    {"ur", "Urdu"},
    {"vi", "Vietnamese"},
    {"cy", "Welsh"},
    {"yi", "Yiddish"}
};


var languageChoices = string.Join("|", _languages.Select(kvp => kvp.Value));

var regex = string.Format("translate(?: me)?" +
            "(?: from ({0}))?" +
            "(?: (?:in)?to ({0}))?" +
            "(.*)", languageChoices);

robot.Respond(regex, msg =>
{
    var term = "\"" + msg.Match[3].Trim() + "\"";
    var origin = GetCode(msg.Match[1]) ?? "auto";
    var target = GetCode(msg.Match[2]) ?? "en";

    msg.Http("https://translate.google.com/translate_a/t")
       .Query(new
        {
            client = "t",
            hl = "en",
            multires = 1,
            sc = 1,
            sl = origin,
            ssel = 0,
            tl = target,
            tsel = 0,
            uptl = "en",
            text = term
        })
        .Headers(new Dictionary<string, string> {{"User-Agent", "Mozilla/5.0"}})
        .GetJson((err, res, body) => {

            var language = _languages[(string) body[2]];
            string result;
            try
            {
                result = body[0][0][0].ToString();
            }
            catch (Exception)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(msg.Match[2]))
            {
                msg.Send(string.Format("{0} is {1} for {2}", term, language, result.Trim()));
            }
            else
            {
                msg.Send(string.Format("The {1} {0} translates as {2} in {3}", term, language, result.Trim(), _languages[target]));
            }
    });
});

robot.AddHelp(
    "mmbot translate me <phrase> - Searches for a translation for the <phrase> and then prints that bad boy out.",
    "mmbot translate me from <source> into <target> <phrase> - Translates <phrase> from <source> into <target>. Both <source> and <target> are optional"
);

private string GetCode(string languageName)
{
    return
        _languages.Where(l => string.Equals(l.Value, languageName, StringComparison.InvariantCultureIgnoreCase))
            .Select(l => l.Key)
            .FirstOrDefault();
}

