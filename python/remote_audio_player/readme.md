# Remote Audio Player

A remote audio player that can be controlled via MQTT. This project allows you to play radio streams and text-to-speech (TTS) audio independently, with volume control and sink management using PulseAudio.

## Features

- **MQTT Control**: Control the audio player remotely using MQTT commands.
- **Radio Streaming**: Play internet radio streams.
- **Text-to-Speech (TTS)**: Convert text to speech and play it through a dedicated audio sink.
- **Independent Volume Control**: Manage volume for radio and TTS separately.
- **PulseAudio Integration**: Use virtual sinks and loopbacks for flexible audio routing.

## Installation

### Prerequisites

- Python 3.x
- PulseAudio
- `pactl` and `amixer` tools
- MQTT broker (e.g., Mosquitto)

### Steps

1. Clone the repository:
    ```bash
    git clone <repository-url>
    cd remote_audio_player
    ```

2. Run the installation script:
    ```bash
    ./install.sh
    ```

3. Check the service status:
    ```bash
    systemctl --user status remote_audio_player
    ```

4. Modify the .env file to configure MQTT and audio settings:
    ```bash
    vim ~/inst/remote_audio_player/.env
    ```

5. Restart
    ```bash
    systemctl --user restart remote_audio_player
    ```

6. Check logs
    ```bash
    journalctl --user -u remote_audio_player -f
    ```

### Configuration

The .env file contains the following settings:

```
MQTT_BROKER=localhost       # MQTT broker address
MQTT_PORT=1883              # MQTT broker port
MQTT_TOPIC=audio/control    # MQTT topic for commands
OUTPUT_DEVICE=default       # PulseAudio output device
```

### Usage

MQTT Commands

| Command          | Description                             |
| ---------------- | --------------------------------------- |
| play_radio <url> | Play a radio stream from the given URL.
| stop_radio       | Stop the currently playing radio.
| speak <text>     | Convert text to speech and play it.
| volume <0-100>   | Set the master volume.

Examples:

1. Play radio stream
    ```bash
    echo -n "play_radio http://stream.radioparadise.com/mp3-192" | pub -broker localhost:1883 -topic audio/control
    ```

2. Stop radio stream
    ```bash
    echo -n "stop_radio" | pub -broker localhost:1883 -topic audio/control
    ```

3. Speak a text
    ```bash
    echo -n "speak Hallo Welt 1 2 3 4 5" | pub -broker localhost:1883 -topic audio/control
    ```

4. Set volume to 50%
    ```bash
    echo -n "volume 50" | pub -broker localhost:1883 -topic audio/control
    ```


## Development

### Updating the Project

To update the project files, run:

```bash
./update.sh
```

This will copy the updated files to the target directory and restart the service.

### Uninstalling

To uninstall the project, run:

```bash
./uninstall.sh
```

This will stop the service, remove the systemd configuration, and delete the project files.

### Audio management

List existing sinks:
```bash
pactl list sinks short
```

Add sinks:
```bash
pactl load-module module-null-sink sink_name=RadioSink sink_properties=device.description="RadioSink"
pactl load-module module-null-sink sink_name=TTSSink sink_properties=device.description="TTSSink"

pactl load-module module-loopback sink=bluez_output.04_21_44_20_B0_D5.1 source=RadioSink.monitor
pactl load-module module-loopback sink=bluez_output.04_21_44_20_B0_D5.1 source=TTSSink.monitor
```

Change master volume:
```bash
amixer get Master
amixer set Master 50%
```

Change sink-volume:
```bash
pactl get-sink-volume RadioSink
pactl set-sink-volume RadioSink 50%
```

Specify sink when playing:
```bash
PULSE_SINK=RadioSink mpg123 http://stream.radioparadise.com/mp3-192
PULSE_SINK=TTSSink aplay /tmp/speech.wav
```
