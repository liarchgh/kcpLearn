rm -Force -Recurse build
mkdir build
cd build

cmake -D BUILD_SHARED_LIBS=ON -D BUILD_TESTING=OFF ..
cmake --build .  --config Debug

cd ..

# see `build\Release\kcp.dll`
