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
    echo "⚠️  systemd --user scheint nicht aktiv zu sein. Starte Script innerhalb einer User-Sitzung."
    exit 1
fi

echo "📁 Erstelle Zielverzeichnis: $PROJECT_DIR"
mkdir -p "$PROJECT_DIR"

echo "📄 Kopiere Projektdateien nach $PROJECT_DIR"
cp main.py audio_controller.py mqtt_client.py "$PROJECT_DIR/"

echo "🧪 Erstelle Beispiel-.env-Datei (falls nicht vorhanden)"
if [ ! -f "$PROJECT_DIR/.env" ]; then
tee "$PROJECT_DIR/.env" > /dev/null <<EOF
MQTT_BROKER=localhost
MQTT_PORT=1883
MQTT_TOPIC=audio/control
OUTPUT_DEVICE=default
EOF
fi

echo "🐍 Erstelle virtuelle Umgebung"
python3 -m venv "$VENV_DIR"

echo "📦 Installiere Python-Abhängigkeiten"
"$VENV_DIR/bin/pip" install --upgrade pip
"$VENV_DIR/bin/pip" install paho-mqtt python-dotenv

echo "📝 Erstelle systemd-Service-Datei"
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

echo "🔄 Lade systemd neu und aktiviere Service"

systemctl --user daemon-reload
systemctl --user enable "$SERVICE_NAME"
systemctl --user restart "$SERVICE_NAME"

echo "✅ Installation abgeschlossen!"
echo "👉 Service-Status prüfen mit: systemctl --user status $SERVICE_NAME"
echo "📄 Konfiguration ändern: $PROJECT_DIR/.env"
echo "🔍 Logs anzeigen: journalctl --user -u $SERVICE_NAME -f"
