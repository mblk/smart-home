# main.py

import os
from dotenv import load_dotenv
from audio_controller import AudioController
from mqtt_client import MqttAudioClient

# Load .env file
load_dotenv()

MQTT_BROKER = os.getenv("MQTT_BROKER", "localhost")
MQTT_PORT = int(os.getenv("MQTT_PORT", 1883))
MQTT_TOPIC = os.getenv("MQTT_TOPIC", "audio/control")
OUTPUT_DEVICE = os.getenv("OUTPUT_DEVICE", "default")

print("Settings:")
print(f"MQTT Broker: {MQTT_BROKER}")
print(f"MQTT Port: {MQTT_PORT}")
print(f"MQTT Topic: {MQTT_TOPIC}")
print(f"Output Device: {OUTPUT_DEVICE}")

if __name__ == "__main__":
    controller = AudioController(output_device=OUTPUT_DEVICE)
    controller.setup_audio_sinks()
    controller.set_default_volumes()

    mqtt_client = MqttAudioClient(
        broker=MQTT_BROKER,
        port=MQTT_PORT,
        topic=MQTT_TOPIC,
        audio_controller=controller
    )
    mqtt_client.start()
