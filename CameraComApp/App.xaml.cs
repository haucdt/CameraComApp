using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CameraComApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application

    {
        private Mutex _mutex;
        protected override void OnStartup(StartupEventArgs e)
        {
            bool isNewInstance;

            isNewInstance = SingleInstanceManager.Initialize("Camera and COM/Socket App");
            if (isNewInstance)
            {

                base.OnStartup(e);
            }
            else
            {
                Shutdown();
            }
           
        }


    }
}
