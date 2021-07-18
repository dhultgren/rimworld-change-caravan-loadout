using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ChangeCaravanLoadout.Patches
{
    public class AbandonButtons
    {
        public static void DrawAbandonButtons(ref Rect rect, ThingDef thingDef, int count, List<Thing> things)
        {
            // Method mostly copied from RimWorld.ITab_ContentsBase.DoThingRow
            if (count != 1 && Widgets.ButtonImage(new Rect(rect.x + rect.width - 24f, rect.y + (rect.height - 24f) / 2f, 24f, 24f), CaravanThingsTabUtility.AbandonSpecificCountButtonTex))
            {
                Find.WindowStack.Add(new Dialog_Slider("RemoveSliderText".Translate(thingDef.label), 1, count, (int x) => OnDropThing(thingDef, x, count)));
            }
            rect.width -= 24f;
            if (Widgets.ButtonImage(new Rect(rect.x + rect.width - 24f, rect.y + (rect.height - 24f) / 2f, 24f, 24f), CaravanThingsTabUtility.AbandonButtonTex))
            {
                string value = thingDef.label;
                if (things.Count == 1 && things[0] is Pawn)
                {
                    value = ((Pawn)things[0]).LabelShortCap;
                }
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmRemoveItemDialog".Translate(value), () => OnDropThing(thingDef, count, count)));
            }
            rect.width -= 24f;
        }

        public static void OnDropThing(ThingDef thingDef, int countToDrop, int totalCount)
        {
            var sendCaravanJob = (Find.Selector.SingleSelectedThing as Pawn)?.GetLord()?.LordJob as LordJob_FormAndSendCaravan;
            if (sendCaravanJob == null)
            {
                Log.Error("Couldn't find send caravan job");
                return;
            }
            var transferableThing = sendCaravanJob.transferables.FirstOrDefault(t => t.ThingDef == thingDef);
            if (transferableThing == null && FindThingHolder(sendCaravanJob, thingDef, totalCount) == null)
            {
                Log.Error("Couldn't find TransferableOneWay for dropped thing and no pawn was carrying it");
                return;
            }

            if (transferableThing?.CountToTransfer >= countToDrop)
            {
                // stop loading the items
                transferableThing.ForceTo(transferableThing.CountToTransfer - countToDrop);
                EndJobForEveryoneHauling(sendCaravanJob, transferableThing);
                ShowDropMessage(transferableThing.Label, countToDrop);
            }
            else
            {
                // drop already loaded items
                Pawn thingHolder = FindThingHolder(sendCaravanJob, thingDef, totalCount);
                var targetThing = thingHolder?.inventory?.innerContainer?.InnerListForReading.FirstOrDefault(t => t.def == thingDef && t.stackCount == totalCount);
                if (targetThing == null)
                {
                    Log.Error("Couldn't find thing to drop");
                    return;
                }
                DropLoadedThing(thingHolder, targetThing, countToDrop);
            }
        }

        private static Pawn FindThingHolder(LordJob_FormAndSendCaravan sendCaravanJob, ThingDef thingDef, int totalCount)
        {
            return sendCaravanJob.lord.ownedPawns
                    .FirstOrDefault(p => p.inventory?.innerContainer?.InnerListForReading.Any(t => t.def == thingDef && t.stackCount == totalCount) == true);
        }

        private static void DropLoadedThing(Pawn thingHolder, Thing thing, int countToDrop)
        {
            var pos = thingHolder?.Position ?? (thing.PositionHeld.IsValid
                ? thing.PositionHeld
                : thing.Position);
            var map = thingHolder?.Map ?? thing.MapHeld ?? thing.Map;
            if (pos == null || !pos.IsValid || map == null)
            {
                Log.Error("Couldn't drop thing " + thing.ToString());
            }
            else
            {
                if (thing.stackCount == countToDrop)
                {
                    thing.holdingOwner.TryDrop(thing, pos, map, ThingPlaceMode.Near, out Thing _);
                }
                else
                {
                    var newThing = thing.SplitOff(countToDrop);
                    GenDrop.TryDropSpawn(newThing, pos, map, ThingPlaceMode.Near, out Thing _);
                }
                ShowDropMessage(thing.LabelNoCount, countToDrop);
            }
        }

        private static void ShowDropMessage(string labelNoCount, int countToDrop)
        {
            // TODO: localization
            Messages.Message($"Removed {labelNoCount}{(countToDrop > 1 ? " x" + countToDrop : "")} from caravan", MessageTypeDefOf.NeutralEvent);
        }

        private static void EndJobForEveryoneHauling(LordJob_FormAndSendCaravan sendCaravanJob, TransferableOneWay t)
        {
            var haulingPawns = sendCaravanJob.lord.ownedPawns
                .Where(p => p.CurJobDef == JobDefOf.PrepareCaravan_GatherItems);

            foreach (var pawn in haulingPawns)
            {
                var job = (JobDriver_PrepareCaravan_GatherItems)pawn.jobs.curDriver;
                if (job.ToHaul?.def == t.ThingDef)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }
        }
    }
}
