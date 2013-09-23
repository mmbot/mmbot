using System.Text.RegularExpressions;

namespace MMBot
{
    public class MatchResult
    {

        public MatchResult(bool isMatch, MatchCollection match = null)
        {
            IsMatch = isMatch;
            Match = match;
        }

        public bool IsMatch { get; private set; }
        public MatchCollection Match { get; private set; }
    }
}