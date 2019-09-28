using System;

namespace Utils
{
    public static class EnvironmentHelperExtensions
    {
        public static (bool exists, bool value) GetEnvironmentVariable(this string name,bool defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(name);
            if (env == null) return (false,defaultValue);
            return (true,bool.Parse(env));
        }
        public static (bool exists, string value) GetEnvironmentVariable(this string name, string defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(name);
            if (env == null) return (false, defaultValue);
            return (true, env);
        }
        public static (bool exists, int value) GetEnvironmentVariable(this string name, int defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(name);
            if (env == null) return (false, defaultValue);
            return (true, int.Parse(env));
        }
    }
}
