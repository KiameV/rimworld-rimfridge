using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SaveStorageSettingsUtil
{
    public static class SaveStorageSettingsGizmoUtil
    {
        private static Assembly saveStateAssembly = null;
        private static bool initialized = false;
        public static bool Exists
        {
            get
            {
                if (!initialized)
                {
                    foreach (ModContentPack pack in LoadedModManager.RunningMods)
                    {
                        foreach (Assembly assembly in pack.assemblies.loadedAssemblies)
                        {
                            if (assembly.GetName().Name.Equals("SaveStorageSettings") &&
                                assembly.GetType("SaveStorageSettings.GizmoUtil") != null)
                            {
                                initialized = true;
                                saveStateAssembly = assembly;
                                break;
                            }
                        }
                        if (initialized)
                        {
                            break;
                        }
                    }
                    initialized = true;
                }
                return saveStateAssembly != null;
            }
        }

        public static IEnumerable<Gizmo> AddSaveLoadGizmos(IEnumerable<Gizmo> gizmos, SaveTypeEnum saveTypeEnum, ThingFilter thingFilter, int groupKey = 987767552)
        {
            return AddSaveLoadGizmos(gizmos, saveTypeEnum.ToString(), thingFilter);
        }

        public static List<Gizmo> AddSaveLoadGizmos(List<Gizmo> gizmos, SaveTypeEnum saveTypeEnum, ThingFilter thingFilter, int groupKey = 987767552)
        {
            return AddSaveLoadGizmos(gizmos, saveTypeEnum.ToString(), thingFilter);
        }

        public static IEnumerable<Gizmo> AddSaveLoadGizmos(IEnumerable<Gizmo> gizmos, string storageTypeName, ThingFilter thingFilter, int groupKey = 987767552)
        {
            List<Gizmo> l = gizmos != null ? new List<Gizmo>(gizmos) : new List<Gizmo>(2);
            return AddSaveLoadGizmos(l, storageTypeName, thingFilter);
        }

        public static List<Gizmo> AddSaveLoadGizmos(List<Gizmo> gizmos, string storageTypeName, ThingFilter thingFilter, int groupKey = 987767552)
        {
            try
            {
                if (Exists)
                {
                    saveStateAssembly.GetType("SaveStorageSettings.GizmoUtil").GetMethod("AddSaveLoadGizmos", BindingFlags.Static | BindingFlags.Public).Invoke(
                        null, parameters: new object[] { gizmos, storageTypeName, thingFilter, groupKey });
                }
            }
            catch(Exception e)
            {
                // Do nothing
                Log.Warning(e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
            }
            return gizmos;
        }
    }
}
