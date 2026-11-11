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

        internal static void SkipPickupTravel(Toil toil, TargetIndex targetIndex)
        {
            if (toil == null || toil.initAction == null)
                return;

            Action originalInitAction = toil.initAction;
            toil.initAction = delegate
            {
                Pawn pawn = toil.actor;
                if (IsReservedPickup(pawn, targetIndex))
                {
                    pawn.jobs.curDriver.ReadyForNextToil();
                    return;
                }

                originalInitAction();
            };
        }

        internal static void TeleportReservedPickup(Toil toil, TargetIndex targetIndex)
        {
            if (toil == null || toil.initAction == null)
                return;

            Action originalInitAction = toil.initAction;
            toil.initAction = delegate
            {
                Pawn pawn = toil.actor;
                if (IsReservedPickup(pawn, targetIndex))
                {
                    Thing target = pawn.CurJob.GetTarget(targetIndex).Thing;
                    if (target.Position != pawn.Position)
                    {
                        Map map = pawn.Map;
                        target.DeSpawn();
                        GenSpawn.Spawn(target, pawn.Position, map);
                    }
                }

                originalInitAction();
            };
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
            if (__args != null && __args.Length > 0 && __args[0] is TargetIndex targetIndex)
                InstantWorkSupplyUtility.SkipPickupTravel(__result, targetIndex);
        }
    }

    [HarmonyPatch]
    internal static class Toils_Haul_StartCarryThing_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(Toils_Haul))
                .Where(method =>
                    method.Name == nameof(Toils_Haul.StartCarryThing) &&
                    method.ReturnType == typeof(Toil));
        }

        private static void Postfix(object[] __args, ref Toil __result)
        {
            if (__args != null && __args.Length > 0 && __args[0] is TargetIndex targetIndex)
                InstantWorkSupplyUtility.TeleportReservedPickup(__result, targetIndex);
        }
    }

    [HarmonyPatch]
    internal static class Pawn_PathFollower_StartPath_Patch
    {
        private const int MinimumTeleportDistanceSquared = 25;

        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(Pawn_PathFollower))
                .Where(method =>
                {
                    if (method.Name != "StartPath" || method.ReturnType != typeof(void))
                        return false;

                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length >= 2 &&
                        parameters[0].ParameterType == typeof(IntVec3) &&
                        parameters[1].ParameterType == typeof(PathEndMode);
                });
        }

        private static void Postfix(Pawn_PathFollower __instance, Pawn ___pawn)
        {
            Pawn pawn = ___pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return;

            PawnPath path = __instance.curPath;
            if (path == null)
                return;

            Map map = pawn.Map;
            IntVec3 destination = path.LastNode;
            if (!destination.IsValid || !destination.InBounds(map))
                return;

            if (destination.DistanceToSquared(pawn.Position) < MinimumTeleportDistanceSquared)
                return;

            bool wasSelected = Find.Selector.IsSelected(pawn);
            __instance.StopDead();
            Rot4 rotation = pawn.Rotation;
            pawn.DeSpawn();
            GenSpawn.Spawn(pawn, destination, map, rotation);
            pawn.Notify_Teleported();

            if (wasSelected)
                Find.Selector.Select(pawn, false, false);
        }
    }
}
