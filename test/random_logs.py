# pip install paho-mqtt
import json
import random
import time
from datetime import datetime
import paho.mqtt.client as mqtt

BROKER = "210.119.12.68"    # 필요 시 변경
PORT   = 1883
TOPIC_LOGS    = "aquabox/logs"
TOPIC_SENSORS = "aquabox/sensors"
INTERVAL_SEC  = 2            # 전송 주기 (초)

# -----------------------
# LOGS: 랜덤 ON/OFF 토글
# -----------------------
LOG_KEYS = ["heater", "fan", "O2", "filtering", "PUMP1", "PUMP2", "Feed", "LED"]
log_state = {k: "OFF" for k in LOG_KEYS}

def maybe_toggle(value, p=0.35):
    if random.random() < p:
        return "OFF" if value == "ON" else "ON"
    return value

def build_logs_payload():
    for k in log_state:
        log_state[k] = maybe_toggle(log_state[k], p=0.35)
    # 소비측이 단순 key:value 를 기대하므로 그대로 보냄
    return dict(log_state)

# -----------------------
# SENSORS: 랜덤 워크 생성
# -----------------------
def clamp(x, lo, hi):
    return max(lo, min(hi, x))

# 기본값(현실적인 범위 근처)
sensor_state = {
    "gas":        80.0,   # 임의 지표 (0~300 가정)
    "humidity":   50.0,   # % (20~90)
    "temp":       25.0,   # 외부온도 °C (15~35)
    "tdsValue":   120.0,  # ppm (0~1000)
    "water_temp": 26.0,   # 수온 °C (20~32)
    "ph":         7.0,    # (6.0~8.5)
}

# 각 항목별 진동 폭 (작을수록 살살 변함)
SENSOR_JITTER = {
    "gas":        8.0,
    "humidity":   2.0,
    "temp":       0.4,
    "tdsValue":   10.0,
    "water_temp": 0.3,
    "ph":         0.05,
}

# 각 항목별 허용 범위
SENSOR_RANGE = {
    "gas":        (0.0, 300.0),
    "humidity":   (20.0, 90.0),
    "temp":       (15.0, 35.0),
    "tdsValue":   (0.0, 1000.0),
    "water_temp": (20.0, 32.0),
    "ph":         (6.0, 8.5),
}

def nudge(value, jitter, lo, hi):
    # 랜덤 워크: 현재 값에 작은 변화를 더해주고 범위 내로 클램프
    step = random.uniform(-jitter, jitter)
    return clamp(value + step, lo, hi)

def build_sensors_payload():
    for k in sensor_state:
        j = SENSOR_JITTER[k]
        lo, hi = SENSOR_RANGE[k]
        sensor_state[k] = nudge(sensor_state[k], j, lo, hi)

    # 보기 좋게 소수점 자릿수 정리
    payload = {
        "gas":        round(sensor_state["gas"], 2),
        "humidity":   round(sensor_state["humidity"], 2),
        "temp":       round(sensor_state["temp"], 2),
        "tdsValue":   round(sensor_state["tdsValue"], 2),
        "water_temp": round(sensor_state["water_temp"], 2),
        "ph":         round(sensor_state["ph"], 2),
    }
    return payload

# -----------------------
# MQTT
# -----------------------
def on_connect(client, userdata, flags, rc):
    print(f"[MQTT] Connected rc={rc}")

def main():
    client = mqtt.Client()
    client.on_connect = on_connect
    client.connect(BROKER, PORT, keepalive=60)
    client.loop_start()

    try:
        while True:
            logs_payload    = build_logs_payload()
            sensors_payload = build_sensors_payload()

            j_logs    = json.dumps(logs_payload, ensure_ascii=False)
            j_sensors = json.dumps(sensors_payload, ensure_ascii=False)

            # 퍼블리시
            client.publish(TOPIC_LOGS, j_logs, qos=0, retain=False)
            client.publish(TOPIC_SENSORS, j_sensors, qos=0, retain=False)

            print(f"[PUB][logs]    {j_logs}")
            print(f"[PUB][sensors] {j_sensors}")

            time.sleep(INTERVAL_SEC)

    except KeyboardInterrupt:
        print("\n[EXIT] stopped by user")
    finally:
        client.loop_stop()
        client.disconnect()

if __name__ == "__main__":
    main()
