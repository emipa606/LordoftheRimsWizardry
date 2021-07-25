using AbilityUser;
using Verse;

namespace Wizardry
{
    public class Mandos_Effect_Haunt : Verb_UseAbility
    {
        private bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) &&
                !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < verbProps.range)
                {
                    if (CasterIsPawn && CasterPawn.apparel != null)
                    {
                        var wornApparel = CasterPawn.apparel.WornApparel;
                        foreach (var apparel in wornApparel)
                        {
                            if (!apparel.AllowVerbCast(this))
                            {
                                return false;
                            }
                        }

                        validTarg = true;
                    }
                    else
                    {
                        validTarg = true;
                    }
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }

            return validTarg;
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
                def = WizardryDefOf.FlyingObject_Haunt
            };
            //Pawn casterPawn = base.CasterPawn;
            LongEventHandler.QueueLongEvent(delegate
            {
                var flyingObject = (Mandos_FlyingObject_Haunt) GenSpawn.Spawn(ThingDef.Named("FlyingObject_Haunt"),
                    CasterPawn.Position, CasterPawn.Map);
                flyingObject.AdvancedLaunch(CasterPawn, ThingDef.Named("Mote_BlackSmoke"), 1, Rand.Range(10, 30),
                    20, false, CasterPawn.DrawPos, currentTarget, launchedThing, 25, false, Rand.Range(180, 240),
                    1f, 0, 0, WizardryDefOf.LotRW_HauntDD);
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
}