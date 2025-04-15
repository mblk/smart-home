# audio_controller.py

import subprocess
import os

class AudioController:
    def __init__(self):
        self.process = None

    def play(self, source):
        self.stop()

        if source.startswith("http://") or source.startswith("https://"):
            # Internet-Stream → verwende mpg123
            try:
                print(f"🌐 Starte Stream mit mpg123: {source}")
                self.process = subprocess.Popen(["mpg123", source])
            except Exception as e:
                print(f"[Fehler] mpg123 konnte nicht gestartet werden: {e}")
        else:
            # Lokale Datei → verwende aplay
            if not os.path.isfile(source):
                print(f"[Fehler] Datei nicht gefunden: {source}")
                return
            try:
                print(f"🎵 Starte aplay mit Datei: {source}")
                self.process = subprocess.Popen(["aplay", source])
            except Exception as e:
                print(f"[Fehler] aplay konnte nicht gestartet werden: {e}")

    def stop(self):
        if self.process is not None:
            print("🛑 Beende laufende Wiedergabe...")
            try:
                self.process.terminate()
                self.process.wait(timeout=5)
            except Exception as e:
                print(f"[Fehler] Prozess konnte nicht sauber beendet werden: {e}")
            finally:
                self.process = None
        else:
            print("ℹ️ Keine Wiedergabe aktiv.")

    def setVolume(self, volume):
        if volume < 0 or volume > 100:
            print(f"[Fehler] Lautstärke muss zwischen 0 und 100 liegen: {volume}")
            return
        try:
            subprocess.run(["amixer", "set", "Master", f"{volume}%"])
            print(f"🔊 Lautstärke auf {volume}% gesetzt.")
        except Exception as e:
            print(f"[Fehler] Lautstärke konnte nicht gesetzt werden: {e}")

    def speak(self, text):
        # use pico2wave
        try:
            print(f"🗣️ Spreche: {text}")
            subprocess.run(["pico2wave", "-l", "de-DE", "-w", "/tmp/speech.wav", text])
            self.play("/tmp/speech.wav")
        except Exception as e:
            print(f"[Fehler] Sprachsynthese fehlgeschlagen: {e}")