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
        // 선택값
        [ObservableProperty] private int autoSwitchTimeSelected = 1;
        [ObservableProperty] private int heaterSelected = 1;
        [ObservableProperty] private int coolingFanSelected = 1;
        [ObservableProperty] private int filterSelected = 1;
        [ObservableProperty] private int pumpSelected = 1;
        [ObservableProperty] private int feederSelected = 1;

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
