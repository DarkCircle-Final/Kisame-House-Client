using client.Services;

namespace client
{
    public partial class App : Application
    {
        // 전역 MQTT 인스턴스
        public static MqttService Mqtt { get; private set; } = new();

        public App()
        {
            InitializeComponent();
            ConnectMqtt(); // 앱 시작 시 MQTT 연결
        }

        private async void ConnectMqtt()
        {
            try
            {
                await Mqtt.ConnectAsync();
                Console.WriteLine("[MQTT] Connected at startup.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MQTT] 연결 실패: {ex.Message}");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
