using System.Windows;
using GalaSoft.MvvmLight.Threading;
using System.Windows.Controls;
using System.IO;
using System;
using System.Configuration;
using DecoraCsycles.HelperClasses;
using System.Diagnostics;
using System.Linq;

namespace DecoraCsycles
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string StorageLocation { get; set; }
        public static string TempStorageLocation { get; set; }
        public static Frame RootFrame { get; set; }

        static App()
        {
            DispatcherHelper.Initialize();

           StorageLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ConfigurationManager.AppSettings["DataFolder"] ?? "Data2");
           TempStorageLocation = Path.Combine(Path.GetTempPath(), ConfigurationManager.AppSettings["TempFolder"] ?? "Decora");

           if (!Directory.Exists(StorageLocation))
               Directory.CreateDirectory(StorageLocation);

           try
           {
               if (Directory.Exists(TempStorageLocation))
                   Directory.Delete(TempStorageLocation, true);
           }
           catch { }

           Directory.CreateDirectory(TempStorageLocation);

           //if( Process.GetProcesses().FirstOrDefault(p=>p.ProcessName == "SunBurn-DojoExample") == null)
           //         Process.Start(@"F:\Project Cycles\SVN\trunk\Cycle test\Dojo-Framework-Example\bin\x86\Debug\SunBurn-DojoExample.exe");

        }


        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorReporting.reportError(e.Exception, "UnHandleException");
        }


    }
}
