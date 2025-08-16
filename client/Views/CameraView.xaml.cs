using client.Services;

namespace client.Views;

public partial class CameraView : ContentPage
{
	private readonly IOrientationService _orientation;
    public CameraView(ViewModels.CameraViewModel vm, IOrientationService orientation)
	{
		InitializeComponent();
		BindingContext = vm;
		_orientation = orientation;
    }

    protected override void OnAppearing()
	{
        base.OnAppearing();
        _orientation.LockLandscape(); // ���� ���� ����
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _orientation.UnLock(); // ȭ�� ȸ�� ��� ����
    }
}