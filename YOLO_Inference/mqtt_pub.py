import paho.mqtt.client as mqtt

PORT = 1883

class MQTTPublisher:
    def __init__(self, broker, port=PORT, keepalive=60):
        self.client = mqtt.Client()
        self.client.connect(broker, port, keepalive)
        self.client.loop_start()

    def publish(self, topic, message, qos=0, retain=False):
        self.client.publish(topic, message, qos=qos, retain=retain)
    
    def pub_close(self):
        self.client.loop_stop()
        self.client.disconnect()