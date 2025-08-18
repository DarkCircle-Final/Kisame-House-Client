import paho.mqtt.client as mqtt

PORT = 1883

class MQTTPublisher:
    def __init__(self, broker, port=PORT, keepalive=60):
        """
        MQTT Pub 클래스
        """
        self.client = mqtt.Client()
        self.client.connect(broker, port, keepalive)
        self.client.loop_start()

    def publish(self, topic, message, qos=0, retain=False):
        """
        MQTT Pub 실행 함수
        """
        self.client.publish(topic, message, qos=qos, retain=retain)
    
    def pub_close(self):
        """
        MQTT 연결 종료 함수
        """
        self.client.loop_stop()
        self.client.disconnect()