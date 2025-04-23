# Readme

List existing sinks:
```
pactl list sinks short
```

Add sinks to /etc/pulse/default.pa
```
##
## smarthome / remote-autio-player
##

load-module module-null-sink sink_name=RadioSink sink_properties=device.description="RadioSink"
load-module module-null-sink sink_name=TTSSink sink_properties=device.description="TTSSink"

load-module module-loopback sink=bluez_output.04_21_44_20_B0_D5.1 source=RadioSink.monitor
load-module module-loopback sink=bluez_output.04_21_44_20_B0_D5.1 source=TTSSink.monitor
```

Change master volume:
```
amixer get Master
amixer set Master 50%
```

Change sink-volume:
```
pactl get-sink-volume RadioSink
pactl set-sink-volume RadioSink 50%
```

Specify sink when playing:
```
PULSE_SINK=RadioSink mpg123 http://stream.radioparadise.com/mp3-192
PULSE_SINK=TTSSink aplay /tmp/speech.wav
```