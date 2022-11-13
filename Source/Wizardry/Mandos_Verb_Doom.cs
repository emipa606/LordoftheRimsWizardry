using AbilityUser;
using Verse;

namespace Wizardry;

public class Mandos_Verb_Doom : Verb_UseAbility
{
    private LocalTargetInfo action = new LocalTargetInfo();
    private bool validTarg;

    public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
    {
        if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) &&
            !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
        {
            validTarg = (root - targ.Cell).LengthHorizontal < verbProps.range;
        }
        else
        {
            validTarg = false;
        }

        return validTarg;
    }

    protected override bool TryCastShot()
    {
        if (currentTarget.Thing is Pawn targetPawn)
        {
            HealthUtility.AdjustSeverity(targetPawn, HediffDef.Named("LotRW_DoomHD"), 1f);
            for (var i = 0; i < 4; i++)
            {
                EffectMaker.MakeEffect(ThingDef.Named("Mote_BlackSmoke"), targetPawn.DrawPos, targetPawn.Map,
                    Rand.Range(.4f, .6f), Rand.Range(0, 360), Rand.Range(2, 3), Rand.Range(-200, 200), .15f, 0f,
                    Rand.Range(.2f, .3f), true);
            }
        }

        base.PostCastShot(true, out var didShoot);
        return didShoot;
    }
}