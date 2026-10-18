#!/usr/bin/env sh
# Build IFix.Core/IFix.exe from the given IFix sources (Source/VSProj directory), build and run an IL2CPP player in UnityProj,
# and write the results to out/<label>/result.txt
#   1. VM bench: UnityProj/Assets/IFixBench/VmBench/Src~ is compiled as Native and Interp; Interp is injected with no_cfg
#   2. workflow: UnityProj's Assembly-CSharp is injected by the IFix editor tools, then a patch is generated with IFIX_PATCHED defined
# usage: run.sh <label> <VSProj_dir>
#   e.g. run.sh new ../VSProj
# Set UNITY_APP to choose the Unity.app
# Set BENCH_PLATFORM=ios to run on the iOS Simulator instead of macOS Standalone (SIM_DEVICE: simulator UDID)
set -e
if [ $# -lt 2 ]; then
    echo "usage: $0 <label> <VSProj_dir>"
    exit 2
fi
HERE=$(cd "$(dirname "$0")" && pwd)
LABEL=$1
SRC=$(cd "$2" && pwd)
UNITY_APP=${UNITY_APP:-/Applications/Unity/Hub/Editor/6000.3.13f1/Unity.app}
BENCH_PLATFORM=${BENCH_PLATFORM:-macos}
if [ "$BENCH_PLATFORM" = "ios" ]; then
    BUILD_TARGET=iOS
else
    BUILD_TARGET=OSXUniversal
fi
SCRIPTING="$UNITY_APP/Contents/Resources/Scripting"
if [ ! -d "$SCRIPTING" ]; then
    SCRIPTING="$UNITY_APP/Contents"
fi
MCS="$SCRIPTING/MonoBleedingEdge/bin/mcs"
MONO="$SCRIPTING/MonoBleedingEdge/bin/mono"
ENGINE_DIR="$SCRIPTING/Managed/UnityEngine"
FACADES_DIR="$SCRIPTING/MonoBleedingEdge/lib/mono/4.7.1-api/Facades"
OUT="$HERE/out/$LABEL"
PROJ="$HERE/UnityProj"
BENCH_SRC="$PROJ/Assets/IFixBench/VmBench/Src~"
NOWARN="-nowarn:618,414,219,168,162,649,169,1717,108,114,672"

rm -rf "$OUT"
mkdir -p "$OUT/raw"
rm -f "$PROJ/Assets/csc.rsp" "$PROJ/Assets/csc.rsp.meta"

echo "[1/6] IFix.Core.dll / IFix.exe (shared shuffled Instruction.cs)"
"$MCS" "$SRC/ShuffleInstruction.cs" -out:"$OUT/ShuffleInstruction.exe"
"$MONO" "$OUT/ShuffleInstruction.exe" "$SRC/Src/Core/Instruction.cs" "$OUT/Instruction.cs" > /dev/null
CORE_FILES=$(ls "$SRC"/Src/Core/*.cs | grep -v '/Instruction.cs$')
"$MCS" $NOWARN -define:UNITY_IPHONE -unsafe -target:library -out:"$OUT/IFix.Core.dll" \
    "$SRC"/Src/Builder/*.cs "$SRC/Src/Version.cs" "$OUT/Instruction.cs" $CORE_FILES
cp "$SRC"/ThirdParty/Mono.Cecil*.dll "$OUT/"
"$MCS" $NOWARN -define:UNITY_IPHONE -unsafe \
    -reference:"$OUT/Mono.Cecil.dll","$OUT/Mono.Cecil.Mdb.dll","$OUT/Mono.Cecil.Pdb.dll" \
    -out:"$OUT/IFix.exe" "$OUT/Instruction.cs" "$SRC"/Src/Tools/*.cs "$SRC/Src/Version.cs"

echo "[2/6] vm bench dlls + inject IFixBench.Interp.dll (no_cfg)"
REFS="-reference:$ENGINE_DIR/UnityEngine.CoreModule.dll,$FACADES_DIR/netstandard.dll"
CASES=$(ls "$BENCH_SRC"/*.cs | grep -v '/Target.cs$')
"$MCS" $NOWARN -target:library -out:"$OUT/IFixBench.Target.dll" "$BENCH_SRC/Target.cs"
"$MCS" $NOWARN -target:library $REFS -reference:"$OUT/IFixBench.Target.dll" \
    -out:"$OUT/IFixBench.Native.dll" $CASES
"$MCS" $NOWARN -target:library -define:IFIX_INTERP $REFS -reference:"$OUT/IFixBench.Target.dll" \
    -out:"$OUT/raw/IFixBench.Interp.dll" $CASES
"$MONO" "$OUT/IFix.exe" -inject "$OUT/IFix.Core.dll" "$OUT/raw/IFixBench.Interp.dll" no_cfg \
    "$OUT/IFixBench.Interp.patch.bytes" "$OUT/IFixBench.Interp.dll" "$OUT" "$ENGINE_DIR" "$FACADES_DIR" \
    > "$OUT/inject.log" 2>&1
if ! grep -q "process success" "$OUT/inject.log" || grep -q "Warning: process" "$OUT/inject.log"; then
    cat "$OUT/inject.log"
    exit 1
fi

echo "[3/6] install IFix (Core, editor tools, IFixToolKit) and vm bench into Unity project"
mkdir -p "$PROJ/Assets/Plugins" "$PROJ/Assets/Resources" "$PROJ/IFixToolKit"
for dll in IFix.Core IFixBench.Target IFixBench.Native IFixBench.Interp; do
    cp "$OUT/$dll.dll" "$PROJ/Assets/Plugins/"
done
cp "$OUT/IFixBench.Interp.patch.bytes" "$PROJ/Assets/Resources/"
rm -rf "$PROJ/Assets/IFix"
cp -R "$SRC/../UnityProj/Assets/IFix" "$PROJ/Assets/IFix"
cp "$OUT/IFix.exe" "$OUT"/Mono.Cecil*.dll "$PROJ/IFixToolKit/"

echo "[4/6] Unity build ($BENCH_PLATFORM), Assembly-CSharp injected by IFix (log: $OUT/unity_build.log)"
if [ "$BENCH_PLATFORM" = "ios" ]; then
    BUILD_METHOD=BenchBuilder.BuildIOS
    BUILD_PATH="$OUT/xcode"
else
    BUILD_METHOD=BenchBuilder.Build
    BUILD_PATH="$OUT/player/IFixBench.app"
fi
"$UNITY_APP/Contents/MacOS/Unity" -batchmode -quit -projectPath "$PROJ" -buildTarget $BUILD_TARGET \
    -executeMethod $BUILD_METHOD -benchBuildPath "$BUILD_PATH" \
    -logFile "$OUT/unity_build.log" || { grep -E "error|Error" "$OUT/unity_build.log" | tail -20; exit 1; }

echo "[5/6] generate Assembly-CSharp patch with IFIX_PATCHED (log: $OUT/unity_patch.log)"
echo "-define:IFIX_PATCHED" > "$PROJ/Assets/csc.rsp"
set +e
"$UNITY_APP/Contents/MacOS/Unity" -batchmode -quit -projectPath "$PROJ" -buildTarget $BUILD_TARGET \
    -executeMethod BenchBuilder.GenWorkflowPatch -benchTarget $BENCH_PLATFORM \
    -wfPatchOut "$OUT/Assembly-CSharp.patch.bytes" -logFile "$OUT/unity_patch.log"
PATCH_STATUS=$?
set -e
rm -f "$PROJ/Assets/csc.rsp" "$PROJ/Assets/csc.rsp.meta"
if [ $PATCH_STATUS -ne 0 ]; then
    grep -E "error|Error|IFixBench" "$OUT/unity_patch.log" | tail -20
    exit 1
fi

if [ "$BENCH_PLATFORM" = "ios" ]; then
    echo "[6/6] xcodebuild + run on iOS Simulator (log: $OUT/xcodebuild.log)"
    # StreamingAssets of an exported Xcode project live in Data/Raw
    mkdir -p "$OUT/xcode/Data/Raw"
    cp "$OUT/Assembly-CSharp.patch.bytes" "$OUT/xcode/Data/Raw/"
    xcodebuild -project "$OUT/xcode/Unity-iPhone.xcodeproj" -scheme Unity-iPhone -configuration Release \
        -sdk iphonesimulator -destination "generic/platform=iOS Simulator" -derivedDataPath "$OUT/derived" \
        CODE_SIGNING_ALLOWED=NO build > "$OUT/xcodebuild.log" 2>&1 || { grep -E "error:" "$OUT/xcodebuild.log" | head -20; exit 1; }
    APP=$(find "$OUT/derived/Build/Products" -maxdepth 2 -name "*.app" | head -1)
    SIM=${SIM_DEVICE:-$(xcrun simctl list devices available | grep -m1 -oE "iPhone.*\([0-9A-F-]{36}\)" | grep -oE "[0-9A-F-]{36}")}
    BUNDLE_ID=com.ifix.bench
    xcrun simctl boot "$SIM" 2> /dev/null || true
    xcrun simctl bootstatus "$SIM" -b > /dev/null
    xcrun simctl uninstall "$SIM" $BUNDLE_ID 2> /dev/null || true
    xcrun simctl install "$SIM" "$APP"
    xcrun simctl launch "$SIM" $BUNDLE_ID > /dev/null
    RESULT="$(xcrun simctl get_app_container "$SIM" $BUNDLE_ID data)/Documents/ifix_bench.txt"
    WAITED=0
    until grep -q "^RESULT" "$RESULT" 2> /dev/null; do
        if [ $WAITED -ge 900 ]; then
            echo "timeout waiting for $RESULT"
            exit 1
        fi
        sleep 3
        WAITED=$((WAITED + 3))
    done
    xcrun simctl terminate "$SIM" $BUNDLE_ID 2> /dev/null || true
    cp "$RESULT" "$OUT/result.txt"
    STATUS=0
    grep -q "^RESULT OK" "$OUT/result.txt" || STATUS=1
else
    echo "[6/6] run player (log: $OUT/player.log)"
    set +e
    "$OUT/player/IFixBench.app/Contents/MacOS/IFixBench" -batchmode -nographics -logFile "$OUT/player.log" \
        -benchOut "$OUT/result.txt" -wfPatch "$OUT/Assembly-CSharp.patch.bytes" > /dev/null 2>&1
    STATUS=$?
    set -e
fi
if [ ! -f "$OUT/result.txt" ]; then
    echo "no result, see $OUT/player.log"
    exit 1
fi
cat "$OUT/result.txt"
exit $STATUS
