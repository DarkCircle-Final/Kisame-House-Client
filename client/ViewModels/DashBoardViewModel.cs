using client.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// ★ MQTT
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

namespace client.ViewModels
{
    public partial class DashBoardViewModel : ObservableObject
    {
        public ObservableCollection<Metric> Metrics { get; } = new();
        public ObservableCollection<MetricRow> MetricRows { get; } = new();

        [ObservableProperty]
        private string _statusText = "상태 바";

        [ObservableProperty]
        private int _fishCount;

        // MQTT 설정값(스샷 기준). 필요 시 AppSettings 등으로 빼서 주입해도 됨.
        private const string MQTT_HOST = "210.119.12.68";
        private const int MQTT_PORT = 1883;
        private const string MQTT_TOPIC = "aquabox/sensors";

        private IMqttClient? _mqttClient;
        private CancellationTokenSource? _mqttCts;

        // 슬라이더바
        private double _intensity = 85;
        public double Intensity
        {
            get => _intensity;
            set
            {
                double clamped = Math.Max(85.0, Math.Min(255.0, value));
                double nearest = Math.Round(clamped / 85.0) * 85.0 + 85.0;
                if (SetProperty(ref _intensity, nearest))
                {
                    OnPropertyChanged(nameof(IntensityLabel));
                }
            }
        }

        public string IntensityLabel =>
            _intensity switch
            {
                <= 127.5 => "현재 단계: 약 (85)",
                <= 212.5 => "현재 단계: 중 (170)",
                _ => "현재 단계: 강 (255)",
            };

        // 기능버튼 1~10
        private readonly bool[] _funcOn = new bool[10];

        // 버튼 색
        private readonly Color _funcBaseColor = Color.FromArgb("#512BD4");
        private readonly double _lightenAmount = 0.7;

        // on/off 텍스트
        public string Func01Text => _funcOn[0] ? "ON" : "OFF";
        public string Func02Text => _funcOn[1] ? "ON" : "OFF";
        public string Func03Text => _funcOn[2] ? "ON" : "OFF";
        public string Func04Text => _funcOn[3] ? "ON" : "OFF";
        public string Func05Text => _funcOn[4] ? "ON" : "OFF";
        public string Func06Text => _funcOn[5] ? "ON" : "OFF";
        public string Func07Text => _funcOn[6] ? "ON" : "OFF";
        public string Func08Text => _funcOn[7] ? "ON" : "OFF";
        public string Func09Text => "카메라";
        public string Func10Text => "로그";

        // 색 반전
        public Color Func01Color => _funcOn[0] ? Lighten(_funcBaseColor, _lightenAmount) : _funcBaseColor;
        public Color Func02Color => _funcOn[1] ? Lighten(_funcBaseColor, _lightenAmount) : _funcBaseColor;
        public Color Func03Color => _funcOn[2] ? Lighten(_funcBaseColor, _lightenAmount) : _funcBaseColor;
        public Color Func04Color => _funcOn[3] ? Lighten(_funcBaseColor, _lightenAmount) : _funcBaseColor;
        public Color Func05Color => _funcOn[4] ? Lighten(_funcBaseColor, _lightenAmount) : _funcBaseColor;
        public Color Func06Color => _funcOn[5] ? Lighten(_funcBaseColor, _lightenAmount) : _funcBaseColor;
        public Color Func07Color => _funcOn[6] ? Lighten(_funcBaseColor, _lightenAmount) : _funcBaseColor;
        public Color Func08Color => _funcOn[7] ? Lighten(_funcBaseColor, _lightenAmount) : _funcBaseColor;
        public Color Func09Color => _funcBaseColor;
        public Color Func10Color => _funcBaseColor;

        public Color Func01TextColor => GetTextColor(Func01Color);
        public Color Func02TextColor => GetTextColor(Func02Color);
        public Color Func03TextColor => GetTextColor(Func03Color);
        public Color Func04TextColor => GetTextColor(Func04Color);
        public Color Func05TextColor => GetTextColor(Func05Color);
        public Color Func06TextColor => GetTextColor(Func06Color);
        public Color Func07TextColor => GetTextColor(Func07Color);
        public Color Func08TextColor => GetTextColor(Func08Color);

        // 자동/수동 스위치
        [ObservableProperty]
        private bool isAutoMode = true;

        public string AutoModelLabel => IsAutoMode ? "자동" : "수동";
        public string AutoModeLabel => AutoModelLabel;

        partial void OnIsAutoModeChanged(bool value)
        {
            StatusText = value ? "자동 모드" : "수동 모드";
            OnPropertyChanged(nameof(AutoModelLabel));
            OnPropertyChanged(nameof(AutoModelLabel));
        }

