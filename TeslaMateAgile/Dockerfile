ARG arch=bookworm-slim

FROM mcr.microsoft.com/dotnet/runtime:9.0-${arch} AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:fe3c1ed472bb0964c100f06aa9b1759f5ed84e0dfe6904d60f6a82159d3c7ae4 AS build
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