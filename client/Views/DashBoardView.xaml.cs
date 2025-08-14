namespace client.Views
{
    public partial class DashBoardView : ContentPage
    {
        public DashBoardView(ViewModels.MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
