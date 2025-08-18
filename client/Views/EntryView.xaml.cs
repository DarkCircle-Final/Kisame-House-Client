using client.Services;

namespace client.Views;

public partial class EntryView : ContentPage
{
	private readonly IOrientationService _orientation;
	public EntryView(ViewModels.EntryViewModel vm, IOrientationService orientation)
	{
		InitializeComponent();
		BindingContext = vm;
		_orientation = orientation;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _orientation.LockPortrait();
    }

    protected override void OnDisappearing()
    {
        _orientation.UnLock();
        base.OnDisappearing();
    }
}
