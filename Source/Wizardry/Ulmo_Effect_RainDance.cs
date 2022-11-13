using AbilityUser;
using Verse;

namespace Wizardry;

public class Ulmo_Effect_RainDance : Verb_UseAbility
{
    public virtual void Effect()
    {
        var targetCell = base.CasterPawn.Position;
        targetCell.z += 3;
        LocalTargetInfo t = targetCell;
        if (targetCell.InBounds(base.CasterPawn.Map) && targetCell.IsValid)
        {
            //base.CasterPawn.rotationTracker.Face(targetCell.ToVector3());
            LongEventHandler.QueueLongEvent(delegate
            {
                var flyingObject = (Ulmo_FlyingObject_RainDance)GenSpawn.Spawn(
                    ThingDef.Named("FlyingObject_RainDance"), CasterPawn.Position, CasterPawn.Map);
                flyingObject.Launch(CasterPawn, t.Cell, base.CasterPawn);
            }, "LaunchingFlyer", false, null);
        }
        else
        {
            Log.Message("not enough height to use this ability");
        }
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