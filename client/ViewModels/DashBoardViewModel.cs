using client.Models;
using client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using client.Helps;

namespace client.ViewModels
{
    public partial class DashBoardViewModel : ObservableObject
    {
        // 버튼 색상: ON → 보라색, OFF → 기본색
        private Color GetFuncColor(int index) => _funcOn[index - 1] ? Colors.Purple : Color.FromArgb("#502bd4");
        private Color GetFuncTextColor(int index) => _funcOn[index - 1] ? Colors.Yellow :  Colors.White;

        // 각 버튼별 색상 프로퍼티
        public Color Func01Color => GetFuncColor(1);
        public Color Func02Color => GetFuncColor(2);
        public Color Func03Color => GetFuncColor(3);
        public Color Func04Color => GetFuncColor(4);
        public Color Func05Color => GetFuncColor(5);
        public Color Func06Color => GetFuncColor(6);
        public Color Func07Color => GetFuncColor(7);
        public Color Func08Color => GetFuncColor(8);

        public Color Func01TextColor => GetFuncTextColor(1);
        public Color Func02TextColor => GetFuncTextColor(2);
        public Color Func03TextColor => GetFuncTextColor(3);
        public Color Func04TextColor => GetFuncTextColor(4);
        public Color Func05TextColor => GetFuncTextColor(5);
        public Color Func06TextColor => GetFuncTextColor(6);
        public Color Func07TextColor => GetFuncTextColor(7);
        public Color Func08TextColor => GetFuncTextColor(8);


        // 버튼 수동조작 보호 시간 기록용
        private readonly DateTime[] _manualOverrideUntil = new DateTime[10];

        public ObservableCollection<Metric> Metrics { get; } = new();
        public ObservableCollection<MetricRow> MetricRows { get; } = new();

        [ObservableProperty] private string _statusText = "상태 바";
        [ObservableProperty] private int _fishCount;
        [ObservableProperty] private string _lastControl = "-";

        private readonly bool[] _funcOn = new bool[10];
        private readonly HashSet<int> _manualTriggerButtons = new() { 1, 2, 4, 5, 6, 8 };
        private CancellationTokenSource? _autoRevertCts;
        private readonly MqttService _mqtt;

        [ObservableProperty] private bool isAutoMode = true;
        public string AutoModelLabel => IsAutoMode ? "자동" : "수동";
        public string AutoModeLabel => AutoModelLabel;

        [ObservableProperty] private string selectedLedColor = "W";
        [ObservableProperty] private int intensity = 255;
        public string IntensityLabel => Intensity switch { <= 100 => "약", <= 170 => "중", _ => "강" };

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
            _mqtt.FishCountReceived += OnFishCountFromService;

            StatusText = _mqtt.IsConnected ? "MQTT 연결됨" : "MQTT 미연결";
        }

        partial void OnIsAutoModeChanged(bool value)
        {
            StatusText = value ? "자동 모드" : "수동 모드";
            OnPropertyChanged(nameof(AutoModelLabel));
            OnPropertyChanged(nameof(AutoModeLabel));
            _ = _mqtt.PublishControlAsync(value ? "N" : "Y");

            if (!value) // 수동 모드로 전환됨
            {
                for (int i = 0; i < _funcOn.Length; i++)
                {
                    _funcOn[i] = false;
                    UpdateFuncUi(i + 1);
                }
            }
        }

        partial void OnSelectedLedColorChanged(string value)
        {
            if (_funcOn[6]) // LED 기능이 ON일 때만 전송
                _ = SendLedCommandAsync();
        }

        partial void OnIntensityChanged(int value)
        {
            OnPropertyChanged(nameof(IntensityLabel));

            if (_funcOn[6]) // LED 기능이 ON일 때만 전송
                _ = SendLedCommandAsync();
        }
        [RelayCommand]
        private async Task Func07()
        {
            int index1 = 7;
            int i = index1 - 1;

            if (_funcOn[i]) // 켜져있으면 → 끔
            {
                await _mqtt.PublishControlAsync("M");
                _funcOn[i] = false;
                UpdateFuncUi(index1);
                StatusText = "LED OFF";
            }
            else // 꺼져있으면 → 현재 설정으로 켜기
            {
                var cmd = GetLedCommandFromUi();
                await _mqtt.PublishControlAsync(cmd);
                _funcOn[i] = true;
                UpdateFuncUi(index1);
                StatusText = $"LED ON: {cmd}";
            }
        }
        //[RelayCommand]
        //private async Task Func07Off()
        //{
        //    int index1 = 7;
        //    int i = index1 - 1;

