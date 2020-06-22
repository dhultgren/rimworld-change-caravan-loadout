using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;

namespace ChangeCaravanLoadout.Patches
{
    [HarmonyPatch(typeof(ITab_Pawn_FormingCaravan), "FillTab")]
    public class PatchFillTab
    {
        // TODO: get constructor patching to work instead
        static bool patched = false;
        static void Prefix(ITab_Pawn_FormingCaravan __instance)
        {
            // Make caravan tab slightly bigger to account for the new buttons
            if (patched) return;
            var sizeMethod = __instance.GetType().GetField("size", BindingFlags.NonPublic | BindingFlags.Instance);
            var size = sizeMethod.GetValue(__instance) as Vector2?;
            if (size.HasValue && size.Value.x <= 500)
            {
                sizeMethod.SetValue(__instance, new Vector2(size.Value.x + 50, size.Value.y));
            }
            patched = true;
        }
    }
}
