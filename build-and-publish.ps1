$version = "0.1"
./build-tailwind.ps1
docker build -t johannesegger/musiscore:$version -t johannesegger/musiscore:latest ./src/
docker push johannesegger/musiscore:$version
docker push johannesegger/musiscore:latest
