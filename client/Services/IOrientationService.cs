using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.Services
{
    public interface IOrientationService
    {
        // 화면 회전 잠금
        void LockPortrait();    // 세로
        void LockLandscape();   //가로
        void UnLock();
    }
}
