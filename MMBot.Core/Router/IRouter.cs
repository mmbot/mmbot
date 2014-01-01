using System;
using System.Collections.Generic;
using Autofac;
using Microsoft.Owin;

namespace MMBot.Router
{

    public interface IRouter
    {
        void Configure(Robot robot, int port);

        void Start();

        void Stop();

        void Get(string path, Func<OwinContext, object> actionFunc);

        void Get(string path, Action<OwinContext> action);

        void Post(string path, Func<OwinContext, object> actionFunc);

        void Post(string path, Action<OwinContext> action);

        IDictionary<Route, Func<OwinContext, object>> Routes { get; }
    }
}