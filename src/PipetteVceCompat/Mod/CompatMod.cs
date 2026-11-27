using System;
using HarmonyLib;

namespace PipetteVceCompat
{
    /// <summary>
    /// Carrier mod for the pipette x Vanilla Chem Ext compatibility patch.
    /// The carrier DLL is required because the game's ModLoader only loads
    /// assembly-bearing mods; all rule data lives in PipetteReactions.json next to it.
    ///
    /// The opened-barrel guard needs to patch PenPipette.PipetteTransfer.OnTarget through
    /// ModReflect, and ModReflect resolves the calling mod from ModLoader.Mods. But the
    /// loader calls IMod.OnEnable BEFORE it registers the mod in ModLoader.Mods, so at
    /// OnEnable time our own mod id is not resolvable yet and ModReflect refuses the
    /// cross-mod lookup. We therefore retry on the first game hook that fires after all
    /// mods are registered (assets loaded / save loaded / new game started).
    /// </summary>
    public sealed class CompatMod : IMod
    {
        internal const string HarmonyId = "pipette_vce_compat";
        internal const string ModId = "pipette_vce_compat";

        private static bool hooksAttached;
        private static bool warnedNotReady;

        public void Init(ModManifest manifest)
        {
            Log.Info("init " + manifest.ID + " v" + manifest.ModVersion);
        }

        public void OnEnable()
        {
            try
            {
                if (Patches.Apply())
                {
                    Log.Info("enabled");
                    return;
                }
                // Startup path: the mod list is not registered yet, wait for a later hook.
                AttachHooks();
                Log.Info("enabled (guard will hook after the game finishes loading mods)");
            }
            catch (Exception e)
            {
                Log.Error("OnEnable failed", e);
            }
        }

        public void OnDisable()
        {
            try
            {
                DetachHooks();
                new Harmony(HarmonyId).UnpatchAll(HarmonyId);
                Patches.Reset();
                Log.Info("disabled");
            }
            catch (Exception e)
            {
                Log.Error("OnDisable failed", e);
            }
        }

        private static void AttachHooks()
        {
            if (hooksAttached)
            {
                return;
            }
            hooksAttached = true;
            ModHook.OnModAssetsLoaded += OnModAssetsLoaded;
            ModHook.OnGameLoadedInit += OnGameReady;
            ModHook.OnNewGameStarted += OnNewGameStarted;
        }

        private static void DetachHooks()
        {
            if (!hooksAttached)
            {
                return;
            }
            hooksAttached = false;
            ModHook.OnModAssetsLoaded -= OnModAssetsLoaded;
            ModHook.OnGameLoadedInit -= OnGameReady;
            ModHook.OnNewGameStarted -= OnNewGameStarted;
        }

        private static void OnModAssetsLoaded(LoadedMod mod)
        {
            TryReady();
        }

        private static void OnGameReady()
        {
            TryReady();
        }

        private static void OnNewGameStarted(string scenarioId)
        {
            TryReady();
        }

        private static void TryReady()
        {
            try
            {
                if (Patches.Applied)
                {
                    DetachHooks();
                    return;
                }
                if (Patches.Apply())
                {
                    DetachHooks();
                    Log.Info("enabled");
                }
                else if (!warnedNotReady)
                {
                    warnedNotReady = true;
                    Log.Warn("Pen_mod_pipette not resolvable yet; opened-barrel guard is inactive. The rules still work.");
                }
            }
            catch (Exception e)
            {
                Log.Error("deferred patch failed", e);
            }
        }
    }

    internal static class Patches
    {
        internal static bool Applied { get; private set; }

        internal static void Reset()
        {
            Applied = false;
        }

        /// <returns>true when the guard is in place (or there is nothing to patch).</returns>
        internal static bool Apply()
        {
            if (Applied)
            {
                return true;
            }
            var harmony = new Harmony(CompatMod.HarmonyId);

            // Cross-mod reflection goes through ModReflect (the target mod is listed in
            // <Prerequisites>, no capability needed). ModReflect only works once this mod
            // is registered in ModLoader.Mods, hence the deferred retry in CompatMod.
            Type type;
            try
            {
                type = ModReflect.FindType("PenPipette.PipetteTransfer");
            }
            catch (Exception e)
            {
                Log.Warn("ModReflect lookup failed: " + e.Message);
                return false;
            }
            if (type == null)
            {
                return false;
            }

            var target = ModReflect.Target(type, "OnTarget", new[] { typeof(GameItem), typeof(GameItem) });
            harmony.Patch(
                target,
                new HarmonyMethod(AccessTools.Method(typeof(OpenedBarrelGuard), "Prefix")),
                new HarmonyMethod(AccessTools.Method(typeof(OpenedBarrelGuard), "Postfix")));
            Applied = true;
            Log.Info("hooked " + type.FullName + ".OnTarget (opened-barrel containment)");
            return true;
        }
    }

    internal static class Log
    {
        private const string Tag = "[pipette-vce-compat] ";

        public static void Info(string message)
        {
            UnityEngine.Debug.Log(Tag + message);
        }

        public static void Warn(string message)
        {
            UnityEngine.Debug.LogWarning(Tag + message);
        }

        public static void Error(string message, Exception e)
        {
            UnityEngine.Debug.LogError(Tag + message + " :: " + (e == null ? "?" : e.ToString()));
        }
    }
}
