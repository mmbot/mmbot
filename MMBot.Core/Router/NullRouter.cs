using System;
using System.Collections.Generic;
using Microsoft.Owin;

namespace MMBot.Router
{
    public class NullRouter : IRouter
    {

        public NullRouter()
        {
            Routes = new Dictionary<Route, Func<OwinContext, object>>();
        }

        public void Configure(Robot robot, int port)
        {
            
        }

        public void Start()
        {
            
        }

        public void Stop()
        {
            
        }

        public void Get(string path, Func<OwinContext, object> actionFunc)
        {
            
        }

        public void Post(string path, Func<OwinContext, object> actionFunc)
        {
            
        }

        public IDictionary<Route, Func<OwinContext, object>> Routes { get; private set; }
    }
}