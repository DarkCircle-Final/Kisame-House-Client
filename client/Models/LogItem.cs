using Microsoft.Maui.Graphics;


namespace client.Models
{
    public class LogItem
    {
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public Color BackgroundColor => Status == "ON" ? Colors.Red : Colors.LightGray;
    }
}