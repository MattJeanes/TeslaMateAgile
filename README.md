# TeslaMate Octopus Agile integration
This app will automatically update your cost for charge sessions within a specified geofence (usually home) using data from Octopus Agile tariff

## Required environment variables
- ConnectionStrings__TeslaMate: Server=127.0.0.1;Port=5432;Database=teslamate;User Id=teslamate;Password=teslamate;
- TeslaMate__UpdateIntervalSeconds: 60
- TeslaMate__GeofenceId: 1

## Docker support
This project is available on Docker

[![](https://img.shields.io/docker/pulls/mattjeanes/teslamateagile.svg)](https://hub.docker.com/repository/docker/mattjeanes/teslamateagile)
