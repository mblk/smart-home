#!/bin/bash

set -e

USER_NAME=$(whoami)
USER_HOME=$(eval echo "~$USER_NAME")
SERVICE_NAME="remote_audio_player"
PROJECT_DIR="$USER_HOME/inst/$SERVICE_NAME"

echo "🔁 Aktualisiere Projektdateien im $PROJECT_DIR"
cp main.py audio_controller.py mqtt_client.py "$PROJECT_DIR/"

echo "🔄 Starte den Service neu, um Änderungen zu übernehmen"
systemctl --user restart "$SERVICE_NAME"

echo "✅ Update abgeschlossen!"
echo "ℹ️ .env und virtuelle Umgebung wurden nicht verändert."
