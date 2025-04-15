# audio_controller.py

import subprocess
import os

class AudioController:
    def __init__(self):
        self.process = None

    def play(self, filepath):
        self.stop()
        if not os.path.isfile(filepath):
            print(f"[Fehler] Datei nicht gefunden: {filepath}")
            return
        try:
            print(f"🎵 Starte aplay mit Datei: {filepath}")
            self.process = subprocess.Popen(["aplay", filepath])
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
