using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace MonAppGestion
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Log unhandled exceptions to a file to help debugging when running from the CLI
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                try { File.AppendAllText("crash.log", $"[Unhandled] {DateTime.Now}\n{ev.ExceptionObject}\n\n"); } catch { }
            };

            this.DispatcherUnhandledException += (s, ev) =>
            {
                try { File.AppendAllText("crash.log", $"[Dispatcher] {DateTime.Now}\n{ev.Exception}\n\n"); } catch { }
            };

            base.OnStartup(e);
        }
    }
}