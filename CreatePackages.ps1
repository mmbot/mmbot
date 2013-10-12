$version = $args[0]

if(!$version){
	throw "You need to specify a version number"
}

msbuild .\mmbot.sln /property:Configuration=Release


.\.nuget\nuget.exe pack .\mmbot\mmbot.csproj -Version $version -Properties Configuration=Release
.\.nuget\nuget.exe pack .\mmbot.jabbr\mmbot.jabbr.csproj -Version $version -Properties Configuration=Release
.\.nuget\nuget.exe pack .\mmbot.hipchat\mmbot.hipchat.csproj -Version $version -Properties Configuration=Release