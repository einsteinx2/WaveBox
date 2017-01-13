using System;
using Ninject.Modules;
using WaveBox.ApiHandler;

namespace WaveBox.Server {
    public class ServerModule : NinjectModule {
        public override void Load() {
            // API Authenticate and Factory
            Bind<IApiAuthenticate>().To<ApiAuthenticate>().InSingletonScope();
            Bind<IApiHandlerFactory>().To<ApiHandlerFactory>().InSingletonScope();
        }
    }
}
