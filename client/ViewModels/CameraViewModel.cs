using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace client.ViewModels
{
    public partial class CameraViewModel : ObservableObject
    {
        [ObservableProperty] private bool isBuffering;

        // MediaMTX 또는 카메라 RTSP 주소 (UDP 기본)
        // 예: rtsp://<Pi-IP>:8554/mystream
        [ObservableProperty] private string streamUrl = "rtsp://<아이피주소>:8554/mystream";

        // 뒤로가기 버튼 메서드
        [RelayCommand]
        private async Task GoBack()
        {
            try
            {
                if (Shell.Current?.Navigation?.NavigationStack?.Count > 1)
                    await Shell.Current.Navigation.PopAsync();
                else
                    await Shell.Current.GoToAsync("..");
            }
            catch
            {
                await Shell.Current.GoToAsync("..");
            }
        }

        
        // CameraViewModel은 카메라 관련 기능을 담당
    }
}
