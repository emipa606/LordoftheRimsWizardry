using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using Verse;

namespace Wizardry;

internal class Mandos_Projectile_Darkness : Projectile_AbilityBase
{
    //unsaved variables
    private readonly int hediffFrequency = 60;
    private int age = -1;
    private int duration = 900;
    private List<IntVec3> inDarkness = new List<IntVec3>();
    private bool initialized;
    private float radius;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref initialized, "initialized", true);
        Scribe_Values.Look(ref age, "age", -1);
        Scribe_Values.Look(ref duration, "duration", 900);
        Scribe_Values.Look(ref radius, "radius", 7f);
        Scribe_Collections.Look(ref inDarkness, "inDarkness", LookMode.Value);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!(age < duration))
        {
            base.Destroy(mode);
        }
    }

    private void Initialize()
    {
        radius = (int)def.projectile.explosionRadius;
        inDarkness = GenRadial.RadialCellsAround(Position, radius, false).ToList();
        for (var i = 0; i < inDarkness.Count; i++)
        {
            if (inDarkness[i].IsValid && inDarkness[i].InBounds(Map))
            {
                var darkness = ThingDef.Named("Mandos_BlackSmoke");
                GenSpawn.Spawn(darkness, inDarkness[i], Map);
                EffectMaker.MakeEffect(ThingDef.Named("Mote_BlackSmoke"), inDarkness[i].ToVector3Shifted(), Map,
                    Rand.Range(1f, 2f), Rand.Range(0, 360), Rand.Range(.1f, .2f), Rand.Range(-20, 20),
                    (float)duration / 240, Rand.Range(.5f, 1.5f), Rand.Range(2f, 3f), true);
                var victim = inDarkness[i].GetFirstPawn(Map);
                if (victim != null)
                {
                    HealthUtility.AdjustSeverity(victim, HediffDef.Named("LotRW_DarknessHD"), 1);
                }
            }
            else
            {
                inDarkness.Remove(inDarkness[i]);
            }
        }

        initialized = true;
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        base.Impact(hitThing, blockedByShield);
        if (!initialized)
        {
            Initialize();
        }

        if (Find.TickManager.TicksGame % hediffFrequency != 0)
        {
            return;
        }

        for (var i = 0; i < inDarkness.Count; i++)
        {
            var victim = inDarkness[i].GetFirstPawn(Map);
            if (victim == null)
            {
                continue;
            }

            HealthUtility.AdjustSeverity(victim, HediffDef.Named("LotRW_DarknessHD"), 1);
            EffectMaker.MakeEffect(ThingDef.Named("Mote_BodyOutline"), victim.DrawPos, victim.Map, 1f, 0, 0,
                0, 0, .05f, .2f, .2f, false);
        }
    }

    public override void Tick()
    {
        base.Tick();
        age++;
    }
}