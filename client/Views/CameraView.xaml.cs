using client.Services;
using LibVLCSharp.Shared;
using System.Threading;

namespace client.Views;

public partial class CameraView : ContentPage
{
    private readonly ViewModels.CameraViewModel _vm;
    private LibVLC _libVlc;
    private MediaPlayer _player;

    private CancellationTokenSource _reconnectCts;
    private bool _manuallyStopped;

    private readonly IOrientationService _orientation;

    public CameraView(ViewModels.CameraViewModel vm, IOrientationService orientation)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
		_orientation = orientation;

        // LibVLC 네이티브 초기화
        Core.Initialize();

        // 초저지연 옵션 (환경 따라 미세조정 가능)
        // * rtsp-tcp를 넣지 않습니다(=UDP 기본 사용)
        var vlcOptions = new[]
        {
            "--no-video-title-show",
            "--clock-jitter=0",
            "--live-caching=0",
            "--network-caching=100",     // 50~150 사이로 조정
            "--drop-late-frames",
            "--skip-frames",
            "--sout-mux-caching=0"
        };

        _libVlc = new LibVLC(vlcOptions);
        _player = new MediaPlayer(_libVlc);

        // 이벤트: 버퍼링/에러/끝남 -> 상태 & 재연결
        _player.Buffering += (_, e) => MainThread.BeginInvokeOnMainThread(() => _vm.IsBuffering = e.Cache < 100);
        _player.Opening += (_, __) => MainThread.BeginInvokeOnMainThread(() => _vm.IsBuffering = true);
        _player.Playing += (_, __) => MainThread.BeginInvokeOnMainThread(() => _vm.IsBuffering = false);
        _player.EndReached += (_, __) => TryScheduleReconnect();   // 서버가 끊김/종료
        _player.EncounteredError += (_, __) => TryScheduleReconnect(); // 네트워크/코덱 에러
    }

    protected override void OnAppearing()
	{
        base.OnAppearing();
        _manuallyStopped = false;
        _orientation?.LockLandscape(); // 가로 모드로 고정
        _reconnectCts = new CancellationTokenSource();
        PlayWithOptions();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _manuallyStopped = true;
        _reconnectCts?.Cancel();

        try { _player?.Stop(); } catch { /* ignore */ }
        _vm.IsBuffering = false;

        _orientation?.UnLock(); // 화면 회전 잠금 해제
    }

    private void PlayWithOptions()
    {
        if (_player == null || string.IsNullOrWhiteSpace(_vm.StreamUrl))
            return;

        try
        {
            // 미디어별 옵션(추가 캐싱 최소화)
            using var media = new Media(_libVlc, new Uri(_vm.StreamUrl));

            // UDP 기본(= rtsp-tcp 강제 X)
            media.AddOption(":network-caching=50");   // 네트워크 상황에 따라 50~150
            media.AddOption(":live-caching=0");
            media.AddOption(":clock-jitter=0");
            media.AddOption(":drop-late-frames");
            media.AddOption(":skip-frames");

            // 대역폭이 낮거나 지터가 심할 때만(필요시) 완충:
            // media.AddOption(":network-caching=120");

            _vm.IsBuffering = true;
            _player.Play(media);
        }
        catch
        {
            TryScheduleReconnect();
        }
    }

    private void TryScheduleReconnect()
    {
        if (_manuallyStopped || _reconnectCts == null || _reconnectCts.IsCancellationRequested)
            return;

        // 백오프 재연결
        _ = ReconnectLoopAsync(_reconnectCts.Token);
    }

    private async Task ReconnectLoopAsync(CancellationToken ct)
    {
        // 중복 실행 방지
        // 간단히: 잠깐 대기 후 상태 보고 재시도
        int[] delaysMs = { 500, 1000, 2000, 3000, 5000, 5000 };

        foreach (var delay in delaysMs)
        {
            if (ct.IsCancellationRequested || _manuallyStopped) return;
            await Task.Delay(delay, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested || _manuallyStopped) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!_player.IsPlaying)
                    PlayWithOptions();
            });

            // 재생되면 종료
            await Task.Delay(600, ct).ConfigureAwait(false);
            if (_player.IsPlaying) return;
        }
        // 장시간 실패 시 마지막으로 한 번 더 시도 후 종료(원하면 무한 루프로 변경)
        if (!ct.IsCancellationRequested && !_manuallyStopped)
            MainThread.BeginInvokeOnMainThread(PlayWithOptions);
    }
}