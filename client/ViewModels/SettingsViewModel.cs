using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace client.ViewModels
{
    public class SettingsViewModel : BindableObject
    {
        private int _auto = 1;
        private int _heater = 1;
        private int _fan = 1;
        private int _filter = 1;
        private int _pump = 1;
        private int _feed = 1;

        // 선택값
        public int AutoSwitchTimeSelected { get => _auto; set { if (_auto == value) return; _auto = value; OnPropertyChanged(); } }
        public int HeaterSelected { get => _heater; set { if (_heater == value) return; _heater = value; OnPropertyChanged(); } }
        public int CoolingFanSelected { get => _fan; set { if (_fan == value) return; _fan = value; OnPropertyChanged(); } }
        public int FilterSelected { get => _filter; set { if (_filter == value) return; _filter = value; OnPropertyChanged(); } }
        public int PumpSelected { get => _pump; set { if (_pump == value) return; _pump = value; OnPropertyChanged(); } }
        public int FeederSelected { get => _feed; set { if (_feed == value) return; _feed = value; OnPropertyChanged(); } }


        // CSV 관련
        public ICommand GobackCOmmand => new Command(async () => await Shell.Current.GoToAsync(".."));
        public ICommand ExportCsvCommand => new Command(ExportCsv);

        private void ExportCsv()
        {
            // CSV 파일로 내보내는 로직을 구현
            Application.Current?.MainPage?.DisplayAlert("CSV", "CSV 내보내기 실행", "OK");
        }
    }
}
