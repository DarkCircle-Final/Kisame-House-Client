namespace client.Views;

public partial class SettingsView : ContentPage
{
	public SettingsView(ViewModels.SettingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}