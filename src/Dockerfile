#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-amd64 AS build-server
WORKDIR /src
COPY ["src/MusiScore.Server/MusiScore.Server.fsproj", "MusiScore.Server/"]
RUN dotnet restore MusiScore.Server
COPY src/ .
WORKDIR /src/MusiScore.Server
RUN dotnet build -c Release -o /app/build

FROM build-server AS publish-server
RUN dotnet publish -c Release -o /app/publish

FROM node:20 AS build-client
WORKDIR /src
COPY src/MusiScore.Client/package.json src/MusiScore.Client/package-lock.json ./
RUN npm ci
COPY src/MusiScore.Client/ ./
RUN npm run build

FROM base AS final
RUN apt update && apt install -y cups-client && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish-server /app/publish .
COPY --from=build-client /src/dist ./wwwroot
ENTRYPOINT ["dotnet", "MusiScore.Server.dll"]
