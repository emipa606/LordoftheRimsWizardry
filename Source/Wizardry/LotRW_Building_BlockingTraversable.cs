using Verse;

namespace Wizardry;

public class LotRW_Building_BlockingTraversable : Building
{
    public override void Tick()
    {
        base.Tick();
        if (Map == null)
        {
            return;
        }

        if (Find.TickManager.TicksGame % 2 != 0)
        {
            return;
        }

        DestroyProjectiles();
        if (Find.TickManager.TicksGame % 6 == 0)
        {
            SlowNearbyPawns();
        }
    }

    public void DestroyProjectiles()
    {
        var cellList = Position.GetThingList(Map);
        foreach (var thing in cellList)
        {
            if (thing is not Projectile || thing.def.defName == "LotRW_Projectile_AirWall")
            {
                continue;
            }

            var displayEffect = DrawPos;
            displayEffect.x += Rand.Range(-.3f, .3f);
            displayEffect.y += Rand.Range(-.3f, .3f);
            displayEffect.z += Rand.Range(-.3f, .3f);
            EffectMaker.MakeEffect(ThingDef.Named("Mote_LightningGlow"), displayEffect, Map,
                thing.def.projectile.GetDamageAmount(1) / 8f);
            thing.Destroy();
        }
    }

    public void SlowNearbyPawns()
    {
        var num = GenRadial.NumCellsInRadius(1);
        for (var i = 0; i < num; i++)
        {
            var intVec = Position + GenRadial.RadialPattern[i];
            if (!intVec.IsValid || !intVec.InBounds(Map))
            {
                continue;
            }

            var p = intVec.GetFirstPawn(Map);
            if (p != null)
            {
                HealthUtility.AdjustSeverity(p, HediffDef.Named("LotRW_SlowHD"), .5f);
            }
        }
    }
}