# Media Sensor for Home Automation

This app monitors the status of the sound output on your PC.
It reports updates to the [Home Assistant](https://www.home-assistant.io/) server.

The purpose of this app is to automatically turn off the light when the media is playing,
and turn the light on when the media is stopped.

![v1.3 screenshot](https://user-images.githubusercontent.com/1673956/85217983-da524d00-b34a-11ea-82f5-2331772eb453.png)

## Features

* Turn on the light when media pauses
  * Turn the light on when sound stops playing
  * Turn the light off when sound starts playing
* Use as a light switch
  * Override the sound sensor and manually control the light
  * Option to not use the sound sensor at all
* Mouse free operation
  * Space, Enter and Escape shortcuts to operate and/or minimize the app.
* Don't worry about turning the PC off
  * The light turns off a minute after the app closes

## Prerequisites

* Home Assistant: https://www.home-assistant.io/
* Windows 10
* .NET Core 3.1 runtime: https://dotnet.microsoft.com/download/dotnet-core/3.1

## Sample code

`mediasensor.yaml` in the same directory as the .exe

```yaml
url: http://hass-server:8123/api/states/sensor.tvroommedia # URL of the API endpoint. See https://developers.home-assistant.io/docs/en/external_api_rest.html
token: redacted # Home Assistant long term token
poll: 250 # Polling delay in milliseconds. This represents delay between calls to the OS.
latch: 1000 # Latching delay in milliseconds. This represents duration of how long media state must be steady before making API call 
soundsensor: true # true to enable the sound sensor. false to use the app as just an on-off switch
```

`automations.yaml` on the Home Assistant server

```yaml
- alias: mediasensor tv room lights on via media
  description: Lights turn on at night when media is stopped
  trigger:
  - entity_id: sensor.tvroommedia
    platform: state
    to: on media
  condition:
  - condition: state
    entity_id: sun.sun
    state: 'below_horizon'
  action:
  - service: switch.turn_on
    data:
      entity_id: switch.tv_room

- alias: mediasensor tv room lights off via media
  description: Lights turn off at night when media plays
  trigger:
  - entity_id: sensor.tvroommedia
    platform: state
    to: off media
  condition:
  - condition: state
    entity_id: sun.sun
    state: 'below_horizon'
  action:
  - service: switch.turn_off
    data:
      entity_id: switch.tv_room

- alias: mediasensor tv room lights on via switch
  description: Light turns on when switch is set
  trigger:
  - entity_id: sensor.tvroommedia
    platform: state
    to: on switch
  action:
  - service: switch.turn_on
    data:
      entity_id: switch.tv_room

- alias: mediasensor tv room lights off via switch
  description: Light turns off when switch is reset
  trigger:
  - entity_id: sensor.tvroommedia
    platform: state
    to: off switch
  action:
  - service: switch.turn_off
    data:
      entity_id: switch.tv_room

- alias: mediasensor tv room lights off after delay
  description: Light turns off two minutes after app shuts down
  trigger:
  - entity_id: sensor.tvroommedia
    platform: state
    to: shutdown
  action:
  - delay: 0:02
  - service: switch.turn_off
    data:
      entity_id: switch.tv_room
```
