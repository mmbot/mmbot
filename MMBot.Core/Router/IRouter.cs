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
        IDictionary<Route, Func<OwinContext, object>> Routes { get; }
    }

    public struct Route : IEquatable<Route>
    {
        public bool Equals(Route other)
        {
            return string.Equals(Path, other.Path) && Method == other.Method;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Route && Equals((Route)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Path != null ? Path.GetHashCode() : 0) * 397) ^ (int)Method;
            }
        }

        public string Path { get; set; }

        public RouteMethod Method { get; set; }

        public enum RouteMethod
        {
            Get,
            Delete,
            Patch,
            Post,
            Put
        }
    }
}