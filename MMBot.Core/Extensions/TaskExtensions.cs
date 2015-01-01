using System;
using System.Threading.Tasks;
using Common.Logging;

namespace MMBot
{
    public static class TaskExtensions
    {
        public static Task<T> Catch<T, TError>(this Task<T> task, Func<TError, T> onError) where TError : Exception
        {
            var tcs = new TaskCompletionSource<T>();

            task.ContinueWith(ant =>
            {
                if (task.IsFaulted && task.Exception.InnerException is TError)
                    tcs.SetResult(onError((TError)task.Exception.InnerException));
                else if (ant.IsCanceled)
                    tcs.SetCanceled();
                else if (task.IsFaulted)
                    tcs.SetException(ant.Exception.InnerException);
                else
                    tcs.SetResult(ant.Result);
            });
            return tcs.Task;
        }

        public static Task<T> Catch<T, TError>(this Task<T> task, Action<TError> onError) where TError : Exception
        {
            return task.Catch<T, TError>(err => { onError(err); return default(T); });
        }

        public static Task CatchAndLog(this Task task, ILog logger, string logMessage, params object[] messageArgs)
        {
            return task.ToTaskOfT<object>().Catch<object, Exception>(e => logger.ErrorFormat(logMessage, e, messageArgs));
        }

        public static Task<T> ToTaskOfT<T>(this Task t)
        {
            var taskOfT = t as Task<T>;
            if (taskOfT != null) return taskOfT;
            var tcs = new TaskCompletionSource<T>();
            t.ContinueWith(ant =>
            {
                if (ant.IsCanceled) tcs.SetCanceled();
                else if (ant.IsFaulted) tcs.SetException(ant.Exception.InnerException);
                else tcs.SetResult(default(T));
            });
            return tcs.Task;
        }
    }
}