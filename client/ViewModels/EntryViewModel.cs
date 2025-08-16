using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace client.ViewModels
{
    public class EntryViewModel : BindableObject
    {
        private string _serial = "";
        private string _nickname = "";
        private string _error = "";

        // 시리얼 하드 코딩 -> 추후 교체요망
        private readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            "19960423", "20090523", "15885588"
        };

        public string Serial
        {
            get => _serial;
            set
            {
                if (_serial == value) return;
                _serial = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConfirm));
                (ConfirmCommand as Command)?.ChangeCanExecute(); // 즉시버튼 활성/비활성 갱신
            }
        }
        public string Nickname
        {
            get => _nickname;
            set
            {
                if (_nickname == value) return;
                _nickname = value;
                OnPropertyChanged();
            }
        }
        public string Error
        {
            get => _error;
            set
            {
                if (_error == value) return;
                _error = value;
                OnPropertyChanged();
            }
        }

        public bool CanConfirm => !string.IsNullOrWhiteSpace(Serial);

        public ICommand ConfirmCommand { get; }
        public ICommand DevBypassCommand { get; }


        public EntryViewModel()
        {
            ConfirmCommand = new Command(async () =>
            {
                Error = "";
                var s = Serial?.Trim() ?? "";
                if (_allowed.Contains(s))
                {
                    // 닉네임등 필요시 상태전달방식 요망
                    await Shell.Current.GoToAsync(nameof(Views.DashBoardView));   // 루트전환
                }
                else
                {
                    Error = "허용되지 않은 시리얼입니다. 다시 입력하세요";
                }
            }, () => CanConfirm);

            DevBypassCommand = new Command(async () =>
            {
                await Shell.Current.GoToAsync(nameof(Views.DashBoardView));
            });

            // CanExecute 자동 갱신
            this.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(CanConfirm))
                    (ConfirmCommand as Command)?.ChangeCanExecute();
            };
        }
    }
}
