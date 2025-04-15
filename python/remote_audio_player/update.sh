#!/bin/bash

set -e

SERVICE_NAME="remote_audio_player"
USER_NAME=$(logname 2>/dev/null || whoami)
HOME_DIR=$(eval echo "~$USER_NAME")
PROJECT_DIR="$HOME_DIR/remote_audio_player"

echo "🔄 Update für $SERVICE_NAME"

if [ ! -d "$PROJECT_DIR" ]; then
    echo "❌ Projektverzeichnis nicht gefunden: $PROJECT_DIR"
    exit 1
fi

echo "📄 Kopiere neue Python-Dateien..."
cp main.py audio_controller.py mqtt_client.py "$PROJECT_DIR/"

echo "🔁 Starte systemd-Service neu..."
sudo systemctl restart "$SERVICE_NAME"

echo "✅ Update abgeschlossen!"
echo "📋 Status prüfen mit: journalctl -u $SERVICE_NAME -f"
