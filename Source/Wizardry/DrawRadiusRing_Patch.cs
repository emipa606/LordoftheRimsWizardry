using HarmonyLib;
using Verse;

namespace Wizardry;

[HarmonyPatch(typeof(GenDraw), "DrawRadiusRing", typeof(IntVec3), typeof(float))]
public class DrawRadiusRing_Patch
{
    public static bool Prefix(IntVec3 center, float radius)
    {
        return !(radius > GenRadial.MaxRadialPatternRadius);
    }
}