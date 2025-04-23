# audio_controller.py

import subprocess
import os

class AudioController:
    def __init__(self, output_device):
        self.radio_process = None
        self.tts_process = None
        self.radio_sink = "RadioSink"
        self.tts_sink = "TTSSink"
        self.output_device = output_device

    def setup_audio_sinks(self):
        try:
            existing_sinks = subprocess.check_output(["pactl", "list", "sinks", "short"]).decode()
            existing_modules = subprocess.check_output(["pactl", "list", "modules", "short"]).decode()
            
            # Prüfe, ob RadioSink existiert
            if self.radio_sink not in existing_sinks:
                subprocess.run(
                    ["pactl", "load-module", "module-null-sink", f"sink_name={self.radio_sink}", "sink_properties=device.description=RadioSink"],
                    check=True
                )
                print(f"✅ Virtueller Sink '{self.radio_sink}' erstellt.")
            else:
                print(f"ℹ️ Virtueller Sink '{self.radio_sink}' existiert bereits.")

            # Prüfe, ob TTSSink existiert
            if self.tts_sink not in existing_sinks:
                subprocess.run(
                    ["pactl", "load-module", "module-null-sink", f"sink_name={self.tts_sink}", "sink_properties=device.description=TTSSink"],
                    check=True
                )
                print(f"✅ Virtueller Sink '{self.tts_sink}' erstellt.")
            else:
                print(f"ℹ️ Virtueller Sink '{self.tts_sink}' existiert bereits.")

            # Prüfe, ob Loopback für RadioSink existiert
            if f"source={self.radio_sink}.monitor" not in existing_modules:
                subprocess.run(
                    ["pactl", "load-module", "module-loopback", f"sink={self.output_device}", f"source={self.radio_sink}.monitor"],
                    check=True
                )
                print(f"✅ Loopback für '{self.radio_sink}' erstellt.")
            else:
                print(f"ℹ️ Loopback für '{self.radio_sink}' existiert bereits.")

            # Prüfe, ob Loopback für TTSSink existiert
            if f"source={self.tts_sink}.monitor" not in existing_modules:
                subprocess.run(
                    ["pactl", "load-module", "module-loopback", f"sink={self.output_device}", f"source={self.tts_sink}.monitor"],
                    check=True
                )
                print(f"✅ Loopback für '{self.tts_sink}' erstellt.")
            else:
                print(f"ℹ️ Loopback für '{self.tts_sink}' existiert bereits.")

        except subprocess.CalledProcessError as e:
            print(f"❌ Fehler beim Einrichten der Audio-Sinks: {e}")

    def play_radio(self, source):
        self.stop_radio()

        try:
            print(f"🌐 Starte Radio-Stream mit mpg123: {source}")
            env = os.environ.copy()
            env["PULSE_SINK"] = self.radio_sink  # Route Audio zu RadioSink
            self.radio_process = subprocess.Popen(["mpg123", source], env=env)
        except Exception as e:
            print(f"[Fehler] mpg123 konnte nicht gestartet werden: {e}")

    def stop_radio(self):
        print("🛑 Beende laufenden Radio-Stream...")

        if self.radio_process is None:
            print("ℹ️ Kein Radio-Stream aktiv.")
            return

        try:
            self.radio_process.terminate()
            self.radio_process.wait(timeout=5)
        except Exception as e:
            print(f"[Fehler] Radio-Prozess konnte nicht sauber beendet werden: {e}")
        finally:
            self.radio_process = None

    def speak(self, text):
        self.stop_tts()

        try:
            print(f"🗣️ Spreche: {text}")
            subprocess.run(["pico2wave", "-l", "de-DE", "-w", "/tmp/speech.wav", text])

            # Reduziere Radio-Lautstärke während TTS
            self.set_sink_volume(self.radio_sink, 50)  # Temporär auf 50% reduzieren

            env = os.environ.copy()
            env["PULSE_SINK"] = self.tts_sink  # Route Audio zu TTSSink
            self.tts_process = subprocess.Popen(["aplay", "/tmp/speech.wav"], env=env)
            self.tts_process.wait()  # Warte, bis TTS abgeschlossen ist

            # Stelle ursprüngliche Radio-Lautstärke wieder her
            self.set_sink_volume(self.radio_sink, 100)

        except Exception as e:
            print(f"[Fehler] Sprachsynthese fehlgeschlagen: {e}")

    def stop_tts(self):
        print("🛑 Beende laufende TTS-Wiedergabe...")

        if self.tts_process is None:
            print("ℹ️ Keine TTS-Wiedergabe aktiv.")
            return

        try:
            self.tts_process.terminate()
            self.tts_process.wait(timeout=5)
        except Exception as e:
            print(f"[Fehler] TTS-Prozess konnte nicht sauber beendet werden: {e}")
        finally:
            self.tts_process = None

    def set_sink_volume(self, sink_name, volume):
        if volume < 0 or volume > 100:
            print(f"[Fehler] Lautstärke muss zwischen 0 und 100 liegen: {volume}")
            return
        try:
            # Setze Lautstärke für den angegebenen Sink
            subprocess.run(["pactl", "set-sink-volume", sink_name, f"{volume}%"])
            print(f"🔊 Lautstärke für {sink_name} auf {volume}% gesetzt.")
        except Exception as e:
            print(f"[Fehler] Lautstärke für {sink_name} konnte nicht gesetzt werden: {e}")