﻿namespace client
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("settings", typeof(Views.SettingsView));
        }
    }
}
