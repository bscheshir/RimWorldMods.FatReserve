using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;

namespace FatReserve
{
    [StaticConstructorOnStartup]
    public static class HarmonyInitializer
    {
        static HarmonyInitializer()
        {
            var harmony = new Harmony("com.BSCheshir.FatReserve");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("FatReserve_LogCoreInit".Translate());
        }
    }

    [StaticConstructorOnStartup]
    public static class FatReserveAssets
    {
        public static readonly Texture2D IconFatReserveOn = ContentFinder<Texture2D>.Get("UI/Icons/FatReserve_Gizmo_On", true);
        public static readonly Texture2D IconFatReserveOff = ContentFinder<Texture2D>.Get("UI/Icons/FatReserve_Gizmo_Off", true);
        public static readonly Texture2D IconSubmenuMouse = ContentFinder<Texture2D>.Get("UI/Icons/rightClickOverlay", false);
    }
}