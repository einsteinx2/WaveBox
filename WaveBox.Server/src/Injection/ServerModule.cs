using System;
using Microsoft.Extensions.DependencyInjection;
using WaveBox.ApiHandler;
using WaveBox.Core;
using WaveBox.Core.Derived;
using WaveBox.Server.Extensions;
using WaveBox.Service.Services.FileManager;
using WaveBox.Static;

namespace WaveBox.Server {
    public static class ServerModule {
        public static IServiceCollection AddWaveBoxServer(this IServiceCollection services) {
            // Database and settings
            services.AddSingleton<IDatabase, Database>();
            services.AddSingleton<IServerSettings, ServerSettings>();

            // API Authenticate and Factory
            services.AddSingleton<IApiAuthenticate, ApiAuthenticate>();
            services.AddSingleton<IApiHandlerFactory, ApiHandlerFactory>();

            // Web client with 5 second timeout (was TimedWebClient/LinuxWebClient under Mono)
            services.AddSingleton<IWebClient>(sp => new HttpClientWebClient(5000));

            // File watcher (FileSystemWatcher is FSEvents-backed on macOS in modern .NET)
            services.AddSingleton<IFileManager, FileManager>();

            return services;
        }
    }
}
