
var robot = Require<Robot>();

private string[] _rules =
{
    "1. A robot may not injure a human being or, through inaction, allow a human being to come to harm.",
    "2. A robot must obey any orders given to it by human beings, except where such orders would conflict with the First Law.",
    "3. A robot must protect its own existence as long as such protection does not conflict with the First or Second Law."
};

private string[] _otherRules =
{
    "A developer may not injure Apple or, through inaction, allow Apple to come to harm.",
    "A developer must obey any orders given to it by Apple, except where such orders would conflict with the First Law.",
    "A developer must protect its own existence as long as such protection does not conflict with the First or Second Law."
};

robot.Respond(@"(what are )?the (three |3 )?(rules|laws)", msg =>
{
    var rules = msg.Message != null && msg.Message.Text != null && (msg.Message.Text.ToLower().Contains("apple") || msg.Message.Text.ToLower().Contains("dev"))
        ? _otherRules
        : _rules;
                
    msg.Send(string.Join(Environment.NewLine, rules));
});


robot.AddHelp("mmbot the rules - Make sure mmbot still knows the rules.");
