using AbilityUser;
using Verse;

namespace Wizardry;

internal class Verb_BLOS : Verb_UseAbility
{
    private bool validTarg;

    //Used specifically for non-unique verbs that ignore LOS
    public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
    {
        if (targ.IsValid &&
            targ.CenterVector3.InBounds(base.CasterPawn
                .Map)) // && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
        {
            validTarg = (root - targ.Cell).LengthHorizontal < verbProps.range;
        }
        else
        {
            validTarg = false;
        }

        return validTarg;
    }
}