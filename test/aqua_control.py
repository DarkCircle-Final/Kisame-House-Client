#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import paho.mqtt.client as mqtt

# MQTT 브로커 설정 (라즈베리파이에서 mosquitto 돌리고 있으면 IP를 라즈베리파이 주소로)
MQTT_HOST = "210.119.12.68"    # PC에서 직접 MQTT 브로커에 붙을 수 있다면 localhost
MQTT_PORT = 1883
TOPIC_CONTROL = "aquabox/control"

def main():
    client = mqtt.Client()
    client.connect(MQTT_HOST, MQTT_PORT, 60)

    print("=== Aquabox Control Tester ===")
    print("알파벳 명령을 입력하면 MQTT로 보냅니다. (종료하려면 + 입력)")
    while True:
        cmd = input("명령 입력 (a,b,o,p 등): ").strip()
        if not cmd:
            continue
        if cmd.lower() == "+":
            break
        client.publish(TOPIC_CONTROL, cmd)
        print(f"[MQTT] Published to {TOPIC_CONTROL}: {cmd}")

    client.disconnect()
    print("종료합니다.")

if __name__ == "__main__":
    main()
