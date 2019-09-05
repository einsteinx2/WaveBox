using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;

namespace WaveBox.ApiHandler {
    public interface IApiHandler {
        // API handler's name
        string Name { get; }

        // API handler's permission check function
        bool CheckPermission(User user, string action);

        // API handler's action
        void Process(UriWrapper uri, IHttpProcessor processor, User user);
    }
}
