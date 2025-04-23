#!/bin/bash

set -e

USER_NAME=$(whoami)
USER_HOME=$(eval echo "~$USER_NAME")
SERVICE_NAME="remote_audio_player"
SYSTEMD_USER_DIR="$USER_HOME/.config/systemd/user"
SERVICE_FILE="$SYSTEMD_USER_DIR/$SERVICE_NAME.service"
PROJECT_DIR="$USER_HOME/inst/$SERVICE_NAME"

echo "🛑 Stopping and disabling user service: $SERVICE_NAME"
systemctl --user stop "$SERVICE_NAME" || true
systemctl --user disable "$SERVICE_NAME" || true

echo "🧹 Removing systemd service file: $SERVICE_FILE"
rm -f "$SERVICE_FILE"

echo "🔄 Reloading systemd"
systemctl --user daemon-reload

echo "🗑️ Removing project directory: $PROJECT_DIR"
rm -rf "$PROJECT_DIR"

echo "✅ Uninstallation completed!"
