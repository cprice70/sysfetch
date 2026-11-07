#!/usr/bin/env bash
# Package a published sysfetch binary for release.
#
# Usage: scripts/package.sh <version> <rid> <publish-dir> <dist-dir>
#
# Produces <dist-dir>/sysfetch-<version>-<rid>.tar.gz (or .zip for win-*)
# plus a matching .sha256 file containing "<hash>  <asset name>".
set -euo pipefail

if [[ $# -ne 4 ]]; then
  echo "usage: $0 <version> <rid> <publish-dir> <dist-dir>" >&2
  exit 2
fi

version="$1"
rid="$2"
publish_dir="$3"
dist_dir="$4"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

mkdir -p "$dist_dir"
dist_dir="$(cd "$dist_dir" && pwd)"

staging="$(mktemp -d)"
trap 'rm -rf "$staging"' EXIT

if [[ "$rid" == win-* ]]; then
  binary="sysfetch.exe"
else
  binary="sysfetch"
fi

if [[ ! -f "$publish_dir/$binary" ]]; then
  echo "error: $publish_dir/$binary not found" >&2
  exit 1
fi

cp "$publish_dir/$binary" "$staging/"
cp "$repo_root/README.md" "$staging/"
[[ -f "$repo_root/LICENSE" ]] && cp "$repo_root/LICENSE" "$staging/"

name="sysfetch-${version}-${rid}"
if [[ "$rid" == win-* ]]; then
  asset="$name.zip"
  if command -v 7z >/dev/null 2>&1; then
    (cd "$staging" && 7z a -tzip -bso0 "$dist_dir/$asset" ./*)
  else
    (cd "$staging" && zip -q "$dist_dir/$asset" ./*)
  fi
else
  asset="$name.tar.gz"
  chmod 755 "$staging/$binary"
  (cd "$staging" && tar -czf "$dist_dir/$asset" ./*)
fi

if command -v sha256sum >/dev/null 2>&1; then
  (cd "$dist_dir" && sha256sum "$asset" > "$asset.sha256")
else
  (cd "$dist_dir" && shasum -a 256 "$asset" > "$asset.sha256")
fi

echo "$dist_dir/$asset"
