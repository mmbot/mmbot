using System.Threading;
using Bert.RateLimiters;

namespace MMBot.Slack
{
    public class PerSecondRateLimiter
    {
        private readonly FixedTokenBucket bucket;

        public PerSecondRateLimiter(int requestsPerSecond)
        {
            bucket = new FixedTokenBucket(1, 1, 1000 / requestsPerSecond);
        }

        public void Limit()
        {
            while (true)
            {
                if (!bucket.ShouldThrottle(1))
                {
                    break;
                }

                Thread.Sleep(100);
            }
        }
    }
}
