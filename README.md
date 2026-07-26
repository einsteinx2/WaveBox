WaveBox
=======

A free, open source personal media server written in C# and available on all platforms thanks to the Mono framework.

Licensed under [The GNU General Public License v3.0](https://www.gnu.org/licenses/gpl.html "Permalink to The GNU General Public License v3.0 - GNU Project"), for more information please read the [LICENSE.md](https://github.com/einsteinx2/WaveBox/blob/main/LICENSE.md) file in this repository or visit the preceding link to the GNU website.

API
---

WaveBox features a very extensible JSON API, and is built with API developers in mind!  Full API documentation can be found in [API_DOCS.md](https://github.com/einsteinx2/WaveBox/blob/main/API_DOCS.md).

Testing
-------

The test suite lives under `tests/` and uses xUnit v3:

* `WaveBox.Core.Tests` — pure unit tests for the core library.
* `WaveBox.Server.Tests` — server unit tests plus integration tests against a real temporary SQLite database (integration tests run serialized; they isolate all state via the `WAVEBOX_ROOT`/`WAVEBOX_TEMP` environment overrides).
* `WaveBox.E2E.Tests` — black-box tests that boot a real server process on a random port with a generated MP3 fixture and drive the legacy `/api` and OpenSubsonic `/rest` APIs over HTTP.

Run everything locally with:

```sh
dotnet test
```

By default the E2E suite launches the locally built (JIT) server. To run it against a NativeAOT-published binary — which is what CI does, since trimming/rooting regressions only appear there — point it at the binary:

```sh
dotnet publish WaveBox.Server -c Release -r osx-arm64
WAVEBOX_E2E_BINARY=$PWD/WaveBox.Server/bin/Release/net10.0/osx-arm64/publish/WaveBox.Server \
  dotnet test tests/WaveBox.E2E.Tests -c Release
```

The transcoded-stream E2E test skips automatically when `ffmpeg` is not installed.

Clients
-------

Though WaveBox is currently under heavy development, several clients and libraries exist which can interact with a WaveBox server. If you wish to have a client included in this list, please send a pull request!

1. [WaveBoxWebClient](https://github.com/einsteinx2/WaveBoxWebClient) - The official HTML5 web client for WaveBox.  GPL3 licensed.
2. [php-wavebox](https://github.com/mdlayher/php-wavebox) - A PHP 5.4+ WaveBox API client library. MIT licensed.
2. [rb-wavebox](https://github.com/mdlayher/rb-wavebox) - A Ruby WaveBox API client library. MIT licensed.

IRC
---

Have questions about the project, or want to help out?  Come talk to us on [irc.freenode.net at #wavebox](irc://irc.freenode.net/wavebox)!
