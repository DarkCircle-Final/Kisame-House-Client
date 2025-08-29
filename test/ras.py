#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Aquabox MQTT <-> Serial Bridge (JSON sensors + JSON logs)
- Sensors:  aquabox/sensors  (JSON, retain)
- Logs:     aquabox/logs     (JSON, retain)
- Control:  aquabox/control  (string commands -> forwarded to Arduino)

Requires:
  pip install paho-mqtt pyserial
"""

import json
import time
import threading
from typing import Dict, Optional

import serial
from serial.serialutil import SerialException
import paho.mqtt.client as mqtt

# =======================
# Config
# =======================
SERIAL_PORT = "/dev/ttyACM0"   # ?섍꼍??留욊쾶 /dev/ttyUSB0, /dev/ttyAMA0 ??SERIAL_BAUD = 115200           # ?꾨몢?대끂? ?숈씪?섍쾶 留욎텛?몄슂
SERIAL_TIMEOUT = 0.2

MQTT_HOST = "210.119.12.68"        # 釉뚮줈而ㅺ? ?쇱쫰踰좊━?뚯씠???덉쑝硫?IP/?몄뒪???낅젰
MQTT_PORT = 1883
MQTT_USERNAME = None
MQTT_PASSWORD = None
CLIENT_ID   = "aquabox-pi-bridge"

TOPIC_SENSORS = "aquabox/sensors"   # JSON
TOPIC_LOGS    = "aquabox/logs"      # JSON
TOPIC_CONTROL = "aquabox/control"   # inbound commands

QOS = 1
PUBLISH_RETAIN = True

# ?쇱꽌 ??留ㅽ븨
SENSOR_KEYS = {
    "GAS": "gas",
    "HUM": "humidity",
    "TEMP": "temp",
    "TDS": "tdsValue",
    "WATER_TEMP": "water_temp",
    "PH": "ph",
}

# 濡쒓렇 ?곹깭 珥덇린媛?LOG_KEYS = ["heater", "fan", "O2", "filtering", "PUMP1", "PUMP2", "Feed", "LED"]

# 諛쒗뻾 鍮덈룄 ?쒖뼱
SENSOR_PUBLISH_MIN_INTERVAL = 1.0  # 珥?LOG_PUBLISH_MIN_INTERVAL = 0.2     # 珥?
# =======================
# Globals
# =======================
ser: Optional[serial.Serial] = None
mqttc: Optional[mqtt.Client] = None
lock = threading.Lock()

sensor_cache: Dict[str, Optional[float]] = {
    "gas": None,
    "humidity": None,
    "temp": None,
    "tdsValue": None,
    "water_temp": None,
    "ph": None,
}
log_cache: Dict[str, str] = {k: "UNKNOWN" for k in LOG_KEYS}

_last_sensor_publish_ts = 0.0
_last_log_publish_ts = 0.0


# =======================
# Serial / MQTT helpers
# =======================
def open_serial():
    global ser
    while True:
        try:
            s = serial.Serial(SERIAL_PORT, SERIAL_BAUD, timeout=SERIAL_TIMEOUT)
            s.flushInput()
            print(f"[SERIAL] Opened {SERIAL_PORT} @ {SERIAL_BAUD}")
            ser = s
            return
        except SerialException as e:
            print(f"[SERIAL] Open failed: {e}. Retry in 2s...")
            time.sleep(2)


def mqtt_connect():
    global mqttc
    # 理쒖떊 paho ?ㅽ??? clean_start ?ъ슜 (寃쎄퀬 ?쒓굅)
    c = mqtt.Client(client_id=CLIENT_ID, protocol=mqtt.MQTTv311)
    if MQTT_USERNAME and MQTT_PASSWORD:
        c.username_pw_set(MQTT_USERNAME, MQTT_PASSWORD)

    def on_connect(client, userdata, flags, rc):
        print(f"[MQTT] Connected rc={rc}")
        client.subscribe(TOPIC_CONTROL, qos=QOS)

    def on_message(client, userdata, msg):
        payload = msg.payload.decode(errors="ignore").strip()
        print(f"[MQTT] CONTROL <- {payload}")
        forward_command_to_serial(payload)

    def on_disconnect(client, userdata, rc):
        print(f"[MQTT] Disconnected rc={rc}")

    c.on_connect = on_connect
    c.on_message = on_message
    c.on_disconnect = on_disconnect

    while True:
        try:
            c.connect(MQTT_HOST, MQTT_PORT, keepalive=30)
            mqttc = c
            c.loop_start()
            return
        except Exception as e:
            print(f"[MQTT] Connect failed: {e}. Retry in 2s...")
            time.sleep(2)


def publish(topic: str, payload, retain=PUBLISH_RETAIN, qos=QOS):
    if mqttc is None:
        return
    try:
        mqttc.publish(topic, payload, qos=qos, retain=retain)
    except Exception as e:
        print(f"[MQTT] Publish error: {e}")


def forward_command_to_serial(cmd: str):
    """?쒖뼱 ?좏뵿 ?섏떊 ???꾨몢?대끂濡?洹몃?濡??꾨떖 (媛쒗뻾 異붽? 沅뚯옣)"""
    global ser
    if ser is None:
        print("[SERIAL] Not open; command dropped.")
        return
    data = cmd.strip()
    if not data:
        return
    try:
        ser.write((data + "\n").encode())
        ser.flush()
        print(f"[SERIAL] -> {data!r}")
    except Exception as e:
        print(f"[SERIAL] Write error: {e}")


# =======================
# Parsing
# =======================
def parse_sensor_line(line: str):
    """
    'GAS:85' / 'WATER_TEMP:-127.00' / 'PH:7.00'
    諛섑솚: {'gas': 85.0} 泥섎읆 ?쇰? key留??ы븿??dict ?먮뒗 None
    """
    try:
        if ":" not in line:
            return None
        key, raw = [x.strip() for x in line.split(":", 1)]
        if key not in SENSOR_KEYS:
            return None
        v = float(raw)
        return {SENSOR_KEYS[key]: v}
    except Exception:
        return None


def parse_log_line(line: str):
    """
    'heater_OFF', 'fan_ON', 'LED_OFF' ??    諛섑솚: {'heater': 'OFF'} 泥섎읆 ?쇰? key留??ы븿??dict ?먮뒗 None
    """
    try:
        if "_" not in line:
            return None
        key, state = line.split("_", 1)
        if key not in LOG_KEYS:
            # ??뚮Ц???쇱슜 ?먮뒗 誘몄?????蹂댁젙 ?쒕룄
            key_norm = key.strip()
            if key_norm not in LOG_KEYS:
                return None
            key = key_norm
        state = state.strip().upper()
        if state not in ("ON", "OFF", "UNKNOWN"):
            # ?덉쇅 媛믩룄 ?덉슜?섎젮硫??쒓굅
            pass
        return {key: state}
    except Exception:
        return None


# =======================
# Publish aggregators
# =======================
def maybe_publish_sensors():
    global _last_sensor_publish_ts
    now = time.time()
    if now - _last_sensor_publish_ts < SENSOR_PUBLISH_MIN_INTERVAL:
        return
    if any(v is not None for v in sensor_cache.values()):
        payload = json.dumps(sensor_cache, ensure_ascii=False)
        publish(TOPIC_SENSORS, payload)
        _last_sensor_publish_ts = now
        # print(f"[MQTT] SENSORS -> {payload}")


def maybe_publish_logs(force=False):
    global _last_log_publish_ts
    now = time.time()
    if not force and (now - _last_log_publish_ts < LOG_PUBLISH_MIN_INTERVAL):
        return
    payload = json.dumps(log_cache, ensure_ascii=False)
    publish(TOPIC_LOGS, payload)
    _last_log_publish_ts = now
    # print(f"[MQTT] LOGS -> {payload}")


# =======================
# Serial reader thread
# =======================
def serial_reader():
    global ser
    buf = ""
    while True:
        if ser is None:
            open_serial()
            buf = ""

        try:
            chunk = ser.read(256)
            if chunk:
                buf += chunk.decode(errors="ignore")
                while "\n" in buf:
                    line, buf = buf.split("\n", 1)
                    line = line.strip()
                    if not line:
                        continue

                    # 1) ?쇱꽌 ?쇱씤
                    parsed_sensor = parse_sensor_line(line)
                    if parsed_sensor:
                        with lock:
                            sensor_cache.update(parsed_sensor)
                        maybe_publish_sensors()
                        continue

                    # 2) 濡쒓렇 ?쇱씤
                    parsed_log = parse_log_line(line)
                    if parsed_log:
                        with lock:
                            log_cache.update(parsed_log)
                        maybe_publish_logs()
                        continue

                    # 3) 湲고? ?쇱씤 (?꾩슂 ???붾쾭洹??좏뵿 ?깆쑝濡?蹂대궪 ???덉쓬)
                    # print(f"[SERIAL] Unmatched: {line}")

            else:
                time.sleep(0.02)

        except SerialException as e:
            print(f"[SERIAL] Error: {e}. Reopen...")
            try:
                ser.close()
            except Exception:
                pass
            ser = None
            time.sleep(1)
        except Exception as e:
            print(f"[SERIAL] Unexpected: {e}")
            time.sleep(0.1)


# =======================
# Main
# =======================
def main():
    mqtt_connect()

    t = threading.Thread(target=serial_reader, daemon=True)
    t.start()

    print("[BRIDGE] Running. Ctrl+C to exit.")
    try:
        while True:
            # 二쇨린??keep-alive 諛쒗뻾 (?먰븯硫?二쇨린 議곗젅/鍮꾪솢?깊솕 媛??
            maybe_publish_sensors()
            maybe_publish_logs()
            time.sleep(0.5)
    except KeyboardInterrupt:
        print("\n[BRIDGE] Stopping...")
    finally:
        if mqttc:
            mqttc.loop_stop()
            mqttc.disconnect()
        if ser:
            ser.close()


if __name__ == "__main__":
    main()
