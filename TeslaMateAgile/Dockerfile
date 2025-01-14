ARG arch=bookworm-slim

FROM mcr.microsoft.com/dotnet/runtime:9.0-${arch} AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:84fd557bebc64015e731aca1085b92c7619e49bdbe247e57392a43d92276f617 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TeslaMateAgile/TeslaMateAgile.csproj", "TeslaMateAgile/"]
RUN dotnet restore "./TeslaMateAgile/TeslaMateAgile.csproj"
COPY . .
WORKDIR "/src/TeslaMateAgile"
RUN dotnet build "./TeslaMateAgile.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TeslaMateAgile.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TeslaMateAgile.dll"]