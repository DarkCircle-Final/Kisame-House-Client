// DashBoardViewModel.cs 전체 코드 (수정됨)

using client.Models;
using client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
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
        private readonly Color _funcBaseColor = Color.FromArgb("#512BD4");
        private readonly double _lightenAmount = 0.7;

        [ObservableProperty] private bool isAutoMode = true;
        public string AutoModelLabel => IsAutoMode ? "자동" : "수동";
        public string AutoModeLabel => AutoModelLabel;

        private readonly HashSet<int> _manualTriggerButtons = new() { 1, 2, 3, 4, 5, 6 };
        private CancellationTokenSource? _autoRevertCts;

        private readonly MqttService _mqtt;

        private double _intensity = 85;
        public double Intensity
        {
            get => _intensity;
            set
            {
                double clamped = Math.Max(85.0, Math.Min(255.0, value));
                double nearest = Math.Round(clamped / 85.0) * 85.0 + 85.0;
                if (SetProperty(ref _intensity, nearest))
                    OnPropertyChanged(nameof(IntensityLabel));
            }
        }

        public string IntensityLabel =>
            _intensity switch
            {
                <= 127.5 => "현재 단계: 약 (85)",
                <= 212.5 => "현재 단계: 중 (170)",
                _ => "현재 단계: 강 (255)",
            };

        partial void OnIsAutoModeChanged(bool value)
        {
            StatusText = value ? "자동 모드" : "수동 모드";
            OnPropertyChanged(nameof(AutoModelLabel));
            OnPropertyChanged(nameof(AutoModeLabel));
        }

        public DashBoardViewModel()
        {
            Metrics.Add(new Metric { Name = "수온(°C)", Value = "대기" });
            Metrics.Add(new Metric { Name = "외부 온도(°C)", Value = "대기" });
            Metrics.Add(new Metric { Name = "수질(TDS)", Value = "-" });
            Metrics.Add(new Metric { Name = "외부 습도(%)", Value = "대기" });
            Metrics.Add(new Metric { Name = "수질(PH)", Value = "대기" });
            Metrics.Add(new Metric { Name = "가스 수치", Value = "대기" });
            RebuildRows();

            _mqtt = new MqttService(
                "210.119.12.68",
                1883,
                "Server=10.0.2.2;Port=3306;Database=kisame;Uid=root;Pwd=12345;SslMode=None;AllowPublicKeyRetrieval=True;");
            _mqtt.SensorsReceived += OnSensorsFromService;
            _mqtt.LogsReceived += OnLogsFromService;
            _mqtt.ControlReceived += OnControlFromService;

            _ = _mqtt.ConnectAsync();
            StatusText = "MQTT 연결 시도 중…";
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
                //StatusText = "센서 갱신됨";
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
                if (color != null)
                    m.TextColor = color;
            }
        }

        private void OnLogsFromService(Dictionary<string, string> logs) { }

        private void OnControlFromService(string control)
        {
            MainThread.BeginInvokeOnMainThread(() => { LastControl = control; });
        }

        private async Task SendControlAsync(string code) => await _mqtt.PublishControlAsync(code);

        [RelayCommand] private async Task Func01() { ToggleFunc(1); await SendControlAsync("a"); }
        [RelayCommand] private async Task Func02() { ToggleFunc(2); await SendControlAsync("b"); }
        [RelayCommand] private async Task Func03() { ToggleFunc(3); await SendControlAsync("c"); }
        [RelayCommand] private async Task Func04() { ToggleFunc(4); await SendControlAsync("d"); }
        [RelayCommand] private async Task Func05() { ToggleFunc(5); await SendControlAsync("e"); }
        [RelayCommand] private async Task Func06() { ToggleFunc(6); await SendControlAsync("f"); }
        [RelayCommand] private async Task Func07() { ToggleFunc(7); await SendControlAsync("g"); }
        [RelayCommand] private async Task Func08() { ToggleFunc(8); await SendControlAsync("h"); }
        [RelayCommand] private async Task Func09() => await Shell.Current.GoToAsync("camera");
        [RelayCommand] private async Task Func10() => await Shell.Current.GoToAsync("logs");
        [RelayCommand] private async Task OpenSettings() => await Shell.Current.GoToAsync("settings");

        private void ToggleFunc(int index1)
        {
            int i = index1 - 1;
            if (i < 0 || i >= 10) return;

            _funcOn[i] = !_funcOn[i];
            StatusText = $"기능 {index1} {(_funcOn[i] ? "ON" : "OFF")}";

            OnPropertyChanged($"Func0{index1}Text");
            OnPropertyChanged($"Func0{index1}Color");
            OnPropertyChanged($"Func0{index1}TextColor");

            if (_funcOn[i] && _manualTriggerButtons.Contains(index1))
                ScheduleAutoRevert(5000);
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
                var row = new MetricRow();

                var left = Metrics[i];
                row.LeftName = left.Name;
                row.LeftValue = left.Value;
                row.LeftTextColor = left.TextColor;

                if (i + 1 < Metrics.Count)
                {
                    var right = Metrics[i + 1];
                    row.RightName = right.Name;
                    row.RightValue = right.Value;
                    row.RightTextColor = right.TextColor;
                }

                MetricRows.Add(row);
            }

            OnPropertyChanged(nameof(MetricRows));
        }

        [RelayCommand] private void OpenDetail(Metric? m) => StatusText = $"[{m?.Name}] 상세요청";
    }
}
