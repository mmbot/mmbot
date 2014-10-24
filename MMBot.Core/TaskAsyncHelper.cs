using System.Threading.Tasks;

namespace MMBot
{
    public class TaskAsyncHelper
    {
        private static readonly Task _emptyTask = Task.FromResult<byte>(0);

        public static Task Empty
        {
            get
            {
                return _emptyTask;
            }
        }
    }
}