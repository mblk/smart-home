# audio_controller.py

import subprocess

class AudioController:
    def __init__(self):
        self.process = None

    def play(self, filepath):
        self.stop()
        print(f"Starte aplay mit Datei: {filepath}")
        self.process = subprocess.Popen(["aplay", filepath])

    def stop(self):
        if self.process is not None:
            print("Beende laufende Wiedergabe...")
            self.process.terminate()
            self.process.wait()
            self.process = None
        else:
            print("Keine Wiedergabe aktiv.")
