# TeslaMateAgile

## Description
This app will automatically update your cost for charge sessions in TeslaMate within a specified geofence (usually home) using data from your smart electricity tariff.

Supported energy providers / tarriffs:
- [Octopus Energy: Agile Octopus](https://octopus.energy/agile/)
- [Tibber](https://tibber.com/en)

## How to use
You can either use it in a Docker container or go to the releases and download the zip of the latest one and run it on the command line using `./TeslaMateAgile`.

If you have used the [TeslaMate Docker install guide](https://docs.teslamate.org/docs/installation/docker) you can simply add this section to the `services:` section of the `docker-compose.yml` file and change the variables as required:

```yaml
services:

  teslamateagile:
    image: mattjeanes/teslamateagile:latest
    restart: always
    environment:
      - DATABASE_USER=teslamate
      - DATABASE_PASS=secret
      - DATABASE_NAME=teslamate
      - DATABASE_HOST=database
      - TeslaMate__UpdateIntervalSeconds=300
      - TeslaMate__GeofenceId=1
      - TeslaMate__Phases=1
      - Octopus__RegionCode=A # Octopus Energy only
      - Tibber__AccessToken=abc123 # Tibber only
```

See below for how to configure the environment variables appropriately

## Required environment variables
```yaml
- TeslaMate__UpdateIntervalSeconds=300 # Check for completed charges without a set cost every x seconds
- TeslaMate__GeofenceId=1 # You can get this by editing the Geofence inside TeslaMate and getting it from the url 
- TeslaMate__Phases=1 # How many phases your electricity has, this will usually be 1
```

### Octopus Energy
```
- Octopus__RegionCode=A # See below Octopus Region Code section
```

### Tibber

```yaml
- Tibber__AccessToken=abc123 # See below Tibber Access Token section
````

## Database connection
You also need to configure the database connection to the TeslaMate PostgreSQL database, you can do this either by supplying a PostgreSQL connection string directly or by using the same ones used by TeslaMate in the `docker-compose.yml`

```yaml
- DATABASE_HOST=database
- DATABASE_NAME=teslamate
- DATABASE_USER=teslamate
- DATABASE_PASS=secret
```

**OR** (not recommended)

```yaml
- ConnectionStrings__TeslaMate=Host=database;Database=teslamate;User Id=teslamate;Password=secret;
```

## Energy provider setup

### Octopus Region Code

Electricity tariffs in Octopus Energy are separated into multiple regions depending on where you live, in order to find your code you can use one of two methods:

1. Go to https://octopus.energy/dashboard/developer/
2. Under Unit Rates, just before 'standard-unit-rates' in the URL provided there is a letter, this is your region code
3. For example in https://<span></span>api.octopus.energy/v1/products/AGILE-18-02-21/electricity-tariffs/E-1R-AGILE-18-02-21-**A**/standard-unit-rates/ your region code is **A**

Or if you're familar with curl / postman / etc

1. Call `GET https://api.octopus.energy/v1/industry/grid-supply-points?postcode=POSTCODEHERE`
2. You will get a response with `"group_id": "_A"` for example, A is your region code

### Tibber Access Token

Tibber requires users to supply their access token to provide pricing information for their tarriff. It is only used to query tarriff information and at no point does TeslaMateAgile request or access any data related to consumption or any account details. You can find the related code [here](https://github.com/MattJeanes/TeslaMateAgile/blob/master/TeslaMateAgile/Services/TibberService.cs).

You can acquire this token here: https://developer.tibber.com/settings/accesstoken

## Docker support
This project is available on Docker

[![](https://img.shields.io/docker/pulls/mattjeanes/teslamateagile.svg)](https://hub.docker.com/repository/docker/mattjeanes/teslamateagile)
