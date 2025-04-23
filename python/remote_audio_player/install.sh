#!/bin/bash

set -e

USER_NAME=$(whoami)
USER_HOME=$(eval echo "~$USER_NAME")

SERVICE_NAME="remote_audio_player"
SYSTEMD_USER_DIR="$USER_HOME/.config/systemd/user"
SERVICE_FILE="$SYSTEMD_USER_DIR/$SERVICE_NAME.service"

PROJECT_DIR="$USER_HOME/inst/$SERVICE_NAME"
VENV_DIR="$PROJECT_DIR/venv"
PYTHON="$VENV_DIR/bin/python"

if ! systemctl --user is-active default.target &>/dev/null; then
    echo "⚠️  systemd --user does not seem to be active. Run this script within a user session."
    exit 1
fi

echo "📁 Creating target directory: $PROJECT_DIR"
mkdir -p "$PROJECT_DIR"

echo "📄 Copying project files to $PROJECT_DIR"
cp main.py audio_controller.py mqtt_client.py "$PROJECT_DIR/"

echo "🧪 Creating example .env file (if not already present)"
if [ ! -f "$PROJECT_DIR/.env" ]; then
tee "$PROJECT_DIR/.env" > /dev/null <<EOF
MQTT_BROKER=localhost
MQTT_PORT=1883
MQTT_TOPIC=audio/control
OUTPUT_DEVICE=default
EOF
fi

echo "🐍 Creating virtual environment"
python3 -m venv "$VENV_DIR"

echo "📦 Installing Python dependencies"
"$VENV_DIR/bin/pip" install --upgrade pip
"$VENV_DIR/bin/pip" install paho-mqtt python-dotenv

echo "📝 Creating systemd service file"
mkdir -p "$SYSTEMD_USER_DIR"
tee "$SERVICE_FILE" > /dev/null <<EOF
[Unit]
Description=Remote Audio Player Service
After=network-online.target
Wants=network-online.target

[Service]
ExecStartPre=/bin/sleep 10
ExecStart=$PYTHON $PROJECT_DIR/main.py
WorkingDirectory=$PROJECT_DIR
Restart=on-failure
Environment=PYTHONUNBUFFERED=1

[Install]
WantedBy=default.target
EOF

echo "🔄 Reloading systemd and enabling service"

systemctl --user daemon-reload
systemctl --user enable "$SERVICE_NAME"
systemctl --user restart "$SERVICE_NAME"

echo "✅ Installation completed!"
echo "👉 Check service status with: systemctl --user status $SERVICE_NAME"
echo "📄 Modify configuration: $PROJECT_DIR/.env"
echo "🔍 View logs: journalctl --user -u $SERVICE_NAME -f"
