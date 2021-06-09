using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mh_dl
{
    public static class AppEngine
    {
        private static readonly object locker = new object();
        private static IServiceProvider provider;

        public static IServiceCollection Services { get; } = new ServiceCollection();

        public static IServiceProvider Provider
        {
            get
            {
                if (provider == null)
                {
                    lock (locker)
                    {
                        if (provider == null)
                        {
                            provider = Services.BuildServiceProvider();
                        }
                    }
                }
                return provider;
            }
        }
        public static T GetService<T>()
        {
            return Provider.GetService<T>();
        }
        public static T GetRequiredService<T>()
        {
            return Provider.GetRequiredService<T>();
        }

    }
}
