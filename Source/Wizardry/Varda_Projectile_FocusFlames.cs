using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry
{
    internal class Varda_Projectile_FocusFlames : Projectile_AbilityBase
    {
        private readonly float branchingFlameDropoff = 0.1f;

        //local, unsaved variables
        private readonly int
            duration = 120; //maximum duration, should expend fireAmount before this occurs; this is a backstop/failsafe

        private readonly float fireStartChance = 0.05f;
        private readonly float mainFlameDropoff = 0.2f;
        private readonly int ticksPerStrike = 2; //how fast flames propogate, lower is faster        

        private int age = -1;
        private Pawn caster;
        private IntVec3 centerCell;
        private Vector3 currentPos;
        private Vector3 direction;
        private Vector3 directionP;
        private float distance;
        private float fireAmount;
        private bool initialized;
        private int nextStrike;
        private int strikeInt;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialized, "initialized");
            Scribe_Values.Look(ref age, "age", -1);
            Scribe_Values.Look(ref strikeInt, "strikeInt");
            Scribe_Values.Look(ref fireAmount, "fireAmount");
            Scribe_Values.Look(ref distance, "distance");
            Scribe_Values.Look(ref centerCell, "centerCell");
            Scribe_Values.Look(ref direction, "direction");
            Scribe_Values.Look(ref directionP, "directionP");
            Scribe_Values.Look(ref currentPos, "currentPos");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (!(age < duration))
            {
                base.Destroy(mode);
            }
        }

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            var unused = def;
            caster = launcher as Pawn;
            var map = caster?.Map;
            var noTarget = false;

            if (!initialized)
            {
                centerCell = Position;
                fireAmount = CalculateFireAmountInArea(centerCell, def.projectile.explosionRadius);
                var closestTarget = SearchForClosestPawn(centerCell, def.projectile.explosionRadius * 5, map);
                if (closestTarget == null)
                {
                    closestTarget = caster;
                    noTarget = true;
                }

                direction = GetVector(closestTarget.Position, noTarget, fireAmount);
                nextStrike = age + ticksPerStrike;
                currentPos = Position.ToVector3();
                distance = Mathf.Min(20f, distance);
                initialized = true;
            }

            if (age <= nextStrike || !(fireAmount > 0))
            {
                return;
            }

            currentPos += direction;
            nextStrike = age + ticksPerStrike;
            if (currentPos.ToIntVec3().GetTerrain(map).passability != Traversability.Impassable &&
                currentPos.ToIntVec3().Walkable(map))
            {
                if (currentPos.ToIntVec3() == Position || Map == null)
                {
                    return;
                }

                EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, currentPos, Map, 1f,
                    (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 5f, Rand.Range(10, 50));
                EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, currentPos, Map, .8f,
                    (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                var hitList = currentPos.ToIntVec3().GetThingList(map);
                Thing burnThing;
                foreach (var thing in hitList)
                {
                    burnThing = thing;
                    DamageEntities(burnThing);
                }

                //GenExplosion.DoExplosion(this.currentPos.ToIntVec3(), this.Map, .4f, DamageDefOf.Flame, this.launcher, 10, SoundDefOf.ArtilleryShellLoaded, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1, 0f, false);
                if (Rand.Chance(fireStartChance))
                {
                    FireUtility.TryStartFireIn(currentPos.ToIntVec3(), map, .2f);
                }

                fireAmount -= mainFlameDropoff;
                strikeInt++;
                var tempVec1 = currentPos;
                IntVec3 lastVec1Pos = default;
                var tempVec2 = currentPos;
                IntVec3 lastVec2Pos = default;
                for (var i = (float) strikeInt / Mathf.RoundToInt(distance); i > .4f; i -= .6f)
                {
                    tempVec1 += directionP;
                    if (tempVec1.ToIntVec3() != currentPos.ToIntVec3() && tempVec1.ToIntVec3() != lastVec1Pos)
                    {
                        lastVec1Pos = tempVec1.ToIntVec3();
                        EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, tempVec1, Map, .8f,
                            (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 4f,
                            Rand.Range(10, 50));
                        EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, tempVec1, Map, .7f,
                            (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                        hitList = lastVec1Pos.GetThingList(map);
                        foreach (var thing in hitList)
                        {
                            burnThing = thing;
                            DamageEntities(burnThing);
                        }

                        if (Rand.Chance(fireStartChance))
                        {
                            FireUtility.TryStartFireIn(lastVec1Pos, map, .2f);
                        }

                        fireAmount -= branchingFlameDropoff;
                        //GenExplosion.DoExplosion(lastVec1Pos, this.Map, .4f, DamageDefOf.Flame, this.launcher, 10, SoundDefOf.ArtilleryShellLoaded, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1, 0f, false);
                    }

                    tempVec2 -= directionP;
                    if (tempVec2.ToIntVec3() == currentPos.ToIntVec3() || tempVec2.ToIntVec3() == lastVec2Pos)
                    {
                        continue;
                    }

                    lastVec2Pos = tempVec2.ToIntVec3();
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, tempVec2, Map, .8f,
                        (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 4f,
                        Rand.Range(10, 50));
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, tempVec2, Map, .7f,
                        (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                    hitList = lastVec2Pos.GetThingList(map);
                    foreach (var thing in hitList)
                    {
                        burnThing = thing;
                        DamageEntities(burnThing);
                    }

                    if (Rand.Chance(fireStartChance))
                    {
                        FireUtility.TryStartFireIn(lastVec1Pos, map, .2f);
                    }

                    fireAmount -= branchingFlameDropoff;
                    //GenExplosion.DoExplosion(lastVec2Pos, this.Map, .4f, DamageDefOf.Flame, this.launcher, 10, SoundDefOf.ArtilleryShellLoaded, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1, 0f, false);
                }
            }
            else
            {
                age = duration;
            }
        }

        public Vector3 GetVector(IntVec3 closestTarget, bool reverseDirection, float magnitude)
        {
            var heading = (closestTarget - Position).ToVector3();
            if (reverseDirection)
            {
                heading = Quaternion.AngleAxis(180, Vector3.up) * heading;
            }

            distance = heading.magnitude;
            var dirVec = heading / distance;
            directionP = Quaternion.AngleAxis(90, Vector3.up) * dirVec;
            return dirVec;
        }

        public Pawn SearchForClosestPawn(IntVec3 center, float radius, Map map)
        {
            Pawn targetVictim = null;
            var targets = GenRadial.RadialCellsAround(center, radius, false);
            for (var i = 0; i < targets.Count(); i++)
            {
                var curCell = targets.ToArray()[i];
                if (!curCell.InBounds(map) || !curCell.IsValid)
                {
                    continue;
                }

                var victim = curCell.GetFirstPawn(map);
                if (victim is not {Downed: false, Dead: false} || victim.Faction == caster.Faction)
                {
                    continue;
                }

                targetVictim = victim;
                i = targets.Count();
            }

            return targetVictim;
        }

        public float CalculateFireAmountInArea(IntVec3 center, float radius)
        {
            float result = 0;
            var fireList = Map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            var targetCells = GenRadial.RadialCellsAround(center, radius, true);
            for (var i = 0; i < targetCells.Count(); i++)
            {
                var curCell = targetCells.ToArray()[i];
                if (!curCell.InBounds(Map) || !curCell.IsValid)
                {
                    continue;
                }

                foreach (var thing in fireList)
                {
                    if (thing.Position != curCell)
                    {
                        continue;
                    }

                    if (thing is Fire fire)
                    {
                        result += fire.fireSize;
                    }

                    RemoveFireAtPosition(curCell);
                }
            }

            return result;
        }

        public void RemoveFireAtPosition(IntVec3 pos)
        {
            GenExplosion.DoExplosion(pos, Map, 1, DamageDefOf.Extinguish, launcher, 100, 0,
                SoundDef.Named("ExpandingFlames"), def, equipmentDef);
        }

        public void DamageEntities(Thing e)
        {
            var amt = Mathf.RoundToInt(Rand.Range(def.projectile.GetDamageAmount(1) * .5f,
                def.projectile.GetDamageAmount(1) * 1.5f) + fireAmount);
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
    }
}