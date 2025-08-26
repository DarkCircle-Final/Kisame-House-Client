//using client.Models;
//using client.Services;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using Microsoft.Maui.ApplicationModel;
//using Microsoft.Maui.Controls;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace client.ViewModels
//{
//    public partial class DashBoardViewModel : ObservableObject
//    {
//        public ObservableCollection<Metric> Metrics { get; } = new();
//        public ObservableCollection<MetricRow> MetricRows { get; } = new();

//        [ObservableProperty] private string _statusText = "상태 바";
//        [ObservableProperty] private int _fishCount;
//        [ObservableProperty] private string _lastControl = "-";

//        private readonly bool[] _funcOn = new bool[10];
//        private readonly Color _funcBaseColor = Color.FromArgb("#512BD4");
//        private readonly double _lightenAmount = 0.7;

//        [ObservableProperty] private bool isAutoMode = true;
//        public string AutoModelLabel => IsAutoMode ? "자동" : "수동";
//        public string AutoModeLabel => AutoModelLabel;

//        private readonly HashSet<int> _manualTriggerButtons = new() { 1, 2, 3, 4, 5, 6 };
//        private CancellationTokenSource? _autoRevertCts;

//        private readonly MqttService _mqtt;

//        private double _intensity = 85;
//        public double Intensity
//        {
//            get => _intensity;
//            set
//            {
//                double clamped = Math.Max(85.0, Math.Min(255.0, value));
//                double nearest = Math.Round(clamped / 85.0) * 85.0 + 85.0;
//                if (SetProperty(ref _intensity, nearest))
//                    OnPropertyChanged(nameof(IntensityLabel));
//            }
//        }

//        public string IntensityLabel =>
//            _intensity switch
//            {
//                <= 127.5 => "현재 단계: 약 (85)",
//                <= 212.5 => "현재 단계: 중 (170)",
//                _ => "현재 단계: 강 (255)",
//            };

//        partial void OnIsAutoModeChanged(bool value)
//        {
//            StatusText = value ? "자동 모드" : "수동 모드";
//            OnPropertyChanged(nameof(AutoModelLabel));
//            OnPropertyChanged(nameof(AutoModeLabel));
//        }

//        public DashBoardViewModel()
//        {
//            Metrics.Add(new Metric { Name = "수온(°C)", Value = "대기" });
//            Metrics.Add(new Metric { Name = "외부 온도(°C)", Value = "대기" });
//            Metrics.Add(new Metric { Name = "수질(TDS)", Value = "-" });
//            Metrics.Add(new Metric { Name = "외부 습도(%)", Value = "대기" });
//            Metrics.Add(new Metric { Name = "수질(PH)", Value = "대기" });
//            Metrics.Add(new Metric { Name = "가스 수치", Value = "대기" });
//            RebuildRows();

//            _mqtt = App.Mqtt; // 전역 MQTT 재사용
//            _mqtt.SensorsReceived += OnSensorsFromService;
//            _mqtt.LogsReceived += OnLogsFromService;
//            _mqtt.ControlReceived += OnControlFromService;

//            StatusText = _mqtt.IsConnected ? "MQTT 연결됨" : "MQTT 미연결";
//        }

//        private void OnSensorsFromService(Dictionary<string, string> data)
//        {
//            string? Try(string k) => data.TryGetValue(k, out var v) ? v : null;

//            MainThread.BeginInvokeOnMainThread(() =>
//            {
//                SetMetric("수온(°C)", Try("water_temp") ?? "-");
//                SetMetric("외부 온도(°C)", Try("temp") ?? "-");
//                SetMetric("외부 습도(%)", Try("humidity") ?? "-");
//                SetMetric("수질(TDS)", Try("tdsValue") ?? "-");
//                SetMetric("수질(PH)", Try("ph") ?? "-");

//                var gasRaw = Try("gas");
//                var (gasText, gasColor) = GetGasLevelTextAndColor(gasRaw);
//                SetMetric("가스 수치", gasText, gasColor);

//                RebuildRows();
//            });
//        }

//        private (string Text, Color Color) GetGasLevelTextAndColor(string? value)
//        {
//            if (float.TryParse(value, out var v))
//            {
//                if (v <= 150) return ("정상", Colors.Black);
//                else if (v <= 250) return ("주의", Colors.Orange);
//                else return ("위험", Colors.Red);
//            }
//            return ("-", Colors.Gray);
//        }

//        private void SetMetric(string name, string value, Color? color = null)
//        {
//            var m = Metrics.FirstOrDefault(x => x.Name == name);
//            if (m != null)
//            {
//                m.Value = value;
//                if (color != null)
//                    m.TextColor = color;
//            }
//        }

//        private void OnLogsFromService(Dictionary<string, string> logs) { }

