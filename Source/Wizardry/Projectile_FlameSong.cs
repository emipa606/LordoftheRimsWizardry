using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry
{
    internal class Projectile_FlameSong : Projectile_AbilityBase
    {
        private readonly float fireStartChance = .25f;
        private int age = -1;

        private Pawn caster;

        private int
            duration = 5; //maximum duration, should expend fireAmount before this occurs; this is a backstop/failsafe

        private int expandingTick;
        private float fireAmount = 10;
        private bool initialized;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age, "age", -1);
            Scribe_Values.Look(ref duration, "duration", 5);
            Scribe_Values.Look(ref expandingTick, "expandingTick");
            Scribe_Values.Look(ref fireAmount, "fireAmount", 20);
            Scribe_Values.Look(ref initialized, "initialized");
            Scribe_References.Look(ref caster, "caster");
        }

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            if (!initialized)
            {
                Initialize();
            }

            var unused = def;
            var map = caster.Map;
            expandingTick++;
            var centerCell = Position;
            var oldExplosionCells = GenRadial.RadialCellsAround(centerCell, expandingTick - 1, true);
            var newExplosionCells = GenRadial.RadialCellsAround(centerCell, expandingTick, true);
            var explosionCells = newExplosionCells.Except(oldExplosionCells);
            for (var i = 0; i < explosionCells.Count(); i++)
            {
                var curCell = explosionCells.ToArray()[i];
                if (!curCell.InBounds(map) || !curCell.IsValid)
                {
                    continue;
                }

                var heading = (curCell - centerCell).ToVector3();
                var distance = heading.magnitude;
                var direction = heading / distance;
                EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, curCell.ToVector3(), map, .8f,
                    (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 4f, Rand.Range(100, 200));
                EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, curCell.ToVector3(), map, .7f,
                    (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                var hitList = curCell.GetThingList(map);
                foreach (var burnThing in hitList)
                {
                    DamageEntities(burnThing);
                }

                //GenExplosion.DoExplosion(this.currentPos.ToIntVec3(), this.Map, .4f, DamageDefOf.Flame, this.launcher, 10, SoundDefOf.ArtilleryShellLoaded, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1, 0f, false);
                if (Rand.Chance(fireStartChance))
                {
                    FireUtility.TryStartFireIn(curCell, map, Rand.Range(.1f, .35f));
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
            var amt = Mathf.RoundToInt(Rand.Range(def.projectile.GetDamageAmount(1) * .75f,
                def.projectile.GetDamageAmount(1) * 1.25f) + fireAmount);
            var dinfo = new DamageInfo(DamageDefOf.Flame, amt);
            if (e != null)
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
            if (!(age < duration) && initialized)
            {
                base.Destroy(mode);
            }
        }
    }
}