FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim-amd64 AS build-env

WORKDIR /app

# Copy fsproj and restore as distinct layers
COPY ./MusiScore.Client/*.fsproj ./MusiScore.Client/
COPY ./MusiScore.Server/*.fsproj ./MusiScore.Server/
COPY ./MusiScore.Shared/*.fsproj ./MusiScore.Shared/
RUN dotnet restore ./MusiScore.Server

# Copy everything else and build
COPY ./MusiScore.Client ./MusiScore.Client
COPY ./MusiScore.Server ./MusiScore.Server
COPY ./MusiScore.Shared ./MusiScore.Shared
RUN dotnet publish ./MusiScore.Server -c Release -o out

###

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim
RUN apt update && apt install -y cups-client && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "MusiScore.Server.dll"]
