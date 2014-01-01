using System;

namespace MMBot.Router
{
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