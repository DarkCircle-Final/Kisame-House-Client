using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.Models
{
    public class Metric
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public Color TextColor { get; set; } = Colors.Black; // 추가됨
    }


    public class MetricRow
    {
        public string LeftName { get; set; } = "";
        public string LeftValue { get; set; } = "";
        public string RightName { get; set; } = "";
        public string RightValue { get; set; } = "";

        // 추가 (색상 적용용)
        public Color LeftTextColor { get; set; } = Colors.Black;
        public Color RightTextColor { get; set; } = Colors.Black;
    }
}

