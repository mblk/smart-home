# mqtt_client.py

import paho.mqtt.client as mqtt

class MqttAudioClient:
    def __init__(self, broker, port, topic, audio_controller):
        self.client = mqtt.Client()
        self.client.on_connect = self.on_connect
        self.client.on_message = self.on_message

        self.broker = broker
        self.port = port
        self.topic = topic
        self.audio_controller = audio_controller

    def on_connect(self, client, userdata, flags, rc):
        print("Verbunden mit MQTT Broker:", rc)
        self.client.subscribe(self.topic)

    def on_message(self, client, userdata, msg):
        command = msg.payload.decode()
        print(f"Befehl empfangen: {command}")

        if command.startswith("play "):
            audio_file = command[5:].strip()
            self.audio_controller.play(audio_file)
        elif command == "stop":
            self.audio_controller.stop()
        else:
            print("Unbekanntes Kommando:", command)

    def start(self):
        self.client.connect(self.broker, self.port, 60)
        try:
            self.client.loop_forever()
        except KeyboardInterrupt:
            print("Beende Skript...")
            self.audio_controller.stop()