//        private void OnControlFromService(string control)
//        {
//            MainThread.BeginInvokeOnMainThread(() => { LastControl = control; });
//        }

//        private async Task SendControlAsync(string code) => await _mqtt.PublishControlAsync(code);

//        [RelayCommand] private async Task Func01() { ToggleFunc(1); await SendControlAsync("a"); }
//        [RelayCommand] private async Task Func02() { ToggleFunc(2); await SendControlAsync("b"); }
//        [RelayCommand] private async Task Func03() { ToggleFunc(3); await SendControlAsync("c"); }
//        [RelayCommand] private async Task Func04() { ToggleFunc(4); await SendControlAsync("d"); }
//        [RelayCommand] private async Task Func05() { ToggleFunc(5); await SendControlAsync("e"); }
//        [RelayCommand] private async Task Func06() { ToggleFunc(6); await SendControlAsync("f"); }
//        [RelayCommand] private async Task Func07() { ToggleFunc(7); await SendControlAsync("g"); }
//        [RelayCommand] private async Task Func08() { ToggleFunc(8); await SendControlAsync("h"); }
//        [RelayCommand] private async Task Func09() => await Shell.Current.GoToAsync("camera");
//        [RelayCommand] private async Task Func10() => await Shell.Current.GoToAsync("logs");
//        [RelayCommand] private async Task OpenSettings() => await Shell.Current.GoToAsync("settings");

//        private void ToggleFunc(int index1)
//        {
//            int i = index1 - 1;
//            if (i < 0 || i >= 10) return;

//            _funcOn[i] = !_funcOn[i];
//            StatusText = $"기능 {index1} {(_funcOn[i] ? "ON" : "OFF")}";

//            OnPropertyChanged($"Func0{index1}Text");
//            OnPropertyChanged($"Func0{index1}Color");
//            OnPropertyChanged($"Func0{index1}TextColor");

//            if (_funcOn[i] && _manualTriggerButtons.Contains(index1))
//                ScheduleAutoRevert(5000);
//        }

//        private void ScheduleAutoRevert(int delayMs = 5000)
//        {
//            _autoRevertCts?.Cancel();
//            _autoRevertCts = new CancellationTokenSource();
//            var token = _autoRevertCts.Token;

//            _ = Task.Run(async () =>
//            {
//                try
//                {
//                    await Task.Delay(delayMs, token);
//                    MainThread.BeginInvokeOnMainThread(() =>
//                    {
//                        IsAutoMode = true;
//                        StatusText = "자동 모드로 전환됨";
//                    });
//                }
//                catch (TaskCanceledException) { }
//            }, token);
//        }

//        private void RebuildRows()
//        {
//            MetricRows.Clear();

//            for (int i = 0; i < Metrics.Count; i += 2)
//            {
//                var row = new MetricRow();

//                var left = Metrics[i];
//                row.LeftName = left.Name;
//                row.LeftValue = left.Value;
//                row.LeftTextColor = left.TextColor;

//                if (i + 1 < Metrics.Count)
//                {
//                    var right = Metrics[i + 1];
//                    row.RightName = right.Name;
//                    row.RightValue = right.Value;
//                    row.RightTextColor = right.TextColor;
//                }

//                MetricRows.Add(row);
//            }

//            OnPropertyChanged(nameof(MetricRows));
//        }

//        [RelayCommand] private void OpenDetail(Metric? m) => StatusText = $"[{m?.Name}] 상세요청";
//    }
//}


