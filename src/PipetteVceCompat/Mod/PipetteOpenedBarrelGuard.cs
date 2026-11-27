using System;
using System.Collections.Generic;
using HarmonyLib;

namespace PipetteVceCompat
{
    /// <summary>
    /// Vanilla Chemistry Expansion turns a nuclear waste barrel into \`nc.opened_waste\`,
    /// a liquid container whose contents must not leave (Barrel.CanLeave), enforced by
    /// hooking WaterHelper.TransferLiquid (BarrelPourPatch). The pipette never calls
    /// WaterHelper.TransferLiquid (PenPipette.PipetteTransfer rewrites the liquid tags
    /// itself), so without this guard you could suck spent fuel, dissolved plutonium and
    /// fission products out of an opened barrel. This prefix/postfix pair snapshots the
    /// blocked parts before a pipette draw and puts them back afterwards. Pouring
    /// reagents *into* the barrel is untouched.
    /// </summary>
    internal static class OpenedBarrelGuard
    {
        internal const string OpenedBarrelId = "nc.opened_waste";
        private const string PuExtractId = "nc_pu_extract";

        private static readonly string[] RadioactiveIds =
        {
            "nc_spent_fuel",
            "nc_pu_dissolved",
            "nc_fission_products"
        };

        private static Type _pipetteVisualType;
        private static Type _hydraModeType;

        private sealed class State
        {
            public GameItem pipette;
            public GameItem barrel;
            public readonly List<string> parts = new List<string>();
            public readonly List<int> barrelBefore = new List<int>();
            public readonly List<int> pipetteBefore = new List<int>();
        }

        private static void Prefix(object[] __args, out State __state)
        {
            __state = null;
            try
            {
                if (__args == null || __args.Length < 2)
                {
                    return;
                }
                var pipette = __args[0] as GameItem;
                var target = __args[1] as GameItem;
                if (pipette == null || target == null || !IsOpenedBarrel(target))
                {
                    return;
                }
                // Dragging a non-empty pipette onto the barrel is a legal pour into it.
                if (!WaterHelper.IsEmpty(pipette))
                {
                    return;
                }
                var liquids = Liquid.Liquids;
                var state = new State { pipette = pipette, barrel = target };
                for (var i = 0; i < liquids.Count; i++)
                {
                    var liquid = liquids[i];
                    if (liquid == null || CanLeave(liquid))
                    {
                        continue;
                    }
                    state.parts.Add(liquid.currentPart);
                    state.barrelBefore.Add(target.GetTagReadonly(liquid.currentPart).GetInt());
                    state.pipetteBefore.Add(pipette.GetTagReadonly(liquid.currentPart).GetInt());
                }
                if (state.parts.Count > 0)
                {
                    __state = state;
                }
            }
            catch (Exception e)
            {
                Log.Error("opened-barrel guard prefix failed", e);
                __state = null;
            }
        }

        private static void Postfix(State __state)
        {
            if (__state == null)
            {
                return;
            }
            try
            {
                var moved = 0;
                for (var i = 0; i < __state.parts.Count; i++)
                {
                    var part = __state.parts[i];
                    var pipetteNow = __state.pipette.GetTagReadonly(part).GetInt();
                    if (pipetteNow != __state.pipetteBefore[i])
                    {
                        moved++;
                    }
                    SetInt(__state.pipette, part, __state.pipetteBefore[i]);
                    SetInt(__state.barrel, part, __state.barrelBefore[i]);
                }
                if (moved > 0)
                {
                    SpriteHelper.UpdateWaterContainerSprite(__state.barrel);
                    WaterFeatureHelper.OnLiquidContainerPourOutOf(__state.barrel);
                    RefreshPipetteAfterRestore(__state.pipette);
                    Log.Info("blocked " + moved + " VCE liquid(s) from leaving the opened nuclear waste barrel through a pipette");
                }
            }
            catch (Exception e)
            {
                Log.Error("opened-barrel guard postfix failed", e);
            }
        }

        /// <summary>
        /// After putting the blocked liquids back the pipette can be empty again while the
        /// pipette mod already refreshed its sprite and hydra mode tag, so ask it to refresh
        /// once more (cross-mod call through ModReflect, silently skipped on mismatch).
        /// </summary>
        private static void RefreshPipetteAfterRestore(GameItem pipette)
        {
            if (pipette == null)
            {
                return;
            }
            try
            {
                if (_pipetteVisualType == null)
                {
                    _pipetteVisualType = ModReflect.FindType("PenPipette.PipetteVisual");
                }
                if (_pipetteVisualType != null)
                {
                    ModReflect.CallStatic(_pipetteVisualType, "Refresh", pipette);
                }
            }
            catch
            {
            }
            try
            {
                if (_hydraModeType == null)
                {
                    _hydraModeType = ModReflect.FindType("PenPipette.HydraMode");
                }
                if (_hydraModeType != null)
                {
                    ModReflect.CallStatic(_hydraModeType, "SyncIfEmpty", pipette);
                }
            }
            catch
            {
            }
        }

        private static void SetInt(GameItem item, string part, int value)
        {
            item.ModifyTag(part, state =>
            {
                state.Enable().SetInt(value);
            }, skipValidate: true);
        }

        private static bool IsOpenedBarrel(GameItem item)
        {
            try
            {
                return item != null && string.Equals(item.identifier, OpenedBarrelId, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Mirror of VanillaChemExt.Barrel.CanLeave.</summary>
        private static bool CanLeave(Liquid liquid)
        {
            if (liquid == null)
            {
                return true;
            }
            if (string.Equals(liquid.identifier, PuExtractId, StringComparison.Ordinal))
            {
                return true;
            }
            if (liquid.isEvaporative)
            {
                for (var i = 0; i < RadioactiveIds.Length; i++)
                {
                    if (string.Equals(RadioactiveIds[i], liquid.identifier, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}