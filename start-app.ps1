wt `
    new-tab --startingDirectory . `-`- docker compose -f .\docker-compose.dev.yml up`; `
    new-tab --startingDirectory .\src\MusiScore.Server `-`- dotnet watch run --environment Development --urls=https://localhost:5001`; `
    new-tab --startingDirectory .\src\MusiScore.Client `-`- npm.cmd install `&`& npm.cmd run dev