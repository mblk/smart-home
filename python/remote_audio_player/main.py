# main.py

import os
from dotenv import load_dotenv
from audio_controller import AudioController
from mqtt_client import MqttAudioClient

# .env laden
load_dotenv()

MQTT_BROKER = os.getenv("MQTT_BROKER", "localhost")
MQTT_PORT = int(os.getenv("MQTT_PORT", 1883))
MQTT_TOPIC = os.getenv("MQTT_TOPIC", "audio/control")

print("Settings:")
print(f"MQTT Broker: {MQTT_BROKER}")
print(f"MQTT Port: {MQTT_PORT}")
print(f"MQTT Topic: {MQTT_TOPIC}")

if __name__ == "__main__":
    controller = AudioController()
    mqtt_client = MqttAudioClient(
        broker=MQTT_BROKER,
        port=MQTT_PORT,
        topic=MQTT_TOPIC,
        audio_controller=controller
    )
    mqtt_client.start()
