ARG arch=bookworm-slim

FROM mcr.microsoft.com/dotnet/runtime:8.0-${arch} AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:f91f9181d68b5ed2c8dea199dbbd2f6d172e60bdc43f8a67878b1ad140cf8d6b AS build
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