using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Lilly.InstantWorkSupply
{
    [StaticConstructorOnStartup]
    internal static class InstantWorkSupplyStartup
    {
        static InstantWorkSupplyStartup()
        {
            new Harmony("lilly.instantworksupply").PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    internal static class InstantWorkSupplyUtility
    {
        internal static void SkipTravel(Toil toil, Func<Pawn, bool> shouldSkip)
        {
            if (toil == null || toil.initAction == null)
                return;

            Action originalInitAction = toil.initAction;
            toil.initAction = delegate
            {
                Pawn pawn = toil.actor;
                if (pawn != null && shouldSkip(pawn))
                {
                    pawn.jobs.curDriver.ReadyForNextToil();
                    return;
                }

                originalInitAction();
            };
        }

        internal static bool IsReservedPickup(Pawn pawn, TargetIndex targetIndex)
        {
            if (pawn == null || pawn.Map == null || pawn.CurJob == null ||
                pawn.carryTracker.CarriedThing != null)
                return false;

            Thing target = pawn.CurJob.GetTarget(targetIndex).Thing;
            return target != null &&
                !(target is Pawn) &&
                target.Spawned &&
                target.def.EverHaulable &&
                !target.IsForbidden(pawn) &&
                pawn.Map.reservationManager.ReservedBy(target, pawn, pawn.CurJob);
        }

        internal static bool ShouldSkipDestinationTravel(Pawn pawn)
        {
            if (pawn == null || pawn.carryTracker.CarriedThing == null || pawn.CurJob == null)
                return false;

            Job job = pawn.CurJob;
            return !(job.targetA.HasThing && job.targetA.Thing is Pawn) &&
                !(job.targetB.HasThing && job.targetB.Thing is Pawn) &&
                !(job.targetC.HasThing && job.targetC.Thing is Pawn);
        }
    }

    [HarmonyPatch(typeof(Toils_Goto), nameof(Toils_Goto.GotoThing), new[] { typeof(TargetIndex), typeof(PathEndMode) })]
    internal static class Toils_Goto_GotoThing_Patch
    {
        private static void Postfix(TargetIndex ind, ref Toil __result)
        {
            InstantWorkSupplyUtility.SkipTravel(
                __result,
                pawn => InstantWorkSupplyUtility.ShouldSkipDestinationTravel(pawn) ||
                    InstantWorkSupplyUtility.IsReservedPickup(pawn, ind));
        }
    }

    [HarmonyPatch(typeof(Toils_Goto), nameof(Toils_Goto.GotoCell), new[] { typeof(TargetIndex), typeof(PathEndMode) })]
    internal static class Toils_Goto_GotoCellTarget_Patch
    {
        private static void Postfix(ref Toil __result)
        {
            InstantWorkSupplyUtility.SkipTravel(
                __result,
                pawn => InstantWorkSupplyUtility.ShouldSkipDestinationTravel(pawn));
        }
    }

    [HarmonyPatch(typeof(Toils_Goto), nameof(Toils_Goto.GotoCell), new[] { typeof(IntVec3), typeof(PathEndMode) })]
    internal static class Toils_Goto_GotoCell_Patch
    {
        private static void Postfix(ref Toil __result)
        {
            InstantWorkSupplyUtility.SkipTravel(
                __result,
                pawn => InstantWorkSupplyUtility.ShouldSkipDestinationTravel(pawn));
        }
    }

    [HarmonyPatch(typeof(Toils_Haul), nameof(Toils_Haul.CarryHauledThingToCell))]
    internal static class Toils_Haul_CarryHauledThingToCell_Patch
    {
        private static void Postfix(ref Toil __result)
        {
            InstantWorkSupplyUtility.SkipTravel(
                __result,
                pawn => InstantWorkSupplyUtility.ShouldSkipDestinationTravel(pawn));
        }
    }

    [HarmonyPatch(typeof(Toils_Haul), nameof(Toils_Haul.CarryHauledThingToContainer))]
    internal static class Toils_Haul_CarryHauledThingToContainer_Patch
    {
        private static void Postfix(ref Toil __result)
        {
            InstantWorkSupplyUtility.SkipTravel(
                __result,
                pawn => InstantWorkSupplyUtility.ShouldSkipDestinationTravel(pawn));
        }
    }
}
