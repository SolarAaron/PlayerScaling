#!/bin/bash
set -euo pipefail

# Stage only the DLL from the current build. The staging directory is ignored
# by git, so remove stale DLLs explicitly before creating the package.
mkdir -p skel/plugins
rm -f skel/plugins/*.dll
cp ../PlayerScaling/bin/Release/netstandard2.1/PlayerScaling.dll skel/plugins/PlayerScaling.dll
cp ../CHANGELOG.md ../LICENSE.txt ../README.md skel

#remove previous package
if [ -f PlayerScalingUpdated.zip ] ; then
  rm -vf PlayerScalingUpdated.zip
fi

#package skeleton
pushd skel
zip -r9 ../PlayerScalingUpdated.zip *
popd
