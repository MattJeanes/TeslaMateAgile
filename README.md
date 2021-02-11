# TeslaMateAgile

## Description
This app will automatically update your cost for charge sessions in TeslaMate within a specified geofence (usually home) using data from your smart electricity tariff.

Supported energy providers / tarriffs:
- [Octopus Energy: Agile Octopus](https://octopus.energy/agile/)
- [Tibber](https://tibber.com/en)
- Fixed Price (manually specify prices for different times of the day)

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
      - TeslaMate__EnergyProvider=Octopus
      - Octopus__RegionCode=A # Octopus Energy only
      - Tibber__AccessToken=abc123 # Tibber only
```

See below for how to configure the environment variables appropriately

## Required environment variables
```yaml
- TeslaMate__UpdateIntervalSeconds=300 # Check for completed charges without a set cost every x seconds
- TeslaMate__GeofenceId=1 # You can get this by editing the Geofence inside TeslaMate and getting it from the url 
```

### Octopus Energy
```yaml
- TeslaMate__EnergyProvider=Octopus
- Octopus__RegionCode=A # See below Octopus Region Code section
```

### Tibber

```yaml
- TeslaMate__EnergyProvider=Tibber
- Tibber__AccessToken=abc123 # See below Tibber Access Token section
```

### Fixed Price

```yaml
- TeslaMate__EnergyProvider=FixedPrice
- FixedPrice__TimeZone=Europe/London # IANA (tz database) time zone code, used for below times 
- FixedPrice__Prices__0=08:00-13:00=1.5 # You can have as many as these as you need
- FixedPrice__Prices__1=13:00-20:00=5
- FixedPrice__Prices__2=20:00-03:30=4
- FixedPrice__Prices__3=03:30-06:00=3.5
- FixedPrice__Prices__4=06:00-08:00=2
```

## Optional environment variables
```yaml
- Logging__LogLevel__Default=Debug # Enables debug logging, useful for seeing exactly how a charge was calculated
- TeslaMate__FeePerKilowattHour=0.25 # Adds a flat fee per kWh, useful for certain arrangements (default: 0)
```

## Database connection
You also need to configure the database connection to the TeslaMate PostgreSQL database, you can do this either by supplying a PostgreSQL connection string directly or by using the same ones used by TeslaMate in the `docker-compose.yml`

```yaml
- DATABASE_HOST=database
- DATABASE_NAME=teslamate
- DATABASE_USER=teslamate
- DATABASE_PASS=secret
- DATABASE_PORT=5432 # Optional (default: 5432)
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

## Troubleshooting

### Recalculating charge costs

In some cases you may want to tell TeslaMateAgile to recalculate a particular charge, to do this you need to set the `cost` column in the `charging_processes` table in the TeslaMate PostgreSQL database to `NULL` for the charges you want to recalculate.

You should filter this by a particular charge `id` or by `geofence_id` if you want to recalculate everything. To find a particular charge id, look in the URL when viewing it in Grafana, it should be on the end of the URL: `...&var-charging_process_id=xxx`.

TeslaMate has a guide on manually fixing data here: https://docs.teslamate.org/docs/maintenance/manually_fixing_data

#### Common SQL queries
Recalculate charge costs for a particular charge: `UPDATE charging_processes SET cost=NULL WHERE id={ChargeId}`
Recalculate charge costs for all charges at your GeofenceId: `UPDATE charging_processes SET cost=NULL WHERE geofence_id={GeofenceId}`

Please be careful when running SQL queries against your TeslaMate database as they may result in permanent data loss. Take a backup of your database before if you're not sure.

## Docker support
This project is available on Docker

[![](https://img.shields.io/docker/pulls/mattjeanes/teslamateagile.svg)](https://hub.docker.com/repository/docker/mattjeanes/teslamateagile)
