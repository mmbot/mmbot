using System;
using System.Collections.Generic;
using Microsoft.Owin;
using Nancy;
using Nancy.Owin;

namespace MMBot.Router.Nancy
{
    public class NancyRouterModule : NancyModule
    {
        public NancyRouterModule(IRouter router)
        {
            foreach (var route in router.Routes)
            {
                switch (route.Key.Method)
                {
                    case Route.RouteMethod.Get:
                        Get[route.Key.Path] = x => (route.Value(CreateOwinContext()));
                        break;
                    case Route.RouteMethod.Delete:
                        Delete[route.Key.Path] = x => (route.Value(CreateOwinContext()));
                        break;
                    case Route.RouteMethod.Patch:
                        Patch[route.Key.Path] = x => (route.Value(CreateOwinContext()));
                        break;
                    case Route.RouteMethod.Post:
                        Post[route.Key.Path] = x => (route.Value(CreateOwinContext()));
                        break;
                    case Route.RouteMethod.Put:
                        Put[route.Key.Path] = x => (route.Value(CreateOwinContext()));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private OwinContext CreateOwinContext()
        {
            var owinContext = new OwinContext((IDictionary<string, object>)Context.Items[NancyOwinHost.RequestEnvironmentKey]);
            owinContext.Request.Body = Context.Request.Body;
            return owinContext;
        }
    }
}