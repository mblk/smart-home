# mqtt_client.py

import paho.mqtt.client as mqtt
import socket

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
        if rc == 0:
            print(f"✅ Successfully connected to MQTT broker ({self.broker}:{self.port})")
            self.client.subscribe(self.topic)
        else:
            print(f"[Error] MQTT connection failed with code {rc}")

    def on_message(self, client, userdata, msg):
        try:
            command = msg.payload.decode()
            print(f"📥 Command received: {command}")

            if command.startswith("play_radio "):
                radio_url = command[11:].strip()
                self.audio_controller.play_radio(radio_url)

            elif command == "stop_radio":
                self.audio_controller.stop_radio()

            elif command.startswith("speak "):
                text = command[6:].strip()
                self.audio_controller.speak(text)

            elif command.startswith("volume "):
                level = command[7:].strip()
                if level.isdigit():
                    volume = int(level)
                    if 0 <= volume <= 100:
                        self.audio_controller.set_master_volume(volume)
                    else:
                        print(f"[Error] Volume must be between 0 and 100: {volume}")

            else:
                print(f"[Warning] Unknown command: '{command}'")
        except Exception as e:
            print(f"[Error] Error processing message: {e}")

    def start(self):
        try:
            print(f"🔌 Connecting to MQTT broker: {self.broker}:{self.port} ...")
            self.client.connect(self.broker, self.port, 60)
        except socket.gaierror:
            print(f"[Error] Invalid hostname or network error: {self.broker}")
            return
        except Exception as e:
            print(f"[Error] Connection attempt failed: {e}")
            return

        try:
            self.client.loop_forever()
        except KeyboardInterrupt:
            print("⛔️ Script terminated by keyboard interrupt...")
        except Exception as e:
            print(f"[Error] Unexpected error in MQTT loop: {e}")
        finally:
            self.audio_controller.stop()
