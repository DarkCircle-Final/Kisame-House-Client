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
        sudo apt install -y python3-picamera2 python3-opencv ffmpeg
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

## MediaMTX
- 실시간 동영상 스트리밍용 RTSP 서버

### 설치방법

1. 준비
```shell
    sudo apt update
    sudo apt install -y curl ca-certificates jq
```

2. MediaMTX 폴더 생성
```shell
    cd ~
    mkdir -p ~/mediamtx
    cd ~/mediamtx
```

3. 아키텍처 확인 후 아키텍처에 맞게 URL 추출
```shell
    ARCH_DEB=$(dpkg --print-architecture)   # arm64 또는 armhf
    if [ "$ARCH_DEB" = "arm64" ]; then
    PATTERN='linux_arm64.*\.tar\.gz$'
    else
    PATTERN='linux_armv7.*\.tar\.gz$'
    fi

    URL=$(curl -s https://api.github.com/repos/bluenviron/mediamtx/releases/latest \
    | jq -r --arg re "$PATTERN" '.assets[] | select(.name|test($re)) | .browser_download_url')

    echo "=> download URL: $URL"
```

4. 파일 다운
```shell
    curl -L -o mediamtx.tar.gz "$URL"
```

5. 파일 확인
```shell
    file mediamtx.tar.gz  # gzip으로 나와야 함
```

6. 압축 풀기 및 실행 권한 설정
```shell
    tar -xzf mediamtx.tar.gz
    chmod +x mediamtx
```

7. MediaMTX에서 사용할 포트가 사용하고 있으면 비우기
```shell
    sudo fuser -k 8554/tcp 1935/tcp 8888/tcp 2>/dev/null || true
```

8. 서버 실행
```shell
    ./mediamtx
```

9. Window PowerShell에서 확인방법
    - VLC 설치
        ```shell
            winget install --id=VideoLAN.VLC -e
        ```

    - 서버 실행된 상태에서 아래 명령어 입력
        ```shell
            "C:\Program Files\VideoLAN\VLC\vlc.exe" "rtsp://<아이피주소>:8554/live" --rtsp-tcp --network-caching=100
        ```
