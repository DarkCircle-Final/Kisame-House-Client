# YOLO_Inference
AI Treacker with Raspberry Pi

## library Install
- pip install 하고 SSH로 접속한 경우 재접속 이후 파일 실행
- 추천은 설치 이후 재부팅하기

1. 가상환경 생성
    ```shell
        python -m venv yolov_env
    ```

2. 가상환경 활성화
    ```shell
        source ./yolov_env/bin/activate
    ```

3. 라이브러리 설치
    ```shell
        pip install ultralytics
        pip install paho-mqtt
        pip install picamera2
    ```

4. picamera2 설치 안될 시
    1. 가상환경 설정 풀기
        ```shell
            deactivate
        ```

    2. 시스템패키지에 picamera2 설치
        ```shell
            sudo apt update
            sudo apt install -y python3-picamera2 python3-libcamera libcamera-apps
        ```

    3. 가상환경 다시 활성화
        ```shell
            # venv 활성화 상태라고 가정
            PTH="./yolov_env/lib/python3.11/site-packages/_sys_path.pth"
            echo "/usr/lib/python3/dist-packages" > "$PTH"

            # 확인
            python -c "import libcamera, picamera2; from picamera2 import Picamera2; print('OK')"
        ```

5. lap 모듈 없을 시
```shell
    pip install lap
```

