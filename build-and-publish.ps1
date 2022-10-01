$version = "0.7"
./build-tailwind.ps1
# docker buildx create --use # create a new build context once
docker buildx build --push --platform linux/amd64,linux/arm/v7 --tag johannesegger/musiscore:$version --tag johannesegger/musiscore:latest ./src/
