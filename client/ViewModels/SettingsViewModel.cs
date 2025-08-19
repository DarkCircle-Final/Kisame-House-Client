using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace client.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        // 선택값
        [ObservableProperty] private int autoSwitchTimeSelected = 1;
        [ObservableProperty] private int heaterSelected = 1;
        [ObservableProperty] private int coolingFanSelected = 1;
        [ObservableProperty] private int filterSelected = 1;
        [ObservableProperty] private int pumpSelected = 1;
        [ObservableProperty] private int feederSelected = 1;


        // CSV 관련
        [RelayCommand]
        private Task ExportCsvAsync()
        {
            // CSV 파일로 내보내는 로직을 구현
            return Task.CompletedTask;
        }

        // 로그아웃 버튼

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await Shell.Current.GoToAsync("//Entry");
        }
    }
}
