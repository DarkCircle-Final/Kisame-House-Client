using client.Services;

namespace client.Views;

public partial class SettingsView : ContentPage
{
	private readonly IOrientationService _orientation;
    public SettingsView(ViewModels.SettingsViewModel vm, IOrientationService orientation)
	{
		InitializeComponent();
		BindingContext = vm;
        _orientation = orientation;
    }

	protected override void OnAppearing()
    {
        base.OnAppearing();
        _orientation.LockPortrait(); // ���Խ� ���� ���� ����
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _orientation.UnLock(); // ���� �� ȸ�� ��� ����
    }
}