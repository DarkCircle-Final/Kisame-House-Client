import subprocess

from picamera2 import Picamera2
from picamera2.encoders import H264Encoder
from picamera2.outputs import FileOutput

class CamHandler:
    def __init__(self, width=1200, height=720, fps=30, fmt="RGB888"):
        """
        캠 연동 클래스
        """
        self.cam = Picamera2()
        frame_time = int(1_000_000 / fps)
        self.ff = self.video_pub()

        video = {"size": (width, height), "format": "YUV420"}
        lores = {"size": (640, 360), "format": fmt}

        config = self.cam.create_video_configuration(
            main=video,
            lores=lores,
            controls={"FrameDurationLimits": (frame_time, frame_time)},
            buffer_count=6
        )

        self.cam.configure(config)
    
    def start(self):
        """
        캠 시작 함수
        """
        enc = H264Encoder(bitrate=3_000_000, iperiod=15, repeat=True)
        self.cam.start_recording(enc, FileOutput(self.ff.stdin))
    
    def stop(self):
        """
        캠 종료 함수
        """
        self.cam.stop_recording()
        self.ff.stdin.close()
        self.ff.wait(timeout=3)
        self.ff.terminate()
    
    def get_frame(self):
        return self.cam.capture_array("lores")
    
    def video_pub(self):
        """
        영상 송출 함수
        """
        ff = subprocess.Popen([
            "ffmpeg",
            "-hide_banner", "-loglevel", "warning",
            # ── 입력(H.264 ES) 타임스탬프 보정 ──
            "-fflags", "+genpts",
            "-use_wallclock_as_timestamps", "1",
            "-f", "h264",               # ← 파이프로 들어오는 게 H.264 ES라고 알려줌
            "-r", "30",                 # ← 입력 프레임레이트 힌트(PTS 생성에 도움)
            "-i", "-",                  # ← stdin

            # ── 출력(RTMP) ──
            "-an",
            "-c:v", "copy",
            "-flush_packets", "1",
            "-flags", "low_delay",
            "-muxdelay", "0", "-muxpreload", "0",
            "-flvflags", "no_duration_filesize",
            "-f", "flv", "rtmp://127.0.0.1:1935/live"
        ], stdin=subprocess.PIPE)

        return ff