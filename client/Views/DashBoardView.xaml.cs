namespace client.Views
{
    public partial class DashBoardView : ContentPage
    {
        public DashBoardView(ViewModels.DashBoardViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
