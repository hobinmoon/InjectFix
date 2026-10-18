#!/usr/bin/env sh
# Build and run the player with IFix before the change (BASE_REF, default HEAD) and with the working tree, and compare
# usage: compare.sh
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(git -C "$HERE" rev-parse --show-toplevel)
BASE_REF=${BASE_REF:-HEAD}
BASE_SRC="$HERE/out/base_src"

rm -rf "$BASE_SRC"
mkdir -p "$BASE_SRC"
git -C "$ROOT" archive "$BASE_REF" Source/VSProj Source/UnityProj/Assets/IFix | tar -x -C "$BASE_SRC"

set +e
"$HERE/run.sh" base "$BASE_SRC/Source/VSProj"
BASE_STATUS=$?
"$HERE/run.sh" new "$ROOT/Source/VSProj"
NEW_STATUS=$?
set -e

echo "base($BASE_REF): exit $BASE_STATUS, new(working tree): exit $NEW_STATUS"
echo "results: $HERE/out/base/result.txt $HERE/out/new/result.txt"
[ $BASE_STATUS -eq 0 ] && [ $NEW_STATUS -eq 0 ]