        private readonly HashSet<int> _manualTriggerButtons = new() { 1, 2, 3, 4, 5, 6 };
        private CancellationTokenSource? _autoRevertCts;

        // ★ 생성자: 기본 Metric 세팅 + MQTT 시작
        public DashBoardViewModel()
        {
            // [0,0], [0,1], [1,0], [1,1] 순서
            Metrics.Add(new Metric { Name = "수온", Value = "대기" }); // water_temp
            Metrics.Add(new Metric { Name = "외부 온도", Value = "대기" }); // temp
            Metrics.Add(new Metric { Name = "수질(TDS)", Value = "-" }); // 현재 payload에 없음
            Metrics.Add(new Metric { Name = "외부 습도", Value = "대기" }); // humidity
            Metrics.Add(new Metric { Name = "수질(PH)", Value = "대기" }); // ph
            Metrics.Add(new Metric { Name = "가스 탐지수치", Value = "대기" }); // gas

            RebuildRows();

            // MQTT 연결 시작 (fire-and-forget)
            _ = StartMqttAsync();
        }

        [RelayCommand] private void OpenDetail(Metric? m) => StatusText = $"[{m?.Name}] 상세요청";
        [RelayCommand] private void Func01() => ToggleFunc(1);
        [RelayCommand] private void Func02() => ToggleFunc(2);
        [RelayCommand] private void Func03() => ToggleFunc(3);
        [RelayCommand] private void Func04() => ToggleFunc(4);
        [RelayCommand] private void Func05() => ToggleFunc(5);
        [RelayCommand] private void Func06() => ToggleFunc(6);
        [RelayCommand] private void Func07() => ToggleFunc(7);
        [RelayCommand] private void Func08() => ToggleFunc(8);
        [RelayCommand] private async Task Func09() => await Shell.Current.GoToAsync("camera");
        [RelayCommand] private async Task Func10() => await Shell.Current.GoToAsync("logs");
        [RelayCommand] private async Task OpenSettings() => await Shell.Current.GoToAsync("settings");

        // ===== UI 유틸 =====
        private static Color GetTextColor(Color bg)
        {
            double luminance = (0.2126 * bg.Red + 0.7152 * bg.Green + 0.0722 * bg.Blue);
            return luminance > 0.6 ? Colors.Black : Colors.White;
        }
        private static Color Lighten(Color baseColor, double amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            return Color.FromRgba(
                baseColor.Red + (1 - baseColor.Red) * amount,
                baseColor.Green + (1 - baseColor.Green) * amount,
                baseColor.Blue + (1 - baseColor.Blue) * amount,
                baseColor.Alpha
            );
        }

        private void ToggleFunc(int index1)
        {
            int i = index1 - 1;
            if (i < 0 || i >= 10) return;

            _funcOn[i] = !_funcOn[i];
            StatusText = $"기능 {index1} {(_funcOn[i] ? "ON" : "OFF")}";

            OnPropertyChanged(GetFuncTextPropertyName(index1));
            OnPropertyChanged(GetFuncColorPropertyName(index1));
            OnPropertyChanged(GetFuncTextColorPropertyName(index1));

            if (_funcOn[i] && _manualTriggerButtons.Contains(index1))
            {
                IsAutoMode = false;
                ScheduleAutoRevert(5000);
            }
        }

