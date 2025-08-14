using client.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using client.Models;

namespace client.ViewModels
{    
    public class MainViewModel : BindableObject
    {
        public ObservableCollection<Metric> Metrics { get; } = new();
        public ObservableCollection<MetricRow> MetricRows { get; } = new();

        private string _statusText = "상태 바";

        private int _fishCount;
        public int FishCount
        {
            get => _fishCount;
            set
            {
                if (_fishCount != value)
                {
                    _fishCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FishCount));
                }
            }
        }
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private double _intensity = 85;
        public double Intensity
        {
            get => _intensity;
            set
            {
                if (Math.Abs(_intensity - value) < 0.5)
                {
                    _intensity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IntensityLabel));
                }
            }
        }

        public string IntensityLabel
        {
            get
            {
                return _intensity switch
                {
                    <= 127.5 => "현재 단계: 약 (85)",
                    <= 212.5 => "현재 단계: 중 (170)",
                    _ => "현재 단계: 강 (255)",
                };
            }
        }

        public ICommand OpenDetailCommand { get; }
        public ICommand MainFuncCommand { get; }
        public ICommand Func01Command { get; }
        public ICommand Func02Command { get; }
        public ICommand Func03Command { get; }      // 워터펌프 토글
        public ICommand Func04Command { get; }
        public ICommand Func05Command { get; }
        public ICommand Func06Command { get; }
        public ICommand Func07Command { get; }
        public ICommand Func08Command { get; }
        public ICommand Func09Command { get; }
        public ICommand Func10Command { get; }

        //워터펌프 인스턴스 & 토글 상태
        private readonly WaterPump1 _pump1 = new();
        private bool _isPumpOn = false;

        public MainViewModel()
        {
            // 샘플 데이터       [0,0], [0,1], [1,0], [1,1] 순서로 배열됨
            Metrics.Add(new Metric { Name = "수온", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "외부 온도", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "수질(TDS)", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "외부 습도", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "수질(PH)", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "가스 탐지수치", Value = "바인딩" });

            RebuildRows();

            //mqtt 연결비동기 
            _ = InitMqttAsync();

            OpenDetailCommand = new Command<Metric>(m =>
            {
                StatusText = $"[{m?.Name}] 상세요청";
            });

            Func01Command = new Command(() => StatusText = "기능 01 실행");       // 위와 동일
            Func02Command = new Command(() => StatusText = "기능 02 실행");
            Func03Command = new Command(async () =>
            {
                try
                {
                    if (_isPumpOn)
                    {
                        await _pump1.TurnOffAsync(); // f
                        _isPumpOn = false;
                        StatusText = "워터펌프1 OFF(f) 전송";
                    }
                    else
                    {
                        await _pump1.TurnOnAsync();  // e
                        _isPumpOn = true;
                        StatusText = "워터펌프1 ON(e) 전송";
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"워터펌프1 오류: {ex.Message}";
                }
            });
            Func04Command = new Command(() => StatusText = "기능 04 실행");
            Func05Command = new Command(() => StatusText = "기능 05 실행");
            Func06Command = new Command(() => StatusText = "기능 06 실행");
            Func07Command = new Command(() => StatusText = "기능 07 실행");
            Func08Command = new Command(() => StatusText = "기능 08 실행");
            Func09Command = new Command(() => StatusText = "기능 09 실행");
            Func10Command = new Command(() => StatusText = "기능 10 실행");
        }

        private async Task InitMqttAsync()
        {
            try
            {
                await _pump1.ConnectAsync();
                StatusText = "워터펌프1 MQTT 연결 완료";
            }
            catch (Exception ex)
            {
                StatusText = $"MQTT 연결 실패: {ex.Message}";
            }
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
    }
}
