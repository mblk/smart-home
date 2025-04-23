# Readme

List existing sinks:
```
pactl list sinks short
```

Add sinks
```
##
## smarthome / remote-autio-player
##

pactl load-module module-null-sink sink_name=RadioSink sink_properties=device.description="RadioSink"
pactl load-module module-null-sink sink_name=TTSSink sink_properties=device.description="TTSSink"

pactl load-module module-loopback sink=bluez_output.04_21_44_20_B0_D5.1 source=RadioSink.monitor
pactl load-module module-loopback sink=bluez_output.04_21_44_20_B0_D5.1 source=TTSSink.monitor
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