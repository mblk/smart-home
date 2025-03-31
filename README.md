# smart-home

Simple home-automation solution for my personal needs without using HomeAssistant or similar software.

# Components

- mosquitto: centrl mqtt broker which all other components connect to
- zigbee2mqtt: exposes zigbee devices via mqtt
- smarthome.service: dotnet service that implements the required logic
- smarthome.ui.blazor: simple frontend
