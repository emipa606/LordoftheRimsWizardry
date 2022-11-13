using AbilityUser;
using Verse;

namespace Wizardry;

public class Ulmo_Effect_WolfSong : Verb_UseAbility
{
    private bool validTarg;

    public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
    {
        if (!targ.IsValid || !targ.CenterVector3.InBounds(base.CasterPawn.Map) ||
            targ.Cell.Fogged(base.CasterPawn.Map) || !targ.Cell.Walkable(base.CasterPawn.Map))
        {
            return false;
        }

        if (!((root - targ.Cell).LengthHorizontal < verbProps.range))
        {
            return false;
        }

        if (!CasterIsPawn || CasterPawn.apparel == null)
        {
            return true;
        }

        var wornApparel = CasterPawn.apparel.WornApparel;
        foreach (var apparel in wornApparel)
        {
            if (!apparel.AllowVerbCast(this))
            {
                return false;
            }
        }

        return true;

        //out of range
    }

    public virtual void Effect()
    {
        var t = currentTarget;
        if (t.Cell == default)
        {
            return;
        }

        var launchedThing = new Thing
        {
            def = WizardryDefOf.FlyingObject_WolfSong
        };
        //Pawn casterPawn = base.CasterPawn;
        LongEventHandler.QueueLongEvent(delegate
        {
            var flyingObject =
                (Ulmo_FlyingObject_WolfSong)GenSpawn.Spawn(ThingDef.Named("FlyingObject_WolfSong"),
                    CasterPawn.Position, CasterPawn.Map);
            flyingObject.Launch(CasterPawn, t.Cell, launchedThing);
        }, "LaunchingFlyer", false, null);
    }

    public override void PostCastShot(bool inResult, out bool outResult)
    {
        if (inResult)
        {
            Effect();
            outResult = true;
        }

        outResult = inResult;
    }
}