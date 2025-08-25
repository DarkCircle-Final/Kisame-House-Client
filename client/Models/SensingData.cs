using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.Models
{
    public class SensingData
    {
        // sensingdate는 DB에서 DEFAULT CURRENT_TIMESTAMP 로 자동 기록됨
        public float? gas { get; set; }
        public float? humidity { get; set; }
        public float? temp { get; set; }
        public float? tdsValue { get; set; }
        public float? water_temp { get; set; }
        public float? ph { get; set; }
    }
}
