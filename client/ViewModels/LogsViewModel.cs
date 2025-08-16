using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace client.ViewModels
{
    public class LogItem
    {
        public string Timestamp { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class LogsViewModel : BindableObject
    {
        public ObservableCollection<LogItem> Items { get; } = new();

        public ICommand RefreshCommand => new Command(Load);


        public bool HasNoItems => Items.Count == 0;
        public string EmptyMessage => "표시할 로그가 없습니다 \n\n";

        private void Load()
        {
            Items.Clear();
            // 실제 데이터 교체 요망
            Items.Add(new LogItem { Timestamp = DateTime.Now.ToString("HH:mm:ss"), Message = "시스템 시작" });
            Items.Add(new LogItem { Timestamp = DateTime.Now.AddMinutes(-5).ToString("HH:mm:ss"), Message = "센서 값 수집" });

            OnPropertyChanged(nameof(HasNoItems));
        }
    }
}
