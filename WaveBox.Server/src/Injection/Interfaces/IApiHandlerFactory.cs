using System;
using System.Collections.Generic;
using WaveBox.ApiHandler;
using WaveBox.Core.Model;

namespace WaveBox.Server {
    public interface IApiHandlerFactory {
        IApiHandler CreateApiHandler(string name);

        void Initialize();
    }
}
