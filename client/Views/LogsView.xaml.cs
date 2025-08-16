namespace client.Views;

public partial class LogsView : ContentPage
{
	public LogsView(ViewModels.LogsViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}