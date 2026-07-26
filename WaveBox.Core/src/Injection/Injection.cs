using System;
using Microsoft.Extensions.DependencyInjection;

namespace WaveBox.Core {
    public static class Injection {
        private static IServiceProvider services;

        public static IServiceProvider Services {
            get {
                if (services == null) {
                    throw new InvalidOperationException("Injection.Initialize must be called before resolving services");
                }
                return services;
            }
        }

        public static void Initialize(IServiceProvider provider) {
            services = provider;
        }

        public static T Get<T>() {
            return Services.GetRequiredService<T>();
        }
    }
}