using client.Models;
using client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Preferences
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace client.ViewModels
{
    public partial class DashBoardViewModel : ObservableObject
    {
        public ObservableCollection<Metric> Metrics { get; } = new();
        public ObservableCollection<MetricRow> MetricRows { get; } = new();

        [ObservableProperty] private string _statusText = "상태 바";
        [ObservableProperty] private int _fishCount;
        [ObservableProperty] private string _lastControl = "-";

        private readonly bool[] _funcOn = new bool[10];

        [ObservableProperty] private bool isAutoMode = true;           // 상단 토글
        [ObservableProperty] private bool isAutoRevertEnabled = true;  // 체크박스: ON=고정, OFF=자동전환 허용

        public string AutoModelLabel => IsAutoMode ? "자동" : "수동";
        public string AutoModeLabel => AutoModelLabel;

        private CancellationTokenSource? _autoRevertCts;
        private readonly MqttService _mqtt;

        // 버튼 활성 규칙
        public bool CanManualControl =>
            (!IsAutoMode && IsAutoRevertEnabled)   // 수동 + 체크ON = 수동 유지
            || (!IsAutoMode && !IsAutoRevertEnabled) // 수동 + 체크OFF = 수동 가능(자동복귀)
            || (IsAutoMode && !IsAutoRevertEnabled); // 자동 + 체크OFF = 수동 가능(자동복귀)

        // 버튼 색상
        private Color GetButtonColor(int index)
        {
            bool isOn = _funcOn[index - 1];
            if (IsAutoMode)
                return isOn ? Color.FromArgb("#E91E63") : Color.FromArgb("#F8BBD0"); // 자동: 진/연 분홍
            else
                return isOn ? Color.FromArgb("#388E3C") : Color.FromArgb("#A5D6A7"); // 수동: 진/연 초록
        }
        public Color Func01Color => GetButtonColor(1);
        public Color Func02Color => GetButtonColor(2);
        public Color Func03Color => GetButtonColor(3);
        public Color Func04Color => GetButtonColor(4);
        public Color Func05Color => GetButtonColor(5);
        public Color Func06Color => GetButtonColor(6);

        // LED 강도 (85/170/255로 스냅)
        private double _intensity = 85;
        public double Intensity
        {
            get => _intensity;
            set
            {
                double clamped = Math.Max(85.0, Math.Min(255.0, value));
                double snapped = (clamped <= 127.5) ? 85 : (clamped <= 212.5 ? 170 : 255);
                if (SetProperty(ref _intensity, snapped))
                    OnPropertyChanged(nameof(IntensityLabel));
            }
        }
        public string IntensityLabel =>
            _intensity switch { <= 127.5 => "현재 단계: 약 (85)", <= 212.5 => "현재 단계: 중 (170)", _ => "현재 단계: 강 (255)", };

        partial void OnIsAutoModeChanged(bool value)
        {
            StatusText = value ? "자동 모드" : "수동 모드";
            OnPropertyChanged(nameof(AutoModelLabel));
            OnPropertyChanged(nameof(AutoModeLabel));
            OnPropertyChanged(nameof(CanManualControl));
            for (int i = 1; i <= 6; i++) OnPropertyChanged($"Func0{i}Color"); // 색상 갱신
        }
        partial void OnIsAutoRevertEnabledChanged(bool value) => OnPropertyChanged(nameof(CanManualControl));

        public DashBoardViewModel()
        {
            Metrics.Add(new Metric { Name = "수온(°C)", Value = "대기" });
            Metrics.Add(new Metric { Name = "외부 온도(°C)", Value = "대기" });
            Metrics.Add(new Metric { Name = "수질(TDS)", Value = "-" });
            Metrics.Add(new Metric { Name = "외부 습도(%)", Value = "대기" });
            Metrics.Add(new Metric { Name = "수질(PH)", Value = "대기" });
            Metrics.Add(new Metric { Name = "가스 수치", Value = "대기" });
            RebuildRows();

            _mqtt = App.Mqtt;
            _mqtt.SensorsReceived += OnSensorsFromService;
            _mqtt.LogsReceived += OnLogsFromService;
            _mqtt.ControlReceived += OnControlFromService;

            StatusText = _mqtt.IsConnected ? "MQTT 연결됨" : "MQTT 미연결";
        }

        private void OnSensorsFromService(Dictionary<string, string> data)
        {
            string? Try(string k) => data.TryGetValue(k, out var v) ? v : null;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SetMetric("수온(°C)", Try("water_temp") ?? "-");
                SetMetric("외부 온도(°C)", Try("temp") ?? "-");
                SetMetric("외부 습도(%)", Try("humidity") ?? "-");
                SetMetric("수질(TDS)", Try("tdsValue") ?? "-");
                SetMetric("수질(PH)", Try("ph") ?? "-");

                var gasRaw = Try("gas");
                var (gasText, gasColor) = GetGasLevelTextAndColor(gasRaw);
                SetMetric("가스 수치", gasText, gasColor);

                RebuildRows();
            });
        }

        private (string Text, Color Color) GetGasLevelTextAndColor(string? value)
        {
            if (float.TryParse(value, out var v))
            {
                if (v <= 150) return ("정상", Colors.Black);
                else if (v <= 250) return ("주의", Colors.Orange);
                else return ("위험", Colors.Red);
            }
            return ("-", Colors.Gray);
        }

        private void SetMetric(string name, string value, Color? color = null)
        {
            var m = Metrics.FirstOrDefault(x => x.Name == name);
            if (m != null)
            {
                m.Value = value;
                if (color != null) m.TextColor = color;
            }
        }

        private void OnLogsFromService(Dictionary<string, string> logs)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (logs.TryGetValue("heater", out var heater)) SetFuncState(1, heater == "on");
                if (logs.TryGetValue("fan", out var fan)) SetFuncState(2, fan == "on");
                if (logs.TryGetValue("filtering", out var filter)) SetFuncState(3, filter == "on");
                if (logs.TryGetValue("pump1", out var pump1)) SetFuncState(4, pump1 == "on");
                if (logs.TryGetValue("pump2", out var pump2)) SetFuncState(5, pump2 == "on");
                if (logs.TryGetValue("feed", out var feed)) SetFuncState(6, feed == "on");
            });
        }

        private void OnControlFromService(string control)
        {
            MainThread.BeginInvokeOnMainThread(() => { LastControl = control; });
        }

        private void SetFuncState(int index, bool on)
        {
            _funcOn[index - 1] = on;
            OnPropertyChanged($"Func0{index}Color");
        }

        private async Task SendPairAsync(string first, string second)
        {
            await _mqtt.PublishControlAsync(first);
            await Task.Delay(100);
            await _mqtt.PublishControlAsync(second);
        }

        // 장치 제어
        private Task SendHeaterAsync(bool on) => on ? SendPairAsync("p", "a") : SendPairAsync("p", "b");
        private Task SendFanAsync(bool on) => on ? SendPairAsync("r", "c") : SendPairAsync("r", "d");
        private Task SendFilterAsync(bool on) => on ? SendPairAsync("t", "g") : SendPairAsync("t", "h");
        private Task SendPump1Async(bool on) => on ? SendPairAsync("v", "i") : SendPairAsync("v", "j");
        private Task SendPump2Async(bool on) => on ? SendPairAsync("z", "k") : SendPairAsync("z", "l");
        private Task SendFeederAsync(bool on) => on ? SendPairAsync("x", "m") : SendPairAsync("x", "n");

        [RelayCommand] private async Task Func01() { if (!CanManualControl) return; ToggleFunc(1); await SendHeaterAsync(_funcOn[0]); }
        [RelayCommand] private async Task Func02() { if (!CanManualControl) return; ToggleFunc(2); await SendFanAsync(_funcOn[1]); }
        [RelayCommand] private async Task Func03() { if (!CanManualControl) return; ToggleFunc(3); await SendFilterAsync(_funcOn[2]); }
        [RelayCommand] private async Task Func04() { if (!CanManualControl) return; ToggleFunc(4); await SendPump1Async(_funcOn[3]); }
        [RelayCommand] private async Task Func05() { if (!CanManualControl) return; ToggleFunc(5); await SendPump2Async(_funcOn[4]); }
        [RelayCommand] private async Task Func06() { if (!CanManualControl) return; ToggleFunc(6); await SendFeederAsync(_funcOn[5]); }
        [RelayCommand] private async Task Func09() => await Shell.Current.GoToAsync("camera");
        [RelayCommand] private async Task Func10() => await Shell.Current.GoToAsync("logs");
        [RelayCommand] private async Task OpenSettings() => await Shell.Current.GoToAsync("settings");

        private void ToggleFunc(int index)
        {
            int i = index - 1; if (i < 0 || i >= 10) return;
            _funcOn[i] = !_funcOn[i];
            StatusText = $"기능 {index} {(_funcOn[i] ? "ON" : "OFF")}";
            OnPropertyChanged($"Func0{index}Color");

            // 체크박스 OFF일 때만 자동복귀 (10/15/20초)
            if (!IsAutoRevertEnabled)
            {
                int sel = Preferences.Get("AutoSwitchTimeSelected", 1);
                int delayMs = sel switch { 1 => 10000, 2 => 15000, 3 => 20000, _ => 10000 };
                ScheduleAutoRevert(delayMs);
            }
        }

        private void ScheduleAutoRevert(int delayMs)
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
                var row = new MetricRow();
                var left = Metrics[i];
                row.LeftName = left.Name; row.LeftValue = left.Value; row.LeftTextColor = left.TextColor;

                if (i + 1 < Metrics.Count)
                {
                    var right = Metrics[i + 1];
                    row.RightName = right.Name; row.RightValue = right.Value; row.RightTextColor = right.TextColor;
                }
                MetricRows.Add(row);
            }
            OnPropertyChanged(nameof(MetricRows));
        }

        [RelayCommand] private void OpenDetail(Metric? m) => StatusText = $"[{m?.Name}] 상세요청";

        // LED 선택 (코드비하인드에서 호출)
        [RelayCommand]
        public async Task LedSelect(string? color)
        {
            if (string.IsNullOrEmpty(color)) return;

            string intensity = Intensity switch { <= 127.5 => "약", <= 212.5 => "중", _ => "강" };
            string code = (color, intensity) switch
            {
                ("W", "강") => "A",
                ("W", "중") => "B",
                ("W", "약") => "C",
                ("R", "강") => "D",
                ("R", "중") => "E",
                ("R", "약") => "F",
                ("G", "강") => "G",
                ("G", "중") => "H",
                ("G", "약") => "I",
                ("B", "강") => "J",
                ("B", "중") => "K",
                ("B", "약") => "L",
                _ => "M"
            };
            await _mqtt.PublishControlAsync(code);
        }
    }
}
