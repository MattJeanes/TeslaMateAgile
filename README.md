# TeslaMate Octopus Agile integration

## Description
This app will automatically update your cost for charge sessions in TeslaMate within a specified geofence (usually home) using data from the Octopus Agile tariff.

## How to use
You can either use it in a Docker container or go to the releases and download the zip of the latest one and run it on the command line using `dotnet TeslaMateAgile.dll` - note you will need [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)

You will need to set configuration using environment variables, the required ones are below.

## Required environment variables
```yaml
- ConnectionStrings__TeslaMate: 'Server=127.0.0.1;Port=5432;Database=teslamate;User Id=teslamate;Password=teslamate;'
- TeslaMate__UpdateIntervalSeconds: '300' # Check for completed charges without a set cost every x seconds
- TeslaMate__GeofenceId: '1' # You can get this by editing the Geofence inside TeslaMate and getting it from the url 
- TeslaMate__Phases: '1' # How many phases your electricity has, this will usually be 1
- Octopus__RegionCode: 'A' # See below
```

## Octopus Region Code

Electricity tariffs in Octopus Energy are separated into multiple regions depending on where you live, in order to find your code you can use one of two methods:

1. Go to https://octopus.energy/dashboard/developer/
2. Under Unit Rates, just before 'standard-unit-rates' in the URL provided there is a letter, this is your region code
3. For example in https://<span></span>api.octopus.energy/v1/products/AGILE-18-02-21/electricity-tariffs/E-1R-AGILE-18-02-21-**A**/standard-unit-rates/ your region code is **A**

Or if you're familar with curl / postman / etc

1. Call `GET https://api.octopus.energy/v1/industry/grid-supply-points?postcode=POSTCODEHERE`
2. You will get a response with `"group_id": "_A"` for example, A is your region code

## Docker support
This project is available on Docker

[![](https://img.shields.io/docker/pulls/mattjeanes/teslamateagile.svg)](https://hub.docker.com/repository/docker/mattjeanes/teslamateagile)
