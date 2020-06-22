using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ChangeCaravanLoadout.Patches
{
    [HarmonyPatch(typeof(ITab_Pawn_FormingCaravan), "DoThingRow")]
    public class PatchDoThingRow
    {
        static MethodInfo drawButtons = typeof(AbandonButtons).GetMethod("DrawAbandonButtons");

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Add call to DrawAbandonButtons right after the first Rect constructor call
            var found = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (!found && instruction.opcode == OpCodes.Call && instruction.ToString().Contains("Rect::.ctor"))
                {
                    // load variables and call DrawAbandonButtons
                    // public static void DrawAbandonButtons(ref Rect rect, ThingDef thingDef, int count, List<Thing> things)
                    yield return new CodeInstruction(OpCodes.Ldloca, 0); // ref rect
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // thingDef
                    yield return new CodeInstruction(OpCodes.Ldarg_2); // count
                    yield return new CodeInstruction(OpCodes.Ldarg_3); // things
                    yield return new CodeInstruction(OpCodes.Call, drawButtons); // call method

                    found = true;
                }
            }
            if (found == false)
            {
                Log.Error("Cannot find call to UnityEngine.Rect constructor in ITab_Pawn_FormingCaravan.DoThingRow");
            }
        }
    }
}
