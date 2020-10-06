using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using UnityEngine;
using Verse;
using RimWorld;

namespace Wizardry
{
    class Projectile_FlameSong : Projectile_AbilityBase
    {

        private int expandingTick = 0;
        private readonly float fireStartChance = .25f;
        private int duration = 5;  //maximum duration, should expend fireAmount before this occurs; this is a backstop/failsafe
        private int age = -1;
        private float fireAmount = 10;
        private bool initialized = false;

        Pawn caster = null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref age, "age", -1, false);
            Scribe_Values.Look<int>(ref duration, "duration", 5, false);
            Scribe_Values.Look<int>(ref expandingTick, "expandingTick", 0, false);
            Scribe_Values.Look<float>(ref fireAmount, "fireAmount", 20, false);
            Scribe_Values.Look<bool>(ref initialized, "initialized", false, false);
            Scribe_References.Look<Pawn>(ref caster, "caster", false);
        }

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            if(!initialized)
            {
                Initialize();
            }
            ThingDef def = this.def;
            Map map = caster.Map;
            expandingTick++;
            IntVec3 centerCell = Position;
            IntVec3 curCell;
            IEnumerable<IntVec3> oldExplosionCells = GenRadial.RadialCellsAround(centerCell, expandingTick - 1, true);
            IEnumerable<IntVec3> newExplosionCells = GenRadial.RadialCellsAround(centerCell, expandingTick, true);
            IEnumerable<IntVec3> explosionCells = newExplosionCells.Except(oldExplosionCells);
            for (int i = 0; i < explosionCells.Count(); i++)
            {
                curCell = explosionCells.ToArray<IntVec3>()[i];
                if (curCell.InBounds(map) && curCell.IsValid)
                {
                    Vector3 heading = (curCell - centerCell).ToVector3();
                    float distance = heading.magnitude;
                    Vector3 direction = heading / distance;
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, curCell.ToVector3(), map, .8f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 4f, Rand.Range(100, 200));
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, curCell.ToVector3(), map, .7f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                    List<Thing> hitList = curCell.GetThingList(map);
                    Thing burnThing = null;
                    for (int j = 0; j < hitList.Count; j++)
                    {
                        burnThing = hitList[j];
                        DamageEntities(burnThing);
                    }
                    //GenExplosion.DoExplosion(this.currentPos.ToIntVec3(), this.Map, .4f, DamageDefOf.Flame, this.launcher, 10, SoundDefOf.ArtilleryShellLoaded, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1, 0f, false);
                    if (Rand.Chance(fireStartChance))
                    {
                        FireUtility.TryStartFireIn(curCell, map, Rand.Range(.1f, .35f));
                    }
                }
            }
        }

        private void Initialize()
        {
            caster = launcher as Pawn;
            duration = Mathf.RoundToInt(def.projectile.explosionRadius);
            age = 0;
            initialized = true;            
        }

        public void DamageEntities(Thing e)
        {            
            int amt = Mathf.RoundToInt(Rand.Range(def.projectile.GetDamageAmount(1, null) * .75f, def.projectile.GetDamageAmount(1, null) * 1.25f) + fireAmount);
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, amt, 0, (float)-1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }

        public override void Tick()
        {
            base.Tick();
            age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = age < duration;
            if (!flag && initialized)
            {
                base.Destroy(mode);
            }
        }

    }
}