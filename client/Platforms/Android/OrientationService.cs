#if ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content.PM;
using Microsoft.Maui.ApplicationModel;

namespace client.Services
{
    public class OrientationService : IOrientationService
    {
        public void LockPortrait() => Platform.CurrentActivity!.RequestedOrientation = ScreenOrientation.Portrait;      // 세로
        public void LockLandscape() => Platform.CurrentActivity!.RequestedOrientation = ScreenOrientation.Landscape;    // 가로
        public void UnLock() => Platform.CurrentActivity!.RequestedOrientation = ScreenOrientation.Unspecified;
    }
}
#endif