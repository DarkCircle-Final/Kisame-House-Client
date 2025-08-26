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
using Microsoft.Maui.Storage; // FileSystem
using Microsoft.Maui.ApplicationModel.DataTransfer; // Share

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

        // 적용 버튼 로직
        [RelayCommand]
        private async Task ApplySettingsAsync()
        {
            await _mqtt.PublishControlAsync(ConvertHeaterToCode(heaterSelected));
            await Task.Delay(100); // 100~200ms 정도 지연  (아두이노 상에서 확인해보기)
            await _mqtt.PublishControlAsync(ConvertFanToCode(coolingFanSelected));
            await Task.Delay(100);
            await _mqtt.PublishControlAsync(ConvertFilterToCode(filterSelected));
            await Task.Delay(100);
            await _mqtt.PublishControlAsync(ConvertPumpToCode(pumpSelected));
            await Task.Delay(100);

            StartFeederSchedule(feederSelected); // 새 스케줄 시작

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
            _feederCts?.Cancel(); // 기존 타이머 취소
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
                var interval = TimeSpan.FromMinutes(6.0 / repeatCount);
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
                catch (TaskCanceledException)
                {
                    // 타이머 중지됨
                }
            }, _feederCts.Token);
        }

        // --- 시간대 보정 헬퍼들 ---
        private static DateTime ToSeoulLocal(DateTime dt)
        {
            // dt가 UTC 기준이라고 가정하고 KST로 변환
            // 플랫폼별 TimeZoneId 차이를 안전하게 처리
            var utc = dt.Kind == DateTimeKind.Utc
                ? dt
                : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

            try
            {
                // Android/iOS/Linux: "Asia/Seoul"
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
                return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            }
            catch
            {
                try
                {
                    // Windows: "Korea Standard Time"
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
                    return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
                }
                catch
                {
                    // 최후의 보루: 단순 +9h (DST 고려 X)
                    return utc.AddHours(9);
                }
            }
        }

        // CSV 관련
        [RelayCommand]
        private async Task ExportCsvAsync()
        {
            var repo = new SensingRepository("Server=10.0.2.2;Port=3306;Database=kisame;Uid=root;Pwd=12345;SslMode=None;AllowPublicKeyRetrieval=True;");
            var rows = await repo.GetMergedDataAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,gas,humidity,temp,tdsValue,water_temp,ph,heater,fan,O2,filtering,pump1,pump2,feed,led");

            foreach (var row in rows)
            {
                // 원본이 UTC라고 가정하고 KST(+9h)로 변환
                // (UTC가 아닌 값이 들어와도 안전하게 UTC로 간주해 변환)
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

        // 로그아웃 버튼
        [RelayCommand]
        private async Task LogoutAsync()
        {
            await Shell.Current.GoToAsync("//Entry");
        }
    }
}
