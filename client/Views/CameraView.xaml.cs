using client.Services;
using LibVLCSharp.Shared;
using Microsoft.Maui.Storage;
using System.Threading;

namespace client.Views;

public partial class CameraView : ContentPage
{
    private readonly ViewModels.CameraViewModel _vm;
    private readonly IOrientationService _orientation;

    private LibVLC _libVlc;
    private MediaPlayer _player;
    private Media? _media;                          // ★ 널러블
    private CancellationTokenSource? _reconnectCts; // ★ 널러블
    private bool _manuallyStopped;
    private bool _viewLoaded;

    // ★ 기본 생성자 브리지: DI 없이 호출돼도 ServiceHelper 통해 주입
    public CameraView() : this(
        client.Helps.ServiceHelper.Services.GetRequiredService<ViewModels.CameraViewModel>(),
        client.Helps.ServiceHelper.Services.GetRequiredService<IOrientationService>())
    { }

    public CameraView(ViewModels.CameraViewModel vm, IOrientationService orientation)
    {
        var logPath = Path.Combine(FileSystem.AppDataDirectory, "libvlc_log.txt");

        InitializeComponent();
        BindingContext = _vm = vm;
        _orientation = orientation;

        // LibVLC 초기화
        Core.Initialize();

        var vlcOptions = new[]
        {
            "--verbose=2",
            $"--logfile={logPath}",
            "--network-caching=50",
            "--live-caching=0",
            "--clock-jitter=0",
            "--drop-late-frames",
            "--skip-frames",
        };

        _libVlc = new LibVLC(vlcOptions);
        _player = new MediaPlayer(_libVlc);

        // 상태 이벤트
        _player.Buffering += (_, e) => MainThread.BeginInvokeOnMainThread(() => _vm.IsBuffering = e.Cache < 100);
        _player.Opening += (_, __) => MainThread.BeginInvokeOnMainThread(() => _vm.IsBuffering = true);
        _player.Playing += (_, __) => MainThread.BeginInvokeOnMainThread(() => _vm.IsBuffering = false);
        _player.EndReached += (_, __) => TryScheduleReconnect();
        _player.EncounteredError += (_, __) => TryScheduleReconnect();

        // ★ VideoView 로딩 이후에만 재생
        VlcView.Loaded += OnVlcLoaded;
        VlcView.Unloaded += OnVlcUnloaded;

        // ★ 미디어플레이어 연결은 Play 전에 반드시
        VlcView.MediaPlayer = _player;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _manuallyStopped = false;
        _orientation?.LockLandscape();
        _reconnectCts = new CancellationTokenSource();

        if (_viewLoaded) PlayWithOptions();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _manuallyStopped = true;
        _reconnectCts?.Cancel();

        try { _player?.Stop(); } catch { }
        _vm.IsBuffering = false;

        if (_media is not null)
        {
            try { _media.Dispose(); } catch { }
            _media = null;
        }

        _orientation?.UnLock();
    }

    // ★ EventHandler 시그니처는 object? 여야 함
    private void OnVlcLoaded(object? sender, EventArgs e)
    {
        _viewLoaded = true;
        if (!_manuallyStopped)
            PlayWithOptions();
    }

    private void OnVlcUnloaded(object? sender, EventArgs e)
    {
        _viewLoaded = false;
        try { _player?.Stop(); } catch { }
    }

    private void PlayWithOptions()
    {
        if (_player == null || string.IsNullOrWhiteSpace(_vm.StreamUrl) || !_viewLoaded)
            return;

        try
        {
            // 이전 미디어 정리
            if (_media is not null)
            {
                try { _media.Dispose(); } catch { }
                _media = null;
            }

            _media = new Media(_libVlc, new Uri(_vm.StreamUrl));

            // 미디어별 옵션
            _media.AddOption(":network-caching=50");
            _media.AddOption(":live-caching=0");
            _media.AddOption(":clock-jitter=0");
            _media.AddOption(":drop-late-frames");
            _media.AddOption(":skip-frames");

#if ANDROID
            if (IsAndroidEmulator())
                _media.AddOption(":rtsp-tcp"); // 에뮬레이터는 TCP 강제
#endif

            _vm.IsBuffering = true;

            if (_player.IsPlaying) _player.Stop();
            _player.Play(_media);
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

        _ = ReconnectLoopAsync(_reconnectCts.Token);
    }

    private async Task ReconnectLoopAsync(CancellationToken ct)
    {
        int[] delaysMs = { 500, 1000, 2000, 3000, 5000, 5000 };

        foreach (var delay in delaysMs)
        {
            if (ct.IsCancellationRequested || _manuallyStopped) return;
            await Task.Delay(delay, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested || _manuallyStopped) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!_player.IsPlaying && _viewLoaded)
                    PlayWithOptions();
            });

            await Task.Delay(600, ct).ConfigureAwait(false);
            if (_player.IsPlaying) return;
        }

        if (!ct.IsCancellationRequested && !_manuallyStopped && _viewLoaded)
            MainThread.BeginInvokeOnMainThread(PlayWithOptions);
    }

#if ANDROID
    private static bool IsAndroidEmulator()
    {
        try
        {
            var fp = Android.OS.Build.Fingerprint?.ToLowerInvariant() ?? "";
            var model = Android.OS.Build.Model?.ToLowerInvariant() ?? "";
            return fp.Contains("generic") || fp.Contains("emulator") || model.Contains("android sdk");
        }
        catch { return false; }
    }
#endif
}
