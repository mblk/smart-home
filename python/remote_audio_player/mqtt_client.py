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
            print(f"✅ Erfolgreich mit MQTT-Broker verbunden ({self.broker}:{self.port})")
            self.client.subscribe(self.topic)
        else:
            print(f"[Fehler] MQTT-Verbindung fehlgeschlagen mit Code {rc}")

    def on_message(self, client, userdata, msg):
        try:
            command = msg.payload.decode()
            print(f"📥 Befehl empfangen: {command}")

            if command.startswith("play "):
                audio_file = command[5:].strip()
                self.audio_controller.play(audio_file)
            elif command == "stop":
                self.audio_controller.stop()
            else:
                print(f"[Warnung] Unbekanntes Kommando: '{command}'")
        except Exception as e:
            print(f"[Fehler] Fehler bei Verarbeitung der Nachricht: {e}")

    def start(self):
        try:
            print(f"🔌 Verbinde mit MQTT-Broker: {self.broker}:{self.port} ...")
            self.client.connect(self.broker, self.port, 60)
        except socket.gaierror:
            print(f"[Fehler] Ungültiger Hostname oder Netzwerkfehler: {self.broker}")
            return
        except Exception as e:
            print(f"[Fehler] Verbindungsversuch fehlgeschlagen: {e}")
            return

        try:
            self.client.loop_forever()
        except KeyboardInterrupt:
            print("⛔️ Beende Skript durch Tastendruck...")
        except Exception as e:
            print(f"[Fehler] Unerwarteter Fehler im MQTT-Loop: {e}")
        finally:
            self.audio_controller.stop()
