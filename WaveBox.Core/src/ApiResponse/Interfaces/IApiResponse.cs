using System;

namespace WaveBox.Core.ApiResponse {
    public interface IApiResponse {
        // The required error field on any API response
        string Error { get; set; }
    }
}