        //    await _mqtt.PublishControlAsync("M"); // LED OFF 명령
        //    _funcOn[i] = false;
        //    UpdateFuncUi(index1);
        //}
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
                if (color != null)
                    m.TextColor = color;
            }
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

        private async Task ToggleFuncWithAutoAsync(int index1, bool isCurrentlyOn, string prefix, string onCmd, string offCmd, string recoveryCmd)
        {
            int i = index1 - 1;
            if (i < 0 || i >= 10) return;

            if (IsAutoMode)
            {
                if (!string.IsNullOrEmpty(prefix))
                    await _mqtt.PublishControlAsync(prefix);
                await _mqtt.PublishControlAsync(isCurrentlyOn ? offCmd : onCmd);
                _funcOn[i] = !isCurrentlyOn;
                UpdateFuncUi(index1);
                // 보호 시간 설정 (예: 5초, AppSettings.AutoSwitchSeconds 사용)
                _manualOverrideUntil[i] = DateTime.Now.AddSeconds(AppSettings.AutoSwitchSeconds);
                ScheduleAutoRevert(recoveryCmd, AppSettings.AutoSwitchSeconds * 1000);
            }
            else
            {
                await _mqtt.PublishControlAsync(isCurrentlyOn ? offCmd : onCmd);
                _funcOn[i] = !isCurrentlyOn;
                UpdateFuncUi(index1);
            }
        }

        private static readonly string[] FuncNames =
        {
            "히터", "팬", "산소", "여과", "펌프1", "펌프2", "LED", "먹이"
        };

        private void UpdateFuncUi(int index1)
        {
            // 상태바는 건드리지 않고 버튼 색상/텍스트만 갱신
            OnPropertyChanged($"Func0{index1}Text");
            OnPropertyChanged($"Func0{index1}Color");
            OnPropertyChanged($"Func0{index1}TextColor");
        }
        private void ScheduleAutoRevert(string recoveryCommand, int delayMs)
        {
            _autoRevertCts?.Cancel();
            _autoRevertCts = new CancellationTokenSource();
            var token = _autoRevertCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs, token);
                    await _mqtt.PublishControlAsync(recoveryCommand);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsAutoMode = true;
                        StatusText = "자동 모드로 전환됨";
                    });
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        private void OnControlFromService(string control) => MainThread.BeginInvokeOnMainThread(() => { LastControl = control; });
        private void OnFishCountFromService(int count) => MainThread.BeginInvokeOnMainThread(() => { FishCount = count; });

        private void OnLogsFromService(Dictionary<string, string> logs)
        {
            if (!IsAutoMode) return;

            int lastChangedIndex = -1;

            void Apply(string key, int index)
            {
                // 대소문자 무시해서 키 찾기
                var entry = logs.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    if (DateTime.Now < _manualOverrideUntil[index])
                        return;

                    bool newState = string.Equals(entry.Value, "ON", StringComparison.OrdinalIgnoreCase);
                    if (_funcOn[index] != newState) // 상태 변했을 때만
                    {
                        _funcOn[index] = newState;
                        UpdateFuncUi(index + 1);
                        lastChangedIndex = index;
                    }
                }
            }

            Apply("heater", 0);
            Apply("fan", 1);
            Apply("filtering", 3);
            Apply("pump1", 4);   // "pump1", "PUMP1" 모두 인식
            Apply("pump2", 5);   // "pump2", "PUMP2" 모두 인식
            Apply("feed", 7);    // "feed", "Feed" 모두 인식

            if (lastChangedIndex >= 0)
            {
                StatusText = $"{FuncNames[lastChangedIndex]} {(_funcOn[lastChangedIndex] ? "ON" : "OFF")}";
            }
        }

        [RelayCommand] private async Task Func01() => await ToggleFuncWithAutoAsync(1, _funcOn[0], "p", "a", "b", "o");
        [RelayCommand] private async Task Func02() => await ToggleFuncWithAutoAsync(2, _funcOn[1], "r", "c", "d", "q");
        [RelayCommand] private async Task Func03() => await ToggleFuncWithAutoAsync(3, _funcOn[2], "", "e", "f", "");
        [RelayCommand] private async Task Func04() => await ToggleFuncWithAutoAsync(4, _funcOn[3], "t", "g", "h", "s");
        [RelayCommand] private async Task Func05() => await ToggleFuncWithAutoAsync(5, _funcOn[4], "v", "i", "j", "u");
        [RelayCommand] private async Task Func06() => await ToggleFuncWithAutoAsync(6, _funcOn[5], "z", "k", "l", "y");
        //[RelayCommand] private async Task Func07() => await ToggleFuncWithAutoAsync(7, _funcOn[6], "", GetLedCommandFromUi(), "M", "");
        [RelayCommand] private async Task Func08() => await ToggleFuncWithAutoAsync(8, _funcOn[7], "x", "m", "n", "w");
        [RelayCommand] private async Task Func09() => await Shell.Current.GoToAsync("camera");
        [RelayCommand] private async Task Func10() => await ExportCsvAsync();
        [RelayCommand] private async Task OpenSettings() => await Shell.Current.GoToAsync("settings");

        private async Task SendLedCommandAsync()
        {
            string code = GetLedCommandFromUi();
            await _mqtt.PublishControlAsync(code);
            StatusText = $"LED 명령 전송: {code}";
        }

        private string GetLedCommandFromUi()
        {
            return (SelectedLedColor, IntensityLabel) switch
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
                //_ => "M"
            };
        }

        [RelayCommand]
        private async Task ExportCsvAsync()
        {
            var repo = new SensingRepository("Server=10.0.2.2;Port=3306;Database=kisame;Uid=root;Pwd=12345;SslMode=None;AllowPublicKeyRetrieval=True;");
            var rows = await repo.GetMergedDataAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,gas,humidity,temp,tdsValue,water_temp,ph,heater,fan,O2,filtering,pump1,pump2,feed,led");

            foreach (var row in rows)
            {
                var localTs = ToSeoulLocal(row.Timestamp);
                var s = row.Sensor;
                var l = row.Log;
                csv.AppendLine($"{localTs:yyyy-MM-dd HH:mm:ss},{s.gas},{s.humidity},{s.temp},{s.tdsValue},{s.water_temp},{s.ph},{l.heater},{l.fan},{l.O2},{l.filtering},{l.pump1},{l.pump2},{l.feed},{l.led}");
            }

            var filename = $"aquabox_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var path = Path.Combine(FileSystem.CacheDirectory, filename);
            File.WriteAllText(path, csv.ToString(), Encoding.UTF8);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "센서/로그 CSV 내보내기",
                File = new ShareFile(path)
            });
        }

        private static DateTime ToSeoulLocal(DateTime dt)
        {
            var utc = dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            try { return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul")); }
            catch { return utc.AddHours(9); }
        }

        [RelayCommand] private void OpenDetail(Metric? m) => StatusText = $"[{m?.Name}] 상세요청";
    }
}
