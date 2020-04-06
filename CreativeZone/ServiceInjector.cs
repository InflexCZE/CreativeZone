using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CreativeZone.Utils;
using ECS;
using HarmonyLib;
using Service;
using Service.Building;
using Service.Localization;
using Service.Street;
using Service.TaskService2;
using Service.UserWorldTasks;
using Zenject;

namespace CreativeZone
{
    public class ServiceInjector
    {
        private static DiContainer Core => global::Kernel.Instance.Container;
        internal static Harmony Harmony;

        private static bool Installed = false;
        private static bool WaitForDebugger = false;

        public static void InstallModServices()
        {
            try
            {
                InstallModServicesImpl();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        public static void InstallModServicesImpl()
        {
            if(Installed)
                return;

            Installed = true;

            while(WaitForDebugger && Debugger.IsAttached == false)
            { }

            try
            {
                using(Log.Debug.OpenScope("Installing services", string.Empty))
                {
                    Install<IEntityManager, CreativeEntityManager>();
                    Install<IStreetService, CreativeStreetService>();
                    Install<IBuildingService, CreativeBuildingService>();
                    Install<ILocalizationService, CreativeLocalizationService>();
                    Install<IUserWorldTasksService, CreativeUserWorldTaskService>();
                }

                Log.Default.PrintLine("Install completed");
            }
            catch(Exception e)
            {
                Log.Error.PrintLine("Exception during service install:");
                Log.Error.PrintLine(e.ToString());
                throw;
            }
        }

        private static void Install<TService, TImpl>() 
        {
            using (Log.Debug.OpenScope($"Installing {typeof(TImpl)} -> {typeof(TService)} ... ", "Done"))
            {
                var vanilla = Core.Resolve<TService>();
                var installMethod = typeof(TImpl).GetMethod("Install", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                installMethod.Invoke(null, new object[] { Harmony, vanilla, typeof(TService) });
            }
        }
    }
}
