using System;
using System.Text.Json;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Service.Services.Http;

namespace WaveBox.Subsonic {
    // Builds and writes Subsonic envelope responses in the format the client requested:
    // XML by default (the Subsonic standard), JSON for f=json, JSONP for f=jsonp&callback=.
    public static class SubsonicWriter {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(SubsonicWriter));

        // Source-generated serializer contexts (NativeAOT-safe), compact/indented pair like HttpContextProcessor
        private static readonly SubsonicJsonContext compactJson = SubsonicJsonContext.Default;
        private static readonly SubsonicJsonContext indentedJson = new SubsonicJsonContext(new JsonSerializerOptions {
            WriteIndented = true
        });

        // A fresh "ok" envelope body; handlers set exactly one payload property on it
        public static SubsonicResponseBody Body() {
            return new SubsonicResponseBody { ServerVersion = ServerInfo.BuildVersion };
        }

        public static void Write(SubsonicRequest req, IHttpProcessor processor, SubsonicResponseBody body) {
            try {
                bool pretty = Injection.Get<IServerSettings>().PrettyJson;
                string format = (req.Get("f") ?? "xml").ToLowerInvariant();

                if (format == "json") {
                    processor.WriteText(ToJson(body, pretty), "application/json");
                    return;
                }

                if (format == "jsonp") {
                    string callback = req.Get("callback");
                    if (String.IsNullOrEmpty(callback)) {
                        // jsonp without a callback degrades to plain JSON
                        processor.WriteText(ToJson(body, pretty), "application/json");
                    } else {
                        processor.WriteText(callback + "(" + ToJson(body, pretty) + ");", "text/javascript");
                    }
                    return;
                }

                // f=xml or anything else: the Subsonic default
                processor.WriteText(SubsonicXmlSerializer.Serialize(body, pretty), "text/xml");
            } catch (Exception e) {
                logger.Error(e);
            }
        }

        public static void WriteError(SubsonicRequest req, IHttpProcessor processor, int code, string message) {
            SubsonicResponseBody body = Body();
            body.Status = "failed";
            body.Error = new SubsonicError { Code = code, Message = message };
            Write(req, processor, body);
        }

        private static string ToJson(SubsonicResponseBody body, bool pretty) {
            SubsonicJsonContext context = pretty ? indentedJson : compactJson;
            return JsonSerializer.Serialize(new SubsonicResponse(body), context.SubsonicResponse);
        }
    }
}
