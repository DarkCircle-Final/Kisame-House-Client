using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace client.ViewModels
{
    public class CameraViewModel : BindableObject
    {
        // 뒤로가기 버튼 메서드
        public ICommand GoBackCommand { get; }
        public CameraViewModel()
        {
            GoBackCommand = new Command(async () => 
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
            });
        }
        
        // CameraViewModel은 카메라 관련 기능을 담당
    }
}
