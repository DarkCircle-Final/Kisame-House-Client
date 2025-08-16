from picamera2 import Picamera2

class CamHandler:
    def __init__(self, width=1200, height=720, fps=30, fmt="RGB888"):
        self.cam = Picamera2()
        frame_time = int(1_000_000 / fps)

        config = self.cam.create_video_configuration(
            main={"size": (width, height), "format": fmt},
            controls={"FrameDurationLimits": (frame_time, frame_time)}
        )

        self.cam.configure(config)
    
    def start(self):
        self.cam.start()
    
    def stop(self):
        self.cam.stop()
    
    def get_frame(self):
        return self.cam.capture_array()