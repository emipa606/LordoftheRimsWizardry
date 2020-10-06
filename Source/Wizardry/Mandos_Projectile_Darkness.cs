using Verse;
using RimWorld;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Wizardry
{
    class Mandos_Projectile_Darkness : Projectile_AbilityBase
    {
        private int age = -1;
        private bool initialized = false;
        private float radius;
        private int duration = 900;
        private List<IntVec3> inDarkness = new List<IntVec3>();

        //unsaved variables
        private readonly int hediffFrequency = 60;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref age, "age", -1, false);
            Scribe_Values.Look<int>(ref duration, "duration", 900, false);
            Scribe_Values.Look<float>(ref radius, "radius", 7f, false);
            Scribe_Collections.Look<IntVec3>(ref inDarkness, "inDarkness", LookMode.Value);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        private void Initialize()
        {
            radius = (int)def.projectile.explosionRadius;
            inDarkness = GenRadial.RadialCellsAround(Position, radius, false).ToList();
            for (int i = 0; i < inDarkness.Count(); i++)
            {
                if (inDarkness[i].IsValid && inDarkness[i].InBounds(Map))
                {
                    ThingDef darkness = ThingDef.Named("Mandos_BlackSmoke");
                    GenSpawn.Spawn(darkness, inDarkness[i], Map, WipeMode.Vanish);
                    //GenExplosion.DoExplosion(inDarkness[i], base.Map, 1, DamageDefOf.Smoke, this.launcher, -1, -1, null, null, null, null, darkness, 1);
                    EffectMaker.MakeEffect(ThingDef.Named("Mote_BlackSmoke"), inDarkness[i].ToVector3Shifted(), Map, Rand.Range(1f, 2f), Rand.Range(0, 360), Rand.Range(.1f, .2f), Rand.Range(-20, 20), duration / 240, Rand.Range(.5f, 1.5f), Rand.Range(2f, 3f), true);
                    //EffectMaker.MakeEffect(ThingDef.Named("Mote_BlackSmoke"), this.inDarkness[i].ToVector3Shifted(), base.Map, Rand.Range(1f, 2f), Rand.Range(0, 360), Rand.Range(.1f, .2f), Rand.Range(-20, 20), this.duration / 60, Rand.Range(.5f, 1.5f), Rand.Range(2f, 3f), true);
                    Pawn victim = null;
                    victim = inDarkness[i].GetFirstPawn(Map);
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

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            if(!initialized)
            {
                Initialize();
            }

            if(Find.TickManager.TicksGame % hediffFrequency == 0)
            {
                for(int i = 0; i < inDarkness.Count(); i++)
                {
                    Pawn victim = null;
                    victim = inDarkness[i].GetFirstPawn(Map);
                    if(victim != null)
                    {
                        HealthUtility.AdjustSeverity(victim, HediffDef.Named("LotRW_DarknessHD"), 1);
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_BodyOutline"), victim.DrawPos, victim.Map, 1f, 0, 0, 0, 0, .05f, .2f, .2f, false);
                    }
                }
            }              
        }

        public override void Tick()
        {
            base.Tick();
            age++;
        }
    }
}
