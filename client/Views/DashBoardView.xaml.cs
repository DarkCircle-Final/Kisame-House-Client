using client.Services;

namespace client.Views
{
    public partial class DashBoardView : ContentPage
    {
        private readonly IOrientationService _orientation;
        public DashBoardView(ViewModels.DashBoardViewModel vm, IOrientationService orientation)
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
        private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
        {
            var slider = (Slider)sender;
            double rawValue = e.NewValue;

            // 단계별 값
            double[] steps = { 85, 170, 255 };
            double closest = steps.OrderBy(s => Math.Abs(s - rawValue)).First();

            slider.Value = closest;
        }

    }
}
