using RimWorld;
using Verse;

namespace Wizardry;

public static class UtilityWizardry
{
    public static bool IsIstari(this Pawn pawn)
    {
        return pawn?.story?.traits?.HasTrait(TraitDef.Named("LotRW_Istari")) ?? false;
    }

    public static bool IsMage(this Pawn pawn)
    {
        return pawn?.story?.traits?.HasTrait(TraitDef.Named("LotRW_MagicAttuned")) ?? false;
    }
}