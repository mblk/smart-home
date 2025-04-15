#!/bin/bash

set -e

SERVICE_NAME="remote_audio_player"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"

USER_NAME=$(logname 2>/dev/null || whoami)

HOME_DIR=$(eval echo "~$USER_NAME")
PROJECT_DIR="$HOME_DIR/remote_audio_player"
VENV_DIR="$PROJECT_DIR/venv"
PYTHON="$VENV_DIR/bin/python"


echo "📁 Erstelle Zielverzeichnis: $PROJECT_DIR"
mkdir -p "$PROJECT_DIR"

echo "📄 Kopiere Projektdateien nach $PROJECT_DIR"
cp main.py audio_controller.py mqtt_client.py "$PROJECT_DIR/"

echo "🧪 Erstelle Beispiel-.env-Datei (falls nicht vorhanden)"
if [ ! -f "$PROJECT_DIR/.env" ]; then
cat > "$PROJECT_DIR/.env" <<EOF
MQTT_BROKER=localhost
MQTT_PORT=1883
MQTT_TOPIC=audio/control
EOF
fi

echo "🐍 Erstelle virtuelle Umgebung"
python3 -m venv "$VENV_DIR"

echo "📦 Installiere Python-Abhängigkeiten"
"$VENV_DIR/bin/pip" install --upgrade pip
"$VENV_DIR/bin/pip" install paho-mqtt python-dotenv

echo "📝 Erstelle systemd-Service-Datei"
sudo tee "$SERVICE_FILE" > /dev/null <<EOF
[Unit]
Description=Remote Audio Player Service
After=network.target

[Service]
ExecStart=$PYTHON $PROJECT_DIR/main.py
WorkingDirectory=$PROJECT_DIR
Restart=on-failure
User=$USER_NAME
Environment=PYTHONUNBUFFERED=1

[Install]
WantedBy=multi-user.target
EOF

echo "🔄 Lade systemd neu und aktiviere Service"
sudo systemctl daemon-reexec
sudo systemctl daemon-reload
sudo systemctl enable "$SERVICE_NAME"
sudo systemctl restart "$SERVICE_NAME"

echo "✅ Installation abgeschlossen!"
echo "👉 Service-Status prüfen mit: sudo systemctl status $SERVICE_NAME"
echo "📄 Konfiguration ändern: $PROJECT_DIR/.env"
echo "🔍 Logs anzeigen: journalctl -u $SERVICE_NAME -f"
