using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.Models
{
    /// <summary>
    /// 물고기 개체 정보 Model
    /// </summary>
    public class TrackingData
    {
        public int? cls { get; set; }

        public int? track_id { get; set; }

        public List<float>? bbox { get; set; }

        public float? confidence { get; set; }
    }
}
