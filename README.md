# mmbot

## Overview
mmbot is an attempt to port github's [Hubot](http://www.github.com/github/hubot) to C#.

## Goals
The main goal of mmbot is to provide a chat bot written in C# with all the functionality of Hubot but with a script environment more familiar to .Net devs. Another major goal is that hubot scripts should be easy to convert into mmbot scripts. This may mean that some weird design choices are made in the API but it should still be very usable, customizable and familiar to .Net devs

## Current Script Implementations


    mmbot animate me <query> - The same thing as `image me`, except adds a few parameters to try to return an animated GIF instead.
    mmbot convert me <expression> to <units> - Convert expression to given units.
    mmbot die - End mmbot process
    mmbot echo <text> - Reply back with <text>
    mmbot gif me <query> - Returns an animated gif matching the requested search term.
    mmbot help - Displays all of the help commands that mmbot knows about.
    mmbot help <query> - Displays all help commands that match <query>.
    mmbot image me <query> - The Original. Queries Google Images for <query> and returns a random top result.
    mmbot map me <query> - Returns a map view of the area returned by `query`.
    mmbot math me <expression> - Calculate the given expression.
    mmbot mustache me <query> - Searches Google Images for the specified query and mustaches it.
    mmbot mustache me <url> - Adds a mustache to the specified URL.
    mmbot mute/unmute - turn the volume on/off
    mmbot ping -  Reply with pong
    mmbot pug bomb N - get N pugs
    mmbot pug me - Receive a pug
    mmbot spot me <query> - Show the top spotify track result for my query
    mmbot spot me winning - Show the best track ever on spotify
    mmbot spotify clear queue - clears the play queue
    mmbot spotify next - Skips to the next track in the queue.
    mmbot spotify pause - Pauses playback
    mmbot spotify play <query> -  Plays the first matching track from spotify.
    mmbot spotify queue <query> -  Queues the first matching track from spotify.
    mmbot spotify remove <query> from queue - Removes matching tracks from the queue
    mmbot spotify show queue
    mmbot the rules - Make sure mmbot still knows the rules.
    mmbot time - Reply with current time
    mmbot translate me <phrase> - Searches for a translation for the <phrase> and then prints that bad boy out.
    mmbot translate me from <source> into <target> <phrase> - Translates <phrase> from <source> into <target>. Both <source> and <target> are optional
    mmbot turn it down [to 11] - shhhh I'm thinking, optionally provide the volume out of 100
    mmbot turn it up [to 66] - crank it baby, optionally provide the volume out of 100
    mmbot urban define me <term>  - Searches Urban Dictionary and returns definition
    mmbot urban example me <term> - Searches Urban Dictionary and returns example
    mmbot urban me <term>         - Searches Urban Dictionary and returns definition
    mmbot what is <term>?         - Searches Urban Dictionary and returns definition
    mmbot xkcd [latest]- The latest XKCD comic
    mmbot xkcd <num> - XKCD comic <num>
    mmbot xkcd random - fetch a random XKCD comic
    mmbot youtube me <query> - Searches YouTube for the query and returns the video embed link.


## Using from scriptcs
You can use mmbot directly from [scriptcs](http://scriptcs.net). For example, create the following script in a folder and name the file mmbot.csx:

```C#
using MMBot;
using MMBot.Jabbr;

var config = new Dictionary<string, string> {
  {"MMBOT_JABBR_HOST", "https://jabbr.net"},
  {"MMBOT_JABBR_NICK", "mmbot"},
  {"MMBOT_JABBR_PASSWORD", "mysuperawesomepassword"},
  {"MBOT_JABBR_ROOMS", "mmbottest"},
};

var robot = Robot.Create<JabbrAdapter>("mmbot", config);

robot.LoadScripts(typeof (Robot).Assembly);

robot.Run().Wait();

Console.WriteLine("Press any key to exit");

Console.ReadKey();
```

You can now just type the following to run your script

    scriptcs -install MMBot.Jabbr -pre
    scriptcs .\mmbot.csx


