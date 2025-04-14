# smart-home

Simple home-automation solution for my personal needs.

# Components

```mermaid
graph TD;

    subgraph SmartHome - Hardware
        ZigbeeStick([Zigbee Coordinator])
        Sensor1([Zigbee Sensors])
        Sensor2([Zigbee Actors])
    end

    subgraph SmartHome - Server
        subgraph Docker
            Mosquitto
            Zigbee2MQTT
            SmartHome.UI.Blazor
            SmartHome.Service
            SmartHome.InfluxDB[(InfluxDB)]
        end
    end

    subgraph CatScale - Hardware
        CatScale.Scale([Cat Scale])
    end

    subgraph CatScale - Server
        CatScale.Service
        CatScale.UI.Blazor
        CatScale.InfluxDB[(InfluxDB)]
        CatScale.Postgres[(PostgreSQL)]
    end

    ZigbeeStick <-- USB ---> Zigbee2MQTT
    Sensor1 <-.-> ZigbeeStick
    Sensor2 <-.-> ZigbeeStick

    Mosquitto <-- MQTT --> Zigbee2MQTT
    Mosquitto <-- MQTT --> SmartHome.Service
    Mosquitto <-- MQTT --> SmartHome.UI.Blazor

    SmartHome.Service --> SmartHome.InfluxDB
    SmartHome.UI.Blazor --> SmartHome.InfluxDB
    SmartHome.Service -- HTTP --> CatScale.Service

    CatScale.Scale --> CatScale.InfluxDB
    CatScale.Scale -- HTTP --> CatScale.Service
    CatScale.Service --> CatScale.Postgres
    CatScale.Service --> CatScale.InfluxDB
    CatScale.UI.Blazor --> CatScale.Service
     
```

- Mosquitto: MQTT Broker
- Zigbee2MQTT: Exposes zigbee devices via MQTT
- SmartHome.Service: Dotnet service which implements the required logic
- Smarthome.UI.Blazor: Simple frontend