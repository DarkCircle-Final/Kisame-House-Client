namespace client
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            
            Routing.RegisterRoute(nameof(Views.DashBoardView), typeof(Views.DashBoardView));
            Routing.RegisterRoute("settings", typeof(Views.SettingsView));
            Routing.RegisterRoute("logs", typeof(Views.LogsView));
            Routing.RegisterRoute("camera", typeof(Views.CameraView));
        }
    }
}