        private void ScheduleAutoRevert(int delayMs = 5000)
        {
            _autoRevertCts?.Cancel();
            _autoRevertCts = new CancellationTokenSource();
            var token = _autoRevertCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs, token);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsAutoMode = true;
                        StatusText = "자동 모드로 전환됨";
                    });
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        private void RebuildRows()
        {
            MetricRows.Clear();
            for (int i = 0; i < Metrics.Count; i += 2)
            {
                var left = Metrics[i];
                var row = new MetricRow
                {
                    LeftName = left.Name,
                    LeftValue = left.Value
                };

                if (i + 1 < Metrics.Count)
                {
                    var right = Metrics[i + 1];
                    row.RightName = right.Name;
                    row.RightValue = right.Value;
                }

                MetricRows.Add(row);
            }
            OnPropertyChanged(nameof(MetricRows));
        }

        private string GetFuncTextPropertyName(int idx) => idx switch
        {
            1 => nameof(Func01Text),
            2 => nameof(Func02Text),
            3 => nameof(Func03Text),
            4 => nameof(Func04Text),
            5 => nameof(Func05Text),
            6 => nameof(Func06Text),
            7 => nameof(Func07Text),
            8 => nameof(Func08Text),
            9 => nameof(Func09Text),
            10 => nameof(Func10Text),
            _ => string.Empty
        };
        private string GetFuncColorPropertyName(int idx) => idx switch
        {
            1 => nameof(Func01Color),
            2 => nameof(Func02Color),
            3 => nameof(Func03Color),
            4 => nameof(Func04Color),
            5 => nameof(Func05Color),
            6 => nameof(Func06Color),
            7 => nameof(Func07Color),
            8 => nameof(Func08Color),
            9 => nameof(Func09Color),
            10 => nameof(Func10Color),
            _ => string.Empty
        };
        private string GetFuncTextColorPropertyName(int idx1) => idx1 switch
        {
            1 => nameof(Func01TextColor),
            2 => nameof(Func02TextColor),
            3 => nameof(Func03TextColor),
            4 => nameof(Func04TextColor),
            5 => nameof(Func05TextColor),
            6 => nameof(Func06TextColor),
            7 => nameof(Func07TextColor),
            8 => nameof(Func08TextColor),
            _ => string.Empty
        };

        // ===== MQTT =====
        private async Task StartMqttAsync()
        {
            try
            {
                _mqttCts?.Cancel();
                _mqttCts = new CancellationTokenSource();

                var factory = new MqttFactory();
                _mqttClient = factory.CreateMqttClient();

                _mqttClient.ApplicationMessageReceivedAsync += async e =>
                {
                    try
                    {
                        var payload = e.ApplicationMessage?.PayloadSegment.ToArray();
                        if (payload is null || payload.Length == 0) return;

                        var json = Encoding.UTF8.GetString(payload);
                        OnSensorsMessage(json);
                    }
                    catch { /* 무시 */ }
                    await Task.CompletedTask;
                };

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(MQTT_HOST, MQTT_PORT)
                    .WithClientId($"maui-client-{Environment.MachineName}-{Guid.NewGuid():N}")
                    .WithCleanSession()
                    .Build();

                await _mqttClient.ConnectAsync(options, _mqttCts.Token);
                await _mqttClient.SubscribeAsync(MQTT_TOPIC);

                MainThread.BeginInvokeOnMainThread(() =>
                    StatusText = $"MQTT 연결됨: {MQTT_HOST}:{MQTT_PORT} / {MQTT_TOPIC}");
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    StatusText = $"MQTT 오류: {ex.Message}");
            }
        }

        // 수신 JSON 파싱 → Metrics 갱신
        private void OnSensorsMessage(string json)
        {
            // 예상 예시:
            // {"gas":93.0,"humidity":45.2,"temp":25.0,"water_temp":-127.0,"ph":7.0}
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string? gas = TryGet(root, "gas");
                string? humidity = TryGet(root, "humidity");
                string? temp = TryGet(root, "temp");
                string? tds = TryGet(root, "tdsValue");
                string? waterTemp = TryGet(root, "water_temp");
                string? ph = TryGet(root, "ph");
               

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SetMetric("수온", ToDisplay(waterTemp, "°C"));
                    SetMetric("외부 온도", ToDisplay(temp, "°C"));
                    SetMetric("외부 습도", ToDisplay(humidity, "%"));
                    SetMetric("수질(TDS)",tds ?? "-");
                    SetMetric("수질(PH)", ph ?? "-");
                    SetMetric("가스 탐지수치", gas ?? "-");
                    // 수질(TDS)는 해당 토픽 오면 SetMetric으로 갱신하면 됨

                    RebuildRows();
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    StatusText = $"파싱 오류: {ex.Message}");
            }

            static string? TryGet(JsonElement root, string name)
            {
                if (root.TryGetProperty(name, out var el))
                {
                    return el.ValueKind switch
                    {
                        JsonValueKind.String => el.GetString(),
                        JsonValueKind.Number => el.ToString(),
                        _ => el.ToString()
                    };
                }
                return null;
            }

            static string ToDisplay(string? v, string unit)
                => (double.TryParse(v, out var d) ? d.ToString("0.##") : (v ?? "-")) + unit;
        }

        private void SetMetric(string name, string value)
        {
            var m = Metrics.FirstOrDefault(x => x.Name == name);
            if (m != null)
            {
                m.Value = value; // Metric은 INotifyPropertyChanged가 아니므로…
            }
        }

        // 종료/해제 시 호출(필요하다면 View Disappearing 등에서 호출)
        public async Task StopMqttAsync()
        {
            try
            {
                _mqttCts?.Cancel();
                if (_mqttClient?.IsConnected == true)
                    await _mqttClient.DisconnectAsync();
            }
            catch { }
        }
    }
}
