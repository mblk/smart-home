#!/bin/bash

set -e

USER_NAME=$(whoami)
USER_HOME=$(eval echo "~$USER_NAME")
SERVICE_NAME="remote_audio_player"
PROJECT_DIR="$USER_HOME/inst/$SERVICE_NAME"

echo "🔁 Updating project files in $PROJECT_DIR"
cp main.py audio_controller.py mqtt_client.py "$PROJECT_DIR/"

echo "🔄 Restarting the service to apply changes"
systemctl --user restart "$SERVICE_NAME"

echo "✅ Update completed!"
echo "ℹ️ .env and virtual environment were not modified."
