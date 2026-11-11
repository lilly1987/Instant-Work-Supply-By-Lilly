using System;
using System.Collections.Generic;
using System.Linq;
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

    [HarmonyPatch]
    internal static class Toils_Goto_GotoThing_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(Toils_Goto))
                .Where(method =>
                {
                    if (method.Name != nameof(Toils_Goto.GotoThing) || method.ReturnType != typeof(Toil))
                        return false;

                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length >= 2 &&
                        parameters[0].ParameterType == typeof(TargetIndex) &&
                        parameters[1].ParameterType == typeof(PathEndMode);
                });
        }

        private static void Postfix(object[] __args, ref Toil __result)
        {
            if (__args == null || __args.Length == 0 || !(__args[0] is TargetIndex targetIndex))
                return;

            InstantWorkSupplyUtility.SkipTravel(
                __result,
                pawn => InstantWorkSupplyUtility.ShouldSkipDestinationTravel(pawn) ||
                    InstantWorkSupplyUtility.IsReservedPickup(pawn, targetIndex));
        }
    }

    [HarmonyPatch]
    internal static class Toils_Goto_GotoCellTarget_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(Toils_Goto))
                .Where(method =>
                {
                    if (method.Name != nameof(Toils_Goto.GotoCell) || method.ReturnType != typeof(Toil))
                        return false;

                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length >= 2 &&
                        parameters[0].ParameterType == typeof(TargetIndex) &&
                        parameters[1].ParameterType == typeof(PathEndMode);
                });
        }

        private static void Postfix(ref Toil __result)
        {
            InstantWorkSupplyUtility.SkipTravel(
                __result,
                pawn => InstantWorkSupplyUtility.ShouldSkipDestinationTravel(pawn));
        }
    }

    [HarmonyPatch]
    internal static class Toils_Goto_GotoCell_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(Toils_Goto))
                .Where(method =>
                {
                    if (method.Name != nameof(Toils_Goto.GotoCell) || method.ReturnType != typeof(Toil))
                        return false;

                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length >= 2 &&
                        parameters[0].ParameterType == typeof(IntVec3) &&
                        parameters[1].ParameterType == typeof(PathEndMode);
                });
        }

        private static void Postfix(ref Toil __result)
        {
            InstantWorkSupplyUtility.SkipTravel(
                __result,
                pawn => InstantWorkSupplyUtility.ShouldSkipDestinationTravel(pawn));
        }
    }

    [HarmonyPatch]
    internal static class Toils_Haul_CarryHauledThingToCell_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(Toils_Haul))
                .Where(method =>
                    method.Name == nameof(Toils_Haul.CarryHauledThingToCell) &&
                    method.ReturnType == typeof(Toil));
        }

        private static void Postfix(ref Toil __result)
        {
            InstantWorkSupplyUtility.SkipTravel(
                __result,
                pawn => InstantWorkSupplyUtility.ShouldSkipDestinationTravel(pawn));
        }
    }

    [HarmonyPatch]
    internal static class Toils_Haul_CarryHauledThingToContainer_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(Toils_Haul))
                .Where(method =>
                    method.Name == nameof(Toils_Haul.CarryHauledThingToContainer) &&
                    method.ReturnType == typeof(Toil));
        }

        private static void Postfix(ref Toil __result)
        {
            InstantWorkSupplyUtility.SkipTravel(
                __result,
                pawn => InstantWorkSupplyUtility.ShouldSkipDestinationTravel(pawn));
        }
    }
}
