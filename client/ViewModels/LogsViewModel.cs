using client.Models;
using client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;

namespace client.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        public ObservableCollection<LogBlock> LogBlocks { get; } = new();


        private readonly MqttService _mqttService = new();
        private readonly DeviceLogService _logService = new();


        [ObservableProperty]
        private int carouselPosition;


        public LogsViewModel()
        {
            _mqttService.LogsReceived += OnLogsReceived;
            _ = _mqttService.ConnectAsync();


            var timer = new System.Timers.Timer(5000);
            timer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (LogBlocks.Count > 1)
                        CarouselPosition = (CarouselPosition + 1) % LogBlocks.Count;
                });
            };
            timer.Start();
        }


        private void OnLogsReceived(Dictionary<string, string> logs)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var entries = logs.Select(x => new LogItem
                {
                    Name = ConvertToKorean(x.Key),
                    Status = x.Value
                }).ToList();


                var top = entries.Take(4).ToList();
                var bottom = entries.Skip(4).Take(4).ToList();


                var block = new LogBlock
                {
                    TopRow = new ObservableCollection<LogItem>(top),
                    BottomRow = new ObservableCollection<LogItem>(bottom),
                    Timestamp = GetKoreaTime()
                };


                LogBlocks.Insert(0, block);
                while (LogBlocks.Count > 150)
                    LogBlocks.RemoveAt(LogBlocks.Count - 1);
            });


            _ = _logService.SaveLogAsync(logs);
        }
        private string GetKoreaTime()
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
            var kst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            return kst.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private string ConvertToKorean(string key) => key switch
        {
            "heater" => "히터",
            "fan" => "냉각팬",
            "O2" => "산소기",
            "filtering" => "여과기",
            "pump1" => "펌프1",
            "pump2" => "펌프2",
            "feed" => "먹이",
            "led" => "LED",
            _ => key
        };
    }
}