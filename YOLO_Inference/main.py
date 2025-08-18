import json, cv2, asyncio, time
from concurrent.futures import ThreadPoolExecutor

from fish_track import FishTrack
from cam_module import CamHandler
from mqtt_pub import MQTTPublisher

IP = "아이피주소"
MQTT_PORT = 1883
TOPIC = "test/topic"
MODEL = "./best.pt"
TRACKER = "./bytetrack.yaml"

async def YoloInfer(yolo, cam, pub):
    """
    데이터 추론 및 MQTT 발행 함수
    """
    loop = asyncio.get_running_loop()
    pool = ThreadPoolExecutor(max_workers=1)
    last_prev_save = 0.0

    try:
        while True:
            frame = cam.get_frame()

            detections = yolo.infer(frame)
            frame = yolo.visualize(frame, detections)

            now = time.time()
            if now - last_prev_save > 1.0:
                cv2.imwrite("./tracked_prev.jpg", frame)
                last_prev_save = now
            
            json_obj = json.dumps([yolo.convert_tensor(frame) for frame in detections], indent=4)
            print(json_obj)
            
            await loop.run_in_executor(pool, MqttPub, pub, TOPIC, json_obj)
            await asyncio.sleep(0)

    except asyncio.CancelledError as ex:
        print(f"예외 : {ex.args}")
    finally:
        pool.shutdown(wait=False)
        print("추론 종료")

def MqttPub(pub: MQTTPublisher, topic: str, msg: str):
    """
    MQTT 발행 함수
    """
    pub.publish(topic, msg, qos=0)

async def main():
    """
    메인 시작 함수
    """
    yolo = FishTrack(MODEL, conf_thres=0.6, tracker=TRACKER)
    cam  = CamHandler()
    pub = MQTTPublisher(IP, MQTT_PORT)

    cam.start()

    try:
        task = asyncio.create_task(YoloInfer(yolo, cam, pub))
        await task
    except asyncio.CancelledError as ex:
        print(f"예외 : {ex.args}")
    finally:
        try:
            cam.stop()
        except Exception as ex:
            print(f"예외 : {ex.args}")
        try:
            pub.pub_close()
        except Exception as ex:
            print(f"예외 : {ex.args}")
        print("최종 종료")

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass