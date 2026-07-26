using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WaveBox.Api;
using WaveBox.Core;
using WaveBox.Core.Model;
using WaveBox.Server;
using WaveBox.Static;

namespace WaveBox {
    public static class Program {
        // Kestrel's options are constructed during Build(), before the DI container or database are
        // ready, so the listen port is read here with a minimal, dependency-free parse of wavebox.conf.
        // The full settings load (which also touches the database) still happens in WaveBoxLifecycleService.
        private static short ReadPortFromConf() {
            const short defaultPort = 6500;
            try {
                string confPath = ServerUtility.RootPath() + "wavebox.conf";
                if (!File.Exists(confPath)) {
                    // First launch: seed the user's conf from the bundled template, same as SettingsSetup
                    string templatePath = ServerUtility.ExecutablePath() + "res" + Path.DirectorySeparatorChar + "wavebox.conf";
                    if (!File.Exists(templatePath)) {
                        return defaultPort;
                    }
                    Directory.CreateDirectory(ServerUtility.RootPath());
                    File.Copy(templatePath, confPath);
                }

                WaveBoxJsonContext context = new WaveBoxJsonContext(new JsonSerializerOptions {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
                ServerSettingsData data = JsonSerializer.Deserialize(File.ReadAllText(confPath), context.ServerSettingsData);
                return data != null && data.Port > 0 ? data.Port : defaultPort;
            } catch (Exception) {
                return defaultPort;
            }
        }

        public static void Main(string[] args) {
            // Root ORM-mapped model types so trimming/NativeAOT preserves their reflection metadata
            ModelTypeRegistry.EnsurePreserved();

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            short port = ReadPortFromConf();

            // Run as a Windows service / systemd unit when launched by the respective service manager;
            // both calls are no-ops in an interactive console.
            builder.Services.AddWindowsService(options => options.ServiceName = "WaveBox");
            builder.Services.AddSystemd();

            builder.Services.AddWaveBoxCore().AddWaveBoxServer();
            builder.Services.AddSingleton<ApiDispatcher>();
            builder.Services.AddHostedService<WaveBoxLifecycleService>();

            // Standards-compliant gzip/deflate for text responses (replaces the hand-rolled negotiation)
            builder.Services.AddResponseCompression(options => {
                options.MimeTypes = new[] {
                    "application/json", "text/html", "text/css", "text/plain",
                    "text/javascript", "application/javascript", "application/xml"
                };
            });

            builder.WebHost.ConfigureKestrel(options => {
                // Legacy API handlers write synchronously through the IHttpProcessor adapter
                options.AllowSynchronousIO = true;

                // Same POST body cap as the legacy server
                options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;

                // Port comes from the pre-parsed wavebox.conf (see ReadPortFromConf above)
                options.ListenAnyIP(port);
            });

            WebApplication app = builder.Build();

            // Initialize the static service locator before anything resolves through it
            Injection.Initialize(app.Services);

            // Route the log4net-shaped logging shim through the host's logging pipeline
            WaveBox.Core.Logging.LogManager.SetFactory(app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

            app.UseResponseCompression();

            // Single terminal handler: /api dispatch plus web UI, matching the legacy server's routing
            ApiDispatcher dispatcher = app.Services.GetRequiredService<ApiDispatcher>();
            app.Run(dispatcher.ProcessAsync);

            app.Run();
        }
    }
}
