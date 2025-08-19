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
        private readonly double _lightenAmount = 0.7; // 0~1 (1일 때 더연함)

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

        // 색 반전기능
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


        // 버튼 텍스트 색
        public Color Func01TextColor => GetTextColor(Func01Color);
        public Color Func02TextColor => GetTextColor(Func02Color);
        public Color Func03TextColor => GetTextColor(Func03Color);
        public Color Func04TextColor => GetTextColor(Func04Color);
        public Color Func05TextColor => GetTextColor(Func05Color);
        public Color Func06TextColor => GetTextColor(Func06Color);
        public Color Func07TextColor => GetTextColor(Func07Color);
        public Color Func08TextColor => GetTextColor(Func08Color);
        //public Color Func09TextColor => GetTextColor(Func09Color);
        //public Color Func10TextColor => GetTextColor(Func10Color);


        // 자동/수동 스위치
        [ObservableProperty]
        private bool isAutoMode = true; // 자동 모드 기본값

        public string AutoModelLabel => IsAutoMode ? "자동" : "수동";
        public string AutoModeLabel => AutoModelLabel;

        partial void OnIsAutoModeChanged(bool value)
        {
            StatusText = value ? "자동 모드" : "수동 모드";
            OnPropertyChanged(nameof(AutoModelLabel));
            OnPropertyChanged(nameof(AutoModelLabel));
        }

        // 자동->수동 전환 및 일정시간뒤 자동으로 전환
        private readonly HashSet<int> _manualTriggerButtons = new() { 1, 2, 3, 4, 5, 6 }; // 버튼번호 입력
        private CancellationTokenSource? _autoRevertCts;



        // 생성자
        public DashBoardViewModel()
        {
            // 샘플 데이터       [0,0], [0,1], [1,0], [1,1] 순서로 배열됨
            Metrics.Add(new Metric { Name = "수온", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "외부 온도", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "수질(TDS)", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "외부 습도", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "수질(PH)", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "가스 탐지수치", Value = "바인딩" });

            RebuildRows();
        }

        [RelayCommand]
        private void OpenDetail(Metric? m)
        {
            StatusText = $"[{m?.Name}] 상세요청";
        }

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

        // 설정창 들어가기
        [RelayCommand] private async Task OpenSettings() => await Shell.Current.GoToAsync("settings");
        

        // 로직
        // 배경 색 따라 텍스트 색 변경
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

            // 상태바 텍스트
            StatusText = $"기능 {index1} {(_funcOn[i] ? "ON" : "OFF")}";

            OnPropertyChanged(GetFuncTextPropertyName(index1));
            OnPropertyChanged(GetFuncColorPropertyName(index1));
            OnPropertyChanged(GetFuncTextColorPropertyName(index1));

            // 자동 -> 수동 기능
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
                catch (TaskCanceledException) { /* 마지막 클릭 우선 */}
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
            //9 => nameof(Func09TextColor),
            //10 => nameof(Func10TextColor),
            _ => string.Empty
        };
    }
}
