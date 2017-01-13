using System;

namespace WaveBox.Service {
    public interface IService {
        // Name of service which is being managed
        string Name { get; set; }

        // Whether or not service is absolutely required for WaveBox to run
        bool Required { get; set; }

        // Whether or not service is already running
        bool Running { get; set; }

        // Service control methods
        bool Start();
        bool Stop();
    }
}
