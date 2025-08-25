//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Input;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;

//namespace client.ViewModels
//{
//    public partial class EntryViewModel : ObservableObject
//    {
//        [ObservableProperty]
//        [NotifyPropertyChangedFor(nameof(CanConfirm))]
//        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
//        private string serial = string.Empty;

//        [ObservableProperty] private string nickname = string.Empty;
//        [ObservableProperty] private string error = string.Empty;

//        // 시리얼 하드 코딩 -> 추후 교체요망
//        private readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase)
//        {
//            "19960423", "20090523", "15885588"
//        };

//        public bool CanConfirm => !string.IsNullOrWhiteSpace(Serial);



//        [RelayCommand(CanExecute = nameof(CanConfirm))]
//        private async Task ConfirmAsync()
//        {
//            Error = "";
//            var s = Serial?.Trim() ?? "";
//            if (_allowed.Contains(s))
//            {
//                // 닉네임등 필요시 상태전달방식 요망
//                await Shell.Current.GoToAsync(nameof(Views.DashBoardView));   // 루트전환
//            }
//            else
//            {
//                Error = "허용되지 않은 시리얼입니다. 다시 입력하세요";
//            }
//        }

//        [RelayCommand]
//        // 개발자용 우회
//        private async Task DevBypassAsync()
//        {
//            await Shell.Current.GoToAsync(nameof(Views.DashBoardView));   // 루트전환
//        }
//    }
//}


using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using client.Services;

namespace client.ViewModels
{
    public partial class EntryViewModel : ObservableObject
    {
        private readonly MqttService _mqtt = App.Mqtt; // 전역 인스턴스 사용

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private string serial = string.Empty;

        [ObservableProperty] private string nickname = string.Empty;
        [ObservableProperty] private string error = string.Empty;

        private readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            "19960423", "20090523", "15885588"
        };

        public bool CanConfirm => !string.IsNullOrWhiteSpace(Serial);

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private async Task ConfirmAsync()
        {
            Error = "";
            var s = Serial?.Trim() ?? "";
            if (_allowed.Contains(s))
            {
                await _mqtt.PublishControlAsync("N"); //  "N" 발행
                await Shell.Current.GoToAsync(nameof(Views.DashBoardView));
            }
            else
            {
                Error = "허용되지 않은 시리얼입니다. 다시 입력하세요";
            }
        }

        [RelayCommand]
        private async Task DevBypassAsync()
        {
            await _mqtt.PublishControlAsync("N"); // "N" 발행
            await Shell.Current.GoToAsync(nameof(Views.DashBoardView));
        }
    }
}

