using HarmonyLib;
using Verse;
using Verse.AI;

namespace Wizardry;

[HarmonyPatch(typeof(Verb), "TryFindShootLineFromTo", null)]
public static class TryFindShootLineFromTo_Base_Patch
{
    public static bool Prefix(Verb __instance, IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine,
        ref bool __result)
    {
        if (__instance.verbProps.IsMeleeAttack)
        {
            resultingLine = new ShootLine(root, targ.Cell);
            __result = ReachabilityImmediate.CanReachImmediate(root, targ, __instance.caster.Map, PathEndMode.Touch,
                null);
            return false;
        }

        if (__instance.verbProps.verbClass.ToString() == "Wizardry.Verb_BLOS")
        {
            //Ignores line of sight
            resultingLine = new ShootLine(root, targ.Cell);
            __result = true;
            return false;
        }

        resultingLine = default;
        __result = true;
        return true;
    }
}