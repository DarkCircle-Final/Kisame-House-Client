namespace client.Views;

public partial class EntryView : ContentPage
{
	public EntryView(ViewModels.EntryViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}