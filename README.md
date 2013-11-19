# mmbot

## Overview
mmbot is an attempt to port github's [Hubot](http://www.github.com/github/hubot) to C#.

## Goals
1.  __Provide a chat bot written in C# with all the functionality of Hubot but with a script environment more familiar to .Net devs.__ (done)
2. __Hubot scripts should be easy to convert into mmbot scripts.__ (done)    
This may mean that some weird design choices are made in the API but it should still be very usable, customizable and familiar to .Net devs
3. __ScriptCS style scripts should be automatically picked up and run from a scripts folder__ (done)
4. __Eventually provide the ability to run from scriptcs__. (blocked)    
There are some blockers here in the NuGet package resolution and [dynamic loading of scripts](https://github.com/scriptcs/scriptcs/issues/243)

## Getting started    

The best plan is to use [chocolatey](https://chocolatey.org/)...

```PowerShell
# install scriptcs if you like. mmbot includes what it needs from scriptcs but it makes package installation easier
cinst scriptcs

# chocolatey can install mmbot globally
cinst mmbot

# Now create a folder to host the scripts and config, then from that dir...
mmbot --init

# You're ready to go...
mmbot


```

When you need an adapter to talk to your chat rooms

```PowerShell
scriptcs install mmbot.jabbr
```
or if you don't have scriptcs...
```PowerShell
nuget install mmbot.jabbr -o packages
```

When you want a script that is in nuget

```PowerShell
scriptcs install mmbot.scriptit
```
...or simply drop the .csx file in the "scripts" folder
...or even better use the scriptthis and scriptthat scripts to input them inline or pull from a gist!!!

For more info read the [getting started guide](https://github.com/PeteGoo/mmbot/wiki/Getting-Started)

## Adapters
Currently the only implemented adapter is for [jabbr](https://jabbr.net) but the implementation is extremely similar to Hubot so other adapters could easily be added.

## Scripts
Writing scripts is easy. You can either implement the IMMBotScript interface and register your script or you can write a simple [scriptcs](http://www.scriptcs.net) script and drop it into a scripts folder beside the MMBot runner executable.

Here is a simple script that responds to "mmbot yo" with "sup?"

``` c#
var robot = Require<Robot>();

robot.Respond("yo", msg => msg.Send("sup?"));
```

This script is a port of the Hubot math script

```c#

var robot = Require<Robot>();

robot.Respond(@"(calc|calculate|calculator|convert|math|maths)( me)? (.*)", msg =>
	{
	    msg
	    .Http("https://www.google.com/ig/calculator")
        .Query(new
        {
            hl = "en",
            q = msg.Match[3]
        })
        .Headers(new Dictionary<string, string>
        {
            {"Accept-Language", "en-us,en;q=0.5"},
            {"Accept-Charset", "utf-8"},
            {"User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:2.0.1) Gecko/20100101 Firefox/4.0.1"}
        })
        .GetJson((err, res, body) => 
        {
        	if(err != null)
        	{
        		msg.Send("Could not compute");
        	}
        	else 
        	{
        		msg.Send((string)body["rhs"] ?? "Could not compute");
        	}
        });
});

robot.AddHelp(
    "mmbot math me <expression> - Calculate the given expression.",
    "mmbot convert me <expression> to <units> - Convert expression to given units."
);
```

## Current Script Implementations


    mmbot animate me <query> - The same thing as `image me`, except adds a few parameters to try to return an animated GIF instead.
    mmbot ascii me <query> - Returns ASCII art of the query text.
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
    mmbot spotify play <spotifyUri> -  Plays the track(s) from the spotify URI (supports tracks, albums and playlists).
    mmbot spotify play album <query> -  Plays the first matching album from spotify.
    mmbot spotify queue <query> -  Queues the first matching track from spotify.
    mmbot spotify queue <spotifyUri> -  Queues the track(s) from the spotify URI (supports tracks, albums and playlists).
    mmbot spotify queue album <query> -  Queues the first matching album from spotify.
    mmbot spotify remove <query> from queue - Removes matching tracks from the queue
    mmbot spotify show artist|album|playlist <name> - Shows the details of the first matching artist, album or playlist
    mmbot spotify show queue
    mmbot spotify shuffle on|off - turn on or off shuffle mode
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






