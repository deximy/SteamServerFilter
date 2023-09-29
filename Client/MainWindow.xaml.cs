using EmbedIO;
using EmbedIO.Files;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView2Async();
        }

        private async void InitializeWebView2Async()
        {
            var web_view_runtime_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Microsoft.WebView2.Runtime");
            var env = await CoreWebView2Environment.CreateAsync(web_view_runtime_path);
            await web_view.EnsureCoreWebView2Async(env);
#if DEBUG
            InitializeDebugUIAsync();
#else
            InitializeReleaseUIAsync();
#endif
            web_view.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        }

#if DEBUG
        private void InitializeDebugUIAsync()
        {
            web_view.CoreWebView2.Navigate("http://localhost:5173/");
        }
#else
        private void InitializeReleaseUIAsync()
        {
            new WebServer(o => o.WithUrlPrefix("http://localhost:5173/").WithMode(HttpListenerMode.EmbedIO))
                .WithStaticFolder("/", "www", true, m => m.WithContentCaching())
                .RunAsync();
            web_view.CoreWebView2.Navigate("http://localhost:5173/");
        }
#endif

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            
        }
    }
}
