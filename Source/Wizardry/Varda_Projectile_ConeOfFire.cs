using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using UnityEngine;
using Verse;
using RimWorld;

namespace Wizardry
{
    class Varda_Projectile_ConeOfFire : Projectile_AbilityBase
    {

        private int age = -1;
        private bool initialized = false;        
        private float distance = 0;        
        Vector3 direction = default;
        Vector3 directionP = default;
        Vector3 currentPos = default;
        IntVec3 centerCell = default;                     
        private int strikeInt = 0;
        private float fireAmount = 12; //amount of fire to expend, subtracts dropoff amounts for each cell traversed

        //local, unsaved variables
        private int nextStrike = 0;
        private readonly int duration = 180;  //maximum duration, should expend fireAmount before this occurs; this is a backstop/failsafe
        private readonly float branchingFlameDropoff = 0.14f;
        private readonly float mainFlameDropoff = 0.2f;
        private readonly float fireStartChance = .25f;
        private readonly int ticksPerStrike = 1; //how fast flames propogate, lower is faster
        Pawn caster = null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref age, "age", -1, false);
            Scribe_Values.Look<int>(ref strikeInt, "strikeInt", 0, false);
            Scribe_Values.Look<float>(ref fireAmount, "fireAmount", 20, false);
            Scribe_Values.Look<float>(ref distance, "distance", 0, false);
            Scribe_Values.Look<IntVec3>(ref centerCell, "centerCell", default,false);
            Scribe_Values.Look<Vector3>(ref direction, "direction", default, false);
            Scribe_Values.Look<Vector3>(ref directionP, "directionP", default, false);
            Scribe_Values.Look<Vector3>(ref currentPos, "currentPos", default, false);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        protected override void Impact(Thing hitThing)
        {            
            base.Impact(hitThing);
            ThingDef def = this.def;
            caster = launcher as Pawn;
            Map map = caster.Map;

            if (!initialized)
            {
                centerCell = caster.Position;
                direction = GetVector(Position, false);
                nextStrike = age + ticksPerStrike;
                currentPos = caster.Position.ToVector3();
                currentPos.y = 0;
                initialized = true;
            }
            if(age > nextStrike && fireAmount > 0)
            {
                currentPos += direction;
                nextStrike = age + ticksPerStrike;
                if (!(currentPos.ToIntVec3().GetTerrain(map).passability == Traversability.Impassable) && currentPos.ToIntVec3().Walkable(map))
                {
                    if (currentPos.ToIntVec3() != caster.Position && Map != null)
                    {
                        EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, currentPos, Map, 1f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 3f, Rand.Range(200, 500));
                        EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, currentPos, Map, .8f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                        List<Thing> hitList = currentPos.ToIntVec3().GetThingList(map);
                        Thing burnThing = null;
                        for (int j = 0; j < hitList.Count; j++)
                        {
                            burnThing = hitList[j];
                            DamageEntities(burnThing);
                        }
                        //GenExplosion.DoExplosion(this.currentPos.ToIntVec3(), this.Map, .4f, DamageDefOf.Flame, this.launcher, 10, SoundDefOf.ArtilleryShellLoaded, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1, 0f, false);
                        if (Rand.Chance(fireStartChance))
                        {
                            FireUtility.TryStartFireIn(currentPos.ToIntVec3(), map, .2f);
                        }
                        fireAmount -= mainFlameDropoff;
                        strikeInt++;
                        Vector3 tempVec1 = currentPos;
                        IntVec3 lastVec1Pos = default;
                        Vector3 tempVec2 = currentPos;
                        IntVec3 lastVec2Pos = default;
                        distance = Mathf.Max(5f, distance);
                        for (float i = (float)strikeInt / distance; i > .3f; i -= .5f)
                        {
                            tempVec1 += directionP;
                            if (tempVec1.ToIntVec3() != currentPos.ToIntVec3() && tempVec1.ToIntVec3() != lastVec1Pos)
                            {
                                lastVec1Pos = tempVec1.ToIntVec3();
                                EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, tempVec1, Map, .8f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 2f, Rand.Range(200, 500));
                                EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, tempVec1, Map, .7f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                                hitList = lastVec1Pos.GetThingList(map);
                                for (int j = 0; j < hitList.Count; j++)
                                {
                                    burnThing = hitList[j];
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
                            if (tempVec2.ToIntVec3() != currentPos.ToIntVec3() && tempVec2.ToIntVec3() != lastVec2Pos)
                            {
                                lastVec2Pos = tempVec2.ToIntVec3();
                                EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, tempVec2, Map, .8f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 2f, Rand.Range(200, 500));
                                EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, tempVec2, Map, .7f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                                hitList = lastVec2Pos.GetThingList(map);
                                for (int j = 0; j < hitList.Count; j++)
                                {
                                    burnThing = hitList[j];
                                    DamageEntities(burnThing);
                                }
                                if (Rand.Chance(fireStartChance))
                                {
                                    FireUtility.TryStartFireIn(lastVec2Pos, map, .2f);
                                }
                                fireAmount -= branchingFlameDropoff;
                                //GenExplosion.DoExplosion(lastVec2Pos, this.Map, .4f, DamageDefOf.Flame, this.launcher, 10, SoundDefOf.ArtilleryShellLoaded, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1, 0f, false);
                            }
                            if (fireAmount < 0)
                            {
                                i = 0;
                            }
                        }
                    }
                }
                else
                {
                    //main branch of fire cone hit impassable or unwalkable terrain
                    age = duration;
                }
            }
        }

        public Vector3 GetVector(IntVec3 closestTarget, bool reverseDirection)
        {
            Vector3 heading = (closestTarget - caster.Position).ToVector3();
            if(reverseDirection)
            {
                heading = Quaternion.AngleAxis(180, Vector3.up) * heading;
            }
            distance = heading.magnitude;
            Vector3 dirVec = heading / distance;
            directionP = Quaternion.AngleAxis(90, Vector3.up) * dirVec;
            return dirVec;
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
    }
}