using System.Collections.ObjectModel;


namespace client.Models
{
    public class LogBlock
    {
        public ObservableCollection<LogItem> TopRow { get; set; } = new();
        public ObservableCollection<LogItem> BottomRow { get; set; } = new();
        public string Timestamp { get; set; } = "";
    }
}
