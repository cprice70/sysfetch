#!/usr/bin/env bash
# Render the Scoop manifest for a release from its .sha256 files.
#
# Usage: scripts/render-scoop.sh <version> <dist-dir> [owner/repo]
#
# Expects <dist-dir> to contain sysfetch-<version>-<rid>.zip.sha256 for
# win-x64 and win-arm64. Writes the manifest (sysfetch.json) to stdout.
set -euo pipefail

if [[ $# -lt 2 || $# -gt 3 ]]; then
  echo "usage: $0 <version> <dist-dir> [owner/repo]" >&2
  exit 2
fi

version="$1"
dist_dir="$2"
repo="${3:-${GITHUB_REPOSITORY:-cprice70/sysfetch}}"

sha() {
  local file="$dist_dir/sysfetch-${version}-$1.zip.sha256"
  if [[ ! -f "$file" ]]; then
    echo "error: missing $file" >&2
    exit 1
  fi
  local hash
  hash="$(cut -d' ' -f1 < "$file")"
  if [[ ! "$hash" =~ ^[0-9a-f]{64}$ ]]; then
    echo "error: bad sha256 in $file" >&2
    exit 1
  fi
  echo "$hash"
}

win_x64="$(sha win-x64)"
win_arm64="$(sha win-arm64)"
releases="https://github.com/${repo}/releases/download"

# $version and $baseurl in the autoupdate block are Scoop placeholders, not shell variables.
cat <<EOF
{
    "version": "${version}",
    "description": "Fast system information tool (Native AOT .NET)",
    "homepage": "https://github.com/${repo}",
    "license": "MIT",
    "architecture": {
        "64bit": {
            "url": "${releases}/v${version}/sysfetch-${version}-win-x64.zip",
            "hash": "${win_x64}"
        },
        "arm64": {
            "url": "${releases}/v${version}/sysfetch-${version}-win-arm64.zip",
            "hash": "${win_arm64}"
        }
    },
    "bin": "sysfetch.exe",
    "checkver": "github",
    "autoupdate": {
        "architecture": {
            "64bit": {
                "url": "${releases}/v\$version/sysfetch-\$version-win-x64.zip"
            },
            "arm64": {
                "url": "${releases}/v\$version/sysfetch-\$version-win-arm64.zip"
            }
        },
        "hash": {
            "url": "\$baseurl/SHA256SUMS"
        }
    }
}
EOF
