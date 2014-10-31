using System;
using System.Collections.Generic;
using Microsoft.Owin;

namespace MMBot.Router
{

    public interface IRouter : IMustBeInitializedWithRobot
    {
        void Configure(int port);

        void Start();

        void Stop();

        void Get(string path, Func<OwinContext, object> actionFunc);

        void Get(string path, Action<OwinContext> action);

        void Post(string path, Func<OwinContext, object> actionFunc);

        void Post(string path, Action<OwinContext> action);

        IDictionary<Route, Func<OwinContext, object>> Routes { get; }

    }
}