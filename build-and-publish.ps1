$version = "0.5"
./build-tailwind.ps1
docker buildx build --push --platform linux/amd64,linux/arm/v7 --tag johannesegger/musiscore:$version --tag johannesegger/musiscore:latest ./src/
