# TeslaMate Octopus Agile integration

## Description
This app will automatically update your cost for charge sessions in TeslaMate within a specified geofence (usually home) using data from the Octopus Agile tariff.

## How to use
You can either use it in a Docker container or go to the releases and download the zip of the latest one and run it on the command line using `dotnet TeslaMateAgile.dll` - note you will need [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)

You will need to set configuration using environment variables, the required ones are below.

## Required environment variables
```yaml
- ConnectionStrings__TeslaMate: 'Server=127.0.0.1;Port=5432;Database=teslamate;User Id=teslamate;Password=teslamate;'
- TeslaMate__UpdateIntervalSeconds: '60' # Check for completed charges without a set cost every x seconds
- TeslaMate__GeofenceId: '1' # You can get this by editing the Geofence inside TeslaMate and getting it from the url 
- TeslaMate__Phases`: '1' # How many phases your electricity has, this will usually be 1
```

## Docker support
This project is available on Docker

[![](https://img.shields.io/docker/pulls/mattjeanes/teslamateagile.svg)](https://hub.docker.com/repository/docker/mattjeanes/teslamateagile)
