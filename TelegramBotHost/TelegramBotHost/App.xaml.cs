using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TelegramBotHost.Services;
using TelegramBotHost.Views;

namespace TelegramBotHost
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
