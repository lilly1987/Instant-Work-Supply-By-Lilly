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
    internal static class Pawn_PathFollower_PatherTick_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn_PathFollower), "PatherTick");
        }

        private static bool Prefix(Pawn_PathFollower __instance, Pawn ___pawn)
        {
            Pawn pawn = ___pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return true;

            PawnPath path = __instance.curPath;
            if (path == null)
                return true;

            Map map = pawn.Map;
            IntVec3 destination = path.LastNode;
            if (!destination.IsValid || !destination.InBounds(map))
                return true;

            if (destination == pawn.Position)
                return true;

            __instance.StopDead();
            Rot4 rotation = pawn.Rotation;
            pawn.DeSpawn();
            GenSpawn.Spawn(pawn, destination, map, rotation);
            pawn.Notify_Teleported();

            return false;
        }
    }
}
