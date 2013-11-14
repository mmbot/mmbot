using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot
{
    public class TaskAsyncHelper
    {
        private static readonly Task _emptyTask = MakeEmpty();

        private static Task MakeEmpty()
        {
            return FromResult<object>(null);
        }

        public static Task Empty
        {
            get
            {
                return _emptyTask;
            }
        }

        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }
    }
}
