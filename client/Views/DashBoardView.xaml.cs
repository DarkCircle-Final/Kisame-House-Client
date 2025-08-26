using client.Services;
using client.ViewModels;

namespace client.Views
{
    public partial class DashBoardView : ContentPage
    {
        private readonly IOrientationService _orientation;
        public DashBoardView(DashBoardViewModel vm, IOrientationService orientation)
        {
            InitializeComponent();
            BindingContext = vm;
            _orientation = orientation;
        }

        // 화면 회전 잠금(세로 유지)
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _orientation.LockPortrait(); // 진입시 세로 모드로 고정
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _orientation.UnLock(); // 나갈 때 회전 잠금 해제
        }

        // LED 라디오버튼 선택 이벤트 핸들러
        private void OnLedRadioChecked(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return; // 체크 해제 이벤트 무시
            if (BindingContext is not DashBoardViewModel vm) return;

            if (sender is RadioButton rb)
            {
                var color = rb.Content?.ToString();
                if (!string.IsNullOrWhiteSpace(color))
                    vm.LedSelectCommand.Execute(color);
            }
        }
    }
}


