using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace client.ViewModels
{
    public class Metric
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
    }
    public class MainViewModel : BindableObject
    {
        public ObservableCollection<Metric> Metrics { get; } = new();

        private string _statusText = "Loading...";
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand OpenDetailCommand { get; }
        public ICommand MainFuncCommand { get; }
        public ICommand Func1Command { get; }
        public ICommand Func2Command { get; }
        public ICommand Func3Command { get; }
        public ICommand Func4Command { get; }
        public ICommand Func5Command { get; }
        public ICommand Func6Command { get; }
        public ICommand Func7Command { get; }
        public ICommand Func8Command { get; }


        public MainViewModel()
        {
            // 샘플 데이터
            Metrics.Add(new Metric { Name = "수온", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "외부 온도", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "외부 습도", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "가스 탐지수치", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "수질(TDS)", Value = "바인딩" });
            Metrics.Add(new Metric { Name = "수질(PH)", Value = "바인딩" });

            OpenDetailCommand = new Command<Metric>(m =>
            {
                StatusText = $"[{m?.Name}] 상세요청";
            });

            MainFuncCommand = new Command(() => StatusText = "메인 동작 실행");   // 상태창에 뜰 텍스트니 차후 버튼정해지면 변경

            Func1Command = new Command(() => StatusText = "기능 1 실행");       // 위와 동일
            Func2Command = new Command(() => StatusText = "기능 2 실행");
            Func3Command = new Command(() => StatusText = "기능 3 실행");
            Func4Command = new Command(() => StatusText = "기능 4 실행");
            Func5Command = new Command(() => StatusText = "기능 5 실행");
            Func6Command = new Command(() => StatusText = "기능 6 실행");
            Func7Command = new Command(() => StatusText = "기능 7 실행");
            Func8Command = new Command(() => StatusText = "기능 8 실행");
        }
    }
}
