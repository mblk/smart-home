# audio_controller.py

import subprocess
import os

class AudioController:
    def __init__(self, output_device):
        self.radio_process = None
        self.radio_sink = "RadioSink"
        self.tts_sink = "TTSSink"
        self.output_device = output_device

    def setup_audio_sinks(self):
        try:
            existing_sinks = subprocess.check_output(["pactl", "list", "sinks", "short"]).decode()
            existing_modules = subprocess.check_output(["pactl", "list", "modules", "short"]).decode()
            
            # Check if RadioSink exists
            if self.radio_sink not in existing_sinks:
                subprocess.run(["pactl", "load-module", "module-null-sink", f"sink_name={self.radio_sink}", "sink_properties=device.description=RadioSink"], check=True)
                print(f"✅ Virtual sink '{self.radio_sink}' created.")
            else:
                print(f"ℹ️ Virtual sink '{self.radio_sink}' already exists.")

            # Check if TTSSink exists
            if self.tts_sink not in existing_sinks:
                subprocess.run(["pactl", "load-module", "module-null-sink", f"sink_name={self.tts_sink}", "sink_properties=device.description=TTSSink"], check=True)
                print(f"✅ Virtual sink '{self.tts_sink}' created.")
            else:
                print(f"ℹ️ Virtual sink '{self.tts_sink}' already exists.")

            # Check if loopback for RadioSink exists
            if f"source={self.radio_sink}.monitor" not in existing_modules:
                subprocess.run(["pactl", "load-module", "module-loopback", f"sink={self.output_device}", f"source={self.radio_sink}.monitor"], check=True)
                print(f"✅ Loopback for '{self.radio_sink}' created.")
            else:
                print(f"ℹ️ Loopback for '{self.radio_sink}' already exists.")

            # Check if loopback for TTSSink exists
            if f"source={self.tts_sink}.monitor" not in existing_modules:
                subprocess.run(["pactl", "load-module", "module-loopback", f"sink={self.output_device}", f"source={self.tts_sink}.monitor"], check=True)
                print(f"✅ Loopback for '{self.tts_sink}' created.")
            else:
                print(f"ℹ️ Loopback for '{self.tts_sink}' already exists.")

        except subprocess.CalledProcessError as e:
            print(f"❌ Error setting up audio sinks: {e}")

    def set_default_volumes(self):
        try:
            self.set_master_volume(50)
            self.set_sink_volume(self.radio_sink, 100)
            self.set_sink_volume(self.tts_sink, 100)
        except Exception as e:
            print(f"[Error] Default volumes could not be set: {e}")

    def play_radio(self, source):
        self.stop_radio()

        try:
            print(f"🌐 Starting radio stream with mpg123: {source}")
            env = os.environ.copy()
            env["PULSE_SINK"] = self.radio_sink  # Route audio to RadioSink
            self.radio_process = subprocess.Popen(["mpg123", source], env=env)
        except Exception as e:
            print(f"[Error] mpg123 could not be started: {e}")

    def stop_radio(self):
        print("🛑 Stopping active radio stream...")

        if self.radio_process is None:
            print("ℹ️ No active radio stream.")
            return

        try:
            self.radio_process.terminate()
            self.radio_process.wait(timeout=5)
        except Exception as e:
            print(f"[Error] Radio process could not be terminated cleanly: {e}")
        finally:
            self.radio_process = None

    def speak(self, text):
        try:
            print(f"🗣️ Speaking: {text}")
            subprocess.run(["pico2wave", "-l", "de-DE", "-w", "/tmp/speech.wav", text])

            # Reduce radio volume during TTS
            self.set_sink_volume(self.radio_sink, 50)

            env = os.environ.copy()
            env["PULSE_SINK"] = self.tts_sink  # Route audio to TTSSink
            subprocess.run(["aplay", "/tmp/speech.wav"], env=env)

            # Restore original radio volume
            self.set_sink_volume(self.radio_sink, 100)

        except Exception as e:
            print(f"[Error] Text-to-speech failed: {e}")

    def set_sink_volume(self, sink_name, volume):
        if volume < 0 or volume > 100:
            print(f"[Error] Volume must be between 0 and 100: {volume}")
            return
        
        try:
            # Set volume for the specified sink
            subprocess.run(["pactl", "set-sink-volume", sink_name, f"{volume}%"], check=True)
            print(f"🔊 Volume for {sink_name} set to {volume}%.")
        except Exception as e:
            print(f"[Error] Volume for {sink_name} could not be set: {e}")
    
    def set_master_volume(self, volume):
        if volume < 0 or volume > 100:
            print(f"[Error] Volume must be between 0 and 100: {volume}")
            return
        
        try:
            # Set volume for the master sink
            subprocess.run(["amixer", "set", "Master", f"{volume}%"], check=True)
            print(f"🔊 Master volume set to {volume}%.")
        except Exception as e:
            print(f"[Error] Master volume could not be set: {e}")