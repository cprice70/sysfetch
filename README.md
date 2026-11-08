# sysfetch

Sysfetch is a neofetch-inspired tool for displaying system information. It is written in C#,
targets .NET 10 and is published as a Native AOT binary for Windows, Linux and macOS.
It is not intended to be a full replacement for neofetch or fastfetch.

## Features

* Native AOT binary with no .NET runtime required; values are read directly from
  `/proc`, `/sys`, `sysctl`, IOKit/CoreGraphics or the Windows registry and Win32 APIs
  instead of spawning helper commands.
* Probes run in parallel under a shared timeout, so one slow source can't stall the output.
* Distro logos with ANSI colors (respects `NO_COLOR` and plain output when piped).
* Developer environment lines: .NET SDK, Node, Python (venv/pyenv aware), Android SDK
  and the current git repo/branch. Versions are read from install layouts on disk, so
  they cost almost nothing; a `--version` call is only a fallback.
* `--json` output for scripts.
* Optional JSON config file for logo, colors, separator and which lines to show.

## Install

Homebrew (macOS and Linux):

```bash
brew install cprice70/sysfetch/sysfetch
```

Scoop (Windows x64 and arm64):

```powershell
scoop bucket add sysfetch https://github.com/cprice70/scoop-sysfetch
scoop install sysfetch
```

Or download the archive for your platform from
[Releases](https://github.com/cprice70/sysfetch/releases): `osx-arm64`, `osx-x64`,
`linux-x64`, `linux-arm64`, `win-x64`, `win-arm64`. Each archive has a single
self-contained binary; `SHA256SUMS` lists the checksums.

## Build

Requires the .NET 10 SDK (plus `clang` and `zlib1g-dev` on Linux for Native AOT).

```bash
dotnet publish sysfetch -c Release -o out
./out/sysfetch
```

## Releasing

Push a tag such as `v0.4.0`. The `Release` workflow builds Native AOT binaries for all six
targets, publishes a GitHub Release with checksums, and renders the Homebrew formula
and the Scoop manifest. If the `HOMEBREW_TAP_TOKEN` secret is set (a fine-grained token
with Contents write access to `cprice70/homebrew-sysfetch` and `cprice70/scoop-sysfetch`),
the formula is pushed to the tap and the manifest to the bucket; otherwise both are kept
as workflow artifacts. Set `SCOOP_BUCKET_TOKEN` only if the bucket needs a separate token.
Tags containing `-` (e.g. `v0.5.0-rc.1`) are marked as pre-releases and skip the tap and
bucket updates.

To try the release build without publishing, run the `Release` workflow manually from the
Actions tab (or `gh workflow run release.yml -f version=0.4.0-test`). It builds and packages
all six targets and keeps the archives as workflow artifacts; the release, tap and bucket jobs are
skipped. Every CI run also uploads the Native AOT binary for each OS as an artifact.

## Usage

```
sysfetch [options]

  --json               Print system info as JSON
  --logo <name>        Logo to show: auto, none, apple, windows, linux,
                       arch, ubuntu, debian, fedora
  --no-logo            Same as --logo none
  --color <mode>       auto, always or never (auto honours NO_COLOR)
  --no-color           Same as --color never
  --config <path>      Read settings from this file
  --no-config          Ignore the config file
  --gen-config [path]  Write a default config file and exit
  --list-logos         List available logos
  -v, --version        Show version
  -h, --help           Show this help
```

## Configuration

Run `sysfetch --gen-config` to write a commented default config to
`$XDG_CONFIG_HOME/sysfetch/config.json` (falls back to `~/.config/sysfetch/config.json`;
`%APPDATA%\sysfetch\config.json` on Windows). Command-line flags override the file.

```jsonc
{
  "logo": "auto",          // auto, none, apple, windows, linux, arch, ubuntu, debian, fedora
  "color": "auto",         // auto, always, never
  "keyColor": "auto",      // auto uses the logo color; or red, brightblue, 208, ...
  "separator": ": ",
  "timeoutMs": 1000,
  "modules": ["title", "underline", "os", "host", "kernel", "uptime",
              "resolution", "cpu", "gpu", "memory",
              "dotnet", "node", "python", "android", "git", "blank", "colors"]
}
```

## License

MIT. See [LICENSE](LICENSE).
