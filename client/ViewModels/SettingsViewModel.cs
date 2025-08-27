// SettingsViewModel.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using client.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using client.Helps; // ✅ 추가된 using

namespace client.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly MqttService _mqtt = App.Mqtt;

        [ObservableProperty] private int autoSwitchTimeSelected = 1;
        [ObservableProperty] private int heaterSelected = 1;
        [ObservableProperty] private int coolingFanSelected = 1;
        [ObservableProperty] private int filterSelected = 1;
        [ObservableProperty] private int pumpSelected = 1;
        [ObservableProperty] private int feederSelected = 1;

        [RelayCommand]
        private async Task ApplySettingsAsync()
        {
            // ✅ 자동전환 시간 설정 저장
            AppSettings.AutoSwitchSeconds = AutoSwitchTimeSelected switch
            {
                1 => 5,
                2 => 10,
                3 => 15,
                _ => 5
            };

            await _mqtt.PublishControlAsync(ConvertHeaterToCode(heaterSelected));
            await Task.Delay(100);
            await _mqtt.PublishControlAsync(ConvertFanToCode(coolingFanSelected));
            await Task.Delay(100);
            await _mqtt.PublishControlAsync(ConvertFilterToCode(filterSelected));
            await Task.Delay(100);
            await _mqtt.PublishControlAsync(ConvertPumpToCode(pumpSelected));
            await Task.Delay(100);

            StartFeederSchedule(feederSelected);

            await Shell.Current.DisplayAlert("적용 완료", "설정이 성공적으로 적용되었습니다.", "확인");
        }

        private string ConvertHeaterToCode(int selected) => selected switch
        {
            1 => "O",
            2 => "P",
            3 => "Q",
            _ => ""
        };

        private string ConvertFanToCode(int selected) => selected switch
        {
            1 => "R",
            2 => "S",
            3 => "T",
            _ => ""
        };

        private string ConvertFilterToCode(int selected) => selected switch
        {
            1 => "U",
            2 => "V",
            _ => ""
        };

        private string ConvertPumpToCode(int selected) => selected switch
        {
            1 => "W",
            2 => "X",
            _ => ""
        };

        private CancellationTokenSource? _feederCts;

        private void StartFeederSchedule(int selected)
        {
            _feederCts?.Cancel();
            _feederCts = new CancellationTokenSource();

            int repeatCount = selected switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                _ => 0
            };

            if (repeatCount == 0)
                return;

            _ = Task.Run(async () =>
            {
                var interval = TimeSpan.FromMinutes(1.0 / repeatCount);
                var token = _feederCts.Token;

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await _mqtt.PublishControlAsync("w");
                        await _mqtt.PublishControlAsync("m");

                        await Task.Delay(interval, token);
                    }
                }
                catch (TaskCanceledException) { }
            }, _feederCts.Token);
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await Shell.Current.GoToAsync("//Entry");
        }
    }
}
