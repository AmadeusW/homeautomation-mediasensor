# Media Sensor for Home Automation

This app monitors the status of the sound output on your PC.
It reports updates to the [Home Assistant](https://www.home-assistant.io/) server.

The purpose of this app is to automatically turn off the light when the media is playing,
and turn the light on when the media is stopped.

![screenshot of v1.2](https://user-images.githubusercontent.com/1673956/78528009-34536580-7793-11ea-97b0-4ffdaf816960.png)

## Features

* Turn the light on when sound stops playing
* Turn the light off when sound starts playing
* Override the sound sensor and manually control the light
* Don't use the sound sensor at all and manually control the light
* Turn the light off when app closes

## Prerequisites

* Home Assistant: https://www.home-assistant.io/
* Windows 10
* .NET Core 3.0 runtime: https://dotnet.microsoft.com/download/dotnet-core/3.0

## Sample code

`mediasensor.yaml` in the same directory as the .exe

```yaml
url: http://hass-server:8123/api/states/sensor.tvroommedia # URL of the API endpoint. See https://developers.home-assistant.io/docs/en/external_api_rest.html
token: redacted # Home Assistant long term token
poll: 250 # Polling delay in milliseconds. This represents delay between calls to the OS.
latch: 1000 # Latching delay in milliseconds. This represents duration of how long media state must be steady before making API call 
soundsensor: true # true to use sound sensor. false to use the app as on-off switch
```

`automations.yaml` on the Home Assistant server

```yaml
- alias: Media STOPPED Light ON at NIGHT
  description: 'When media is Stopped, and it is night time, turn the light on'
  trigger:
  - entity_id: sensor.tvroommedia
    platform: state
    to: stopped
  condition:
  - condition: sun
    after: sunrise
    before: sunseet
  action:
  - data:
      entity_id: switch.tv_room
    service: switch.turn_on
- alias: Media PLAYING Lights OFF at NIGHT
  description: 'When media is Playing, and it is night time, turn the light off'
  trigger:
  - entity_id: sensor.tvroommedia
    platform: state
    to: playing
  condition:
  - condition: sun
    after: sunrise
    before: sunset
  action:
  - data:
      entity_id: switch.tv_room
    service: switch.turn_off
- alias: Media FORCE STOPPED Light ON
  description: 'When media is force set to Stopped, turn the light on'
  trigger:
  - entity_id: sensor.tvroommedia
    platform: state
    to: force stopped
  action:
  - data:
      entity_id: switch.tv_room
    service: switch.turn_on
- alias: Media FORCE PLAYING Lights OFF
  description: 'When media is force set to Playing, turn the light off'
  trigger:
  - entity_id: sensor.tvroommedia
    platform: state
    to: force playing
  action:
  - data:
      entity_id: switch.tv_room
    service: switch.turn_off

```
