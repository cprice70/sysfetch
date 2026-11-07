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

## Build

```bash
dotnet publish sysfetch -c Release -o out
./out/sysfetch
```

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
