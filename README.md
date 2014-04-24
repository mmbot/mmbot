[![Stories in You Take It](https://badge.waffle.io/mmbot/mmbot.png?label=you%20take%20it)](https://waffle.io/mmbot/mmbot)   
![Build Status](http://teamcity.codebetter.com/app/rest/builds/buildType:\(id:bt1169\)/statusIcon)    
[![Gitter chat](https://badges.gitter.im/mmbot/mmbot.png)](https://gitter.im/mmbot/mmbot)

# mmbot

## Overview
mmbot is a port of github's [Hubot](http://www.github.com/github/hubot) to C#.

![mmbot](Docs/Images/mmbot.avatar.png)

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
# chocolatey can install mmbot globally
cinst mmbot

# Now create a folder to host the scripts and config, then from that dir...
mmbot --init

# You're ready to go...
mmbot


```

When you need an adapter to talk to your chat rooms

```PowerShell
nuget install mmbot.jabbr -o packages
```

When you want a script that is in nuget use the nuget command line in your path (installed via `cinst nuget.commandline`)

```PowerShell
nuget install mmbot.scriptit -o packages
```
...or simply drop the .csx file in the "scripts" folder
...or even better use the scriptthis and scriptthat scripts to input them inline or pull from a gist!!!

For more info read the [getting started guide](https://github.com/PeteGoo/mmbot/wiki/Getting-Started)

## Adapters
Currently adapters exist for [jabbr](https://jabbr.net), [HipChat](https://www.hipchat.com/), [Slack](http://slack.com) and [XMPP](https://www.google.com?#q=xmpp) but with plans to add a CampFire adapter soon. The implementation is extremely similar to Hubot so other adapters could easily be added. Learn how to get your preferred adapter up and running in [configuring mmbot](https://github.com/mmbot/mmbot/wiki/Configuring-mmbot).

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

The script catalog is available at [mmbot.github.io/mmbot.scripts](http://mmbot.github.io/mmbot.scripts)

You can also search the script catalog from within mmbot and even install scripts from there.

```
# List the scripts in the catalog
mmbot scripts

# Install a script (Pug)
mmbot download script Pug
```





