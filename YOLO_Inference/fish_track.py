import cv2, torch
from ultralytics import YOLO

class FishTrack:
    def __init__(self, model_path, conf_thres=0.6, persist=True, verbose=False, device='cpu', tracker=""):
        """
        YOLO 모델 초기화 \n
        :param model_path: 학습 완료된 모델 파일 경로 (.pt)
        :param conf_thres: confidence threshold
        :param device: 'cpu' or 'cuda'
        """
        self.model = YOLO(model_path)
        self.conf_thres = conf_thres
        self.persist = persist
        self.verbose = verbose
        self.device = device
        self.tracker = tracker

    def infer(self, frame):
        """
        추론 수행 \n
        :param frame: 단일 프레임 (numpy array)
        :return: 결과 리스트 (각 객체별 dict)
        """
        results = self.model.track(frame, conf=self.conf_thres, persist=self.persist, verbose=self.verbose, device=self.device, tracker=self.tracker)
        detections = []

        for r in results:
            boxes = r.boxes
            for box in boxes:
                xywh = box.xywh[0].cpu().numpy().tolist()
                conf = float(box.conf[0].cpu().item())
                track_id = int(box.id.item()) if box.id is not None else -1
                cls = int(box.cls[0].item())
                detections.append({
                    "track_id": track_id,
                    "cls": cls,
                    "bbox": xywh,
                    "confidence": conf,
                })
        return detections

    def visualize(self, frame, detections):
        """
        추론 결과 시각화
        """
        for det in detections:
            x, y, w, h = det['bbox']
            conf = det['confidence']
            track_id = det['track_id']
            # bbox 좌표 변환 (중심좌표 → 좌상단/우하단)
            x1, y1 = int(x - w / 2), int(y - h / 2)
            x2, y2 = int(x + w / 2), int(y + h / 2)
            cv2.rectangle(frame, (x1, y1), (x2, y2), (0,255,0), 2)
            cv2.putText(frame, f"ID:{track_id} {conf:.2f}", (x1, y1 - 5),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0,255,0), 2)
        return frame

    def convert_tensor(self, obj):
        """
        JSON 전처리
        """
        if isinstance(obj, (float, int)):
            return obj
        elif isinstance(obj, list):
            return [self.convert_tensor(i) for i in obj]
        elif isinstance(obj, dict):
            return {key: self.convert_tensor(value) for key, value in obj.items()}
        elif isinstance(obj, torch.Tensor):
            return obj.item()  # Tensor에서 값을 추출
        return obj