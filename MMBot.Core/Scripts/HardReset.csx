/**
* <description>
*     Provides a command to do a Full Reset.
* </description>
*
* <commands>
*     mmbot full reset
* </commands>
* 
* <author>
*     Anthony Compton
* </author>
*/

var robot = Require<Robot>();

robot.Respond(BuildCommand(new[]{"hardreset"}), msg => {
	robot.Reset();
});

private string BuildCommand(IEnumerable<string> parts)
{
	return string.Join(@"\s+", parts.Select(p => string.Format("({0})", p)));
}