using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.Services
{
    public class NoOpOrientationService : IOrientationService
    {
        public void LockPortrait() { }      // 세로
        public void LockLandscape() { }     // 가로
        public void UnLock() { }
    }
}
