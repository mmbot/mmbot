using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Nancy;
using Owin;

namespace MMBot.Router.Nancy
{
    public class NancyRouter : IRouter
    {
        
        private int _port;
        private IDisposable _webappDisposable;
        private Robot _robot;
        private bool _isConfigured;
        private readonly IDictionary<Route, Func<OwinContext, object>> _routes = new Dictionary<Route, Func<OwinContext, object>>();
        protected bool IsStarted;
        private readonly Subject<Route> _routeRegistered = new Subject<Route>();

        public NancyRouter() : this(TimeSpan.FromSeconds(10))
        {

        }

        public NancyRouter(TimeSpan restartThrottlePeriod)
        {
            _routeRegistered.Where(_ => IsStarted).Throttle(restartThrottlePeriod).Subscribe(_ =>
            {
                Stop();
                Start();
            });
        }

        public virtual IDictionary<Route, Func<OwinContext, object>> Routes
        {
            get { return _routes; }
        }

        protected Robot Robot
        {
            get { return _robot; }
        }

        public virtual void Initialize(Robot robot)
        {
            _robot = robot;
        }

        public virtual void Configure(int port)
        {
            _port = port;
            _isConfigured = true;
        }

        public virtual void Start()
        {
            if (!_isConfigured)
            {
                throw new InvalidOperationException("The router has not yet been configured. You must call Configure before calling start");
            }

            var url = string.Format("http://+:{0}", _port);
            _webappDisposable = WebApp.Start(url, app => app.UseNancy(options => options.Bootstrapper = new Bootstrapper(this)));
            
            Robot.Logger.Info(string.Format("Router (Nancy) is running on http://localhost:{0}", _port));

            IsStarted = true;
        }

        public virtual void Stop()
        {
            IsStarted = false;
            if (_webappDisposable == null)
            {
                return;
            }
            
            _webappDisposable.Dispose();
            _webappDisposable = null;
        }

        public virtual void Get(string path, Func<OwinContext, object> actionFunc)
        {
            var route = new Route{ Method = Route.RouteMethod.Get, Path = path};
            Routes.Add(route, WrapActionWithExceptionHandling(Route.RouteMethod.Get, actionFunc));
            _routeRegistered.OnNext(route);
        }

        public virtual void Get(string path, Action<OwinContext> action)
        {
            var route = new Route { Method = Route.RouteMethod.Get, Path = path };
            Routes.Add(route, WrapActionWithExceptionHandling(Route.RouteMethod.Get, action));
            _routeRegistered.OnNext(route);
        }

        public virtual void Post(string path, Func<OwinContext, object> actionFunc)
        {
            var route = new Route { Method = Route.RouteMethod.Post, Path = path };
            Routes.Add(route, WrapActionWithExceptionHandling(Route.RouteMethod.Post, actionFunc));
            _routeRegistered.OnNext(route);
        }


        public virtual void Post(string path, Action<OwinContext> action)
        {
            var route = new Route { Method = Route.RouteMethod.Post, Path = path };
            Routes.Add(route, context => WrapActionWithExceptionHandling(Route.RouteMethod.Post, action)(context));
            _routeRegistered.OnNext(route);
        }

        private Func<OwinContext, object> WrapActionWithExceptionHandling(Route.RouteMethod method, Func<OwinContext, object> actionFunc)
        {
            return context => { 
                try
                {
                    return actionFunc(context);
                }
                catch (Exception e)
                {
                    Robot.Logger.Error(string.Format("Error receiving {0} Router message", method.ToString()), e);
                    return HttpStatusCode.InternalServerError;
                }
            };
        }

        private Func<OwinContext, object> WrapActionWithExceptionHandling(Route.RouteMethod method, Action<OwinContext> actionFunc)
        {
            return context =>
            {
                try
                {
                    actionFunc(context);
                    return null;
                }
                catch (Exception e)
                {
                    Robot.Logger.Error(string.Format("Error receiving {0} Router message", method.ToString()), e);
                    return HttpStatusCode.InternalServerError;
                }
            };
        }
    }
}