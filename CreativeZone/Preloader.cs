using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreativeZone.Utils;
using HarmonyLib;
using Zenject;

namespace CreativeZone
{
    public static class Preloader
    {
        /// <summary>
        /// Invoked by Doorstop
        /// </summary>
        public static void Main()
        {
            try
            {
                Log.Default.PrintLine(DateTime.Now.ToString());
                Log.Default.PrintLine("Loading CreativeZone");

                ServiceInjector.Harmony = new Harmony("CreativeZone");

                LoadGameAssembly();
                PatchGameStartup();

                Log.Default.PrintLine("Startup inject done");
            }
            catch(Exception e)
            {
                Log.Error.PrintLine("Exception during init:");
                Log.Error.PrintLine(e.ToString());
                
            }
        }

        private static void LoadGameAssembly()
        {
            var t = typeof(AfterTheEndKernel);
        }

        private static void PatchGameStartup()
        {
            var method = typeof(SceneContext).GetMethod("Awake");
            ServiceInjector.Harmony.Patch(method, null, new HarmonyMethod(typeof(ServiceInjector).GetMethod(nameof(ServiceInjector.InstallModServices))));
        }
    }
}
