namespace client.Views
{
    public partial class DashBoardView : ContentPage
    {
        int count = 0;

        private static readonly double[] Steps = new[] { 85d, 170d, 255d };

        public DashBoardView()
        {
            InitializeComponent();
            BindingContext = new ViewModels.MainViewModel();
        }        

        private void OnIntensityChanged(object sender, ValueChangedEventArgs e)
        {
            double nearest = Math.Round(e.NewValue / 85.0) * 85.0;
            if (nearest < 85.0) nearest = 85.0;
            if (nearest > 255.0) nearest = 255.0;

            if (Math.Abs(nearest - e.NewValue) > 0.5)
            {
                ((Slider)sender).Value = nearest;
            }

            if (BindingContext is ViewModels.MainViewModel vm && Math.Abs(vm.Intensity - nearest) > 0.5)
            {
                vm.Intensity = nearest;
            }
        }
    }
}
