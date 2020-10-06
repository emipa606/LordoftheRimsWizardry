using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using UnityEngine;
using Verse;
using RimWorld;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    class Aule_Projectile_RendEarth : Projectile_AbilityBase
    {
        private int age = -1;
        private bool initialized = false;
        private float distance = 0;
        private float angle = 0;               
        private int strikeCount = 0;
        private float maxRange = 0;
        private int approximateDuration = 0;
        private bool wallImpact = false;
        IntVec3 boltPosition = default;
        IntVec3 boltOrigin = default;
        Vector3 direction = default;        

        //local, unsaved variables
        private int duration = 600;  //maximum duration as a failsafe
        private readonly int fadeTicks = 300;
        private readonly int ticksPerStrike = 5;
        private int nextStrike = 0;
        private readonly float boltTravelRate = 2f;
        Pawn caster = null;

        private int boltMaxCount = 20;
        private float boltRange = 100;

        private Mesh boltMesh = null;
        private static readonly List<Mesh> boltMeshes = new List<Mesh>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref wallImpact, "wallImpact", false, false);
            Scribe_Values.Look<int>(ref age, "age", -1, false);
            Scribe_Values.Look<int>(ref duration, "duration", 0, false);
            Scribe_Values.Look<int>(ref approximateDuration, "approximateDuration", 0, false);
            Scribe_Values.Look<int>(ref strikeCount, "strikeCount", 0, false);
            Scribe_Values.Look<float>(ref distance, "distance", 0, false);
            Scribe_Values.Look<float>(ref angle, "angle", 0, false);
            Scribe_Values.Look<IntVec3>(ref boltOrigin, "boltOrigin", default, false);
            Scribe_Values.Look<IntVec3>(ref boltPosition, "boltPosition", default, false);
            Scribe_Values.Look<Vector3>(ref direction, "direction", default, false);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = age < (duration + fadeTicks);
            if (!flag)
            {
                boltMeshes.Clear();
                base.Destroy(mode);
            }
        }

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            ThingDef def = this.def;
            caster = launcher as Pawn;
            if (Map != null)
            {
                if (!initialized)
                {
                    direction = GetVector(caster.Position, Position);
                    angle = (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat();
                    boltOrigin = caster.Position + (2f * direction).ToIntVec3();                    
                    maxRange = (boltOrigin - Position).LengthHorizontal;
                    boltMaxCount = Mathf.RoundToInt(maxRange);
                    approximateDuration = (int)((maxRange / boltTravelRate) * ticksPerStrike * 1.25f);                    
                    boltMesh = RandomBoltMesh;
                    initialized = true;                    
                }                

                if (nextStrike < age && age < duration && !wallImpact)
                {
                    strikeCount++;
                    boltMesh = null;
                    boltPosition = (boltOrigin + (boltTravelRate * strikeCount * direction).ToIntVec3());
                    boltRange = (boltOrigin - boltPosition).LengthHorizontal;
                    boltMaxCount = Mathf.RoundToInt(boltRange);
                    boltMesh = RandomBoltMesh;                    
                    nextStrike = age + ticksPerStrike;
                    DoQuakeDamages(2, boltPosition);
                    
                    if (Rand.Chance(.6f))
                    {
                        IntVec3 smallMeshDestination;
                        if (Rand.Chance(.5f))
                        {
                            smallMeshDestination = (boltPosition + ((Quaternion.AngleAxis(Rand.Range(30, 60), Vector3.up) * direction) * Rand.Range(2f, 5f)).ToIntVec3());
                        }
                        else
                        {
                            smallMeshDestination = (boltPosition + ((Quaternion.AngleAxis(Rand.Range(-30, -60), Vector3.up) * direction) * Rand.Range(2f, 5f)).ToIntVec3());
                        }

                        Map.weatherManager.eventHandler.AddEvent(new MeshMaker(Map, MatPool.rendEarthMat7, boltPosition, smallMeshDestination, Rand.Range(2f, 6f), AltitudeLayer.Floor, approximateDuration - age, fadeTicks, 10));
                        DoQuakeDamages(1.4f, smallMeshDestination);                       
                    }

                    if (maxRange < (boltOrigin - boltPosition).LengthHorizontal)
                    {
                        wallImpact = true;
                        duration = approximateDuration;
                    }
                }
                DrawStrike(boltOrigin, boltPosition.ToVector3());
            }
        }

        private void DoQuakeDamages(float radius, IntVec3 location)
        {
            int num = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < num; i++)
            {
                Building structure = null;
                Pawn pawn = null;
                IntVec3 intVec = location + GenRadial.RadialPattern[i];
                structure = null;
                if (intVec.IsValid && intVec.InBounds(Map))
                {
                    if (Rand.Chance(.4f))
                    {
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_ThickDust"), intVec.ToVector3Shifted(), Map, Rand.Range(.2f, 2f), Rand.Range(0, 360), Rand.Range(.5f, 1f), Rand.Range(10, 250), 0, Rand.Range(.3f, .9f), Rand.Range(.05f, .3f), Rand.Range(.6f, 2.4f), true);
                    }

                    structure = intVec.GetFirstBuilding(Map);
                    if (structure != null)
                    {
                        if (structure.def.designationCategory == DesignationCategoryDefOf.Structure)
                        {
                            DamageEntities(structure, structure.def.BaseMaxHitPoints, DamageDefOf.Crush);
                            Vector3 moteDirection = GetVector(origin.ToIntVec3(), intVec);
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map, Rand.Range(.3f, .5f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 8f, 0);
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map, Rand.Range(.3f, .6f), (Quaternion.AngleAxis(Rand.Range(-70, -110), Vector3.up) * moteDirection).ToAngleFlat(), 6f, 0);
                            GenExplosion.DoExplosion(intVec, Map, .4f, WizardryDefOf.LotRW_RockFragments, launcher, Rand.Range(6, 16), 0, SoundDefOf.Pawn_Melee_Punch_HitBuilding, null, null, null, ThingDef.Named("Filth_RubbleRock"), .4f, 1, false, null, 0f, 1, 0, false);
                            MoteMaker.ThrowSmoke(intVec.ToVector3Shifted(), Map, Rand.Range(.6f, 1f));
                            if (intVec == boltPosition)
                            {
                                wallImpact = true;
                                duration = approximateDuration;
                            }
                        }
                        else if (structure.def.building.isResourceRock)
                        {
                            ThingDef yieldThing = structure.def.building.mineableThing;
                            int yieldAmount = (int)(structure.def.building.mineableYield * Rand.Range(.7f, .9f));
                            DamageEntities(structure, structure.def.BaseMaxHitPoints, DamageDefOf.Crush);
                            Vector3 moteDirection = GetVector(origin.ToIntVec3(), intVec);
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map, Rand.Range(.3f, .5f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 8f, 0);
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map, Rand.Range(.3f, .6f), (Quaternion.AngleAxis(Rand.Range(-70, -110), Vector3.up) * moteDirection).ToAngleFlat(), 6f, 0);
                            GenExplosion.DoExplosion(intVec, Map, .4f, WizardryDefOf.LotRW_RockFragments, launcher, Rand.Range(6, 16), 0, SoundDefOf.Crunch, null, null, null, yieldThing, 1f, yieldAmount, false, null, 0f, 1, 0, false);
                            MoteMaker.ThrowSmoke(intVec.ToVector3Shifted(), Map, Rand.Range(.6f, 1f));
                            if (intVec == boltPosition)
                            {
                                wallImpact = true;
                                duration = approximateDuration;
                            }
                        }
                        else if (structure.def.building.isNaturalRock)
                        {
                            ThingDef yieldThing = structure.def.building.mineableThing;
                            DamageEntities(structure, structure.def.BaseMaxHitPoints, DamageDefOf.Crush);
                            Vector3 moteDirection = GetVector(origin.ToIntVec3(), intVec);
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map, Rand.Range(.3f, .5f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 8f, 0);
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map, Rand.Range(.3f, .6f), (Quaternion.AngleAxis(Rand.Range(-70, -110), Vector3.up) * moteDirection).ToAngleFlat(), 6f, 0);
                            GenExplosion.DoExplosion(intVec, Map, .4f, WizardryDefOf.LotRW_RockFragments, launcher, Rand.Range(6, 16), 0, SoundDefOf.Crunch, null, null, null, yieldThing, .2f, 1, false, null, 0f, 1, 0, false);
                            MoteMaker.ThrowSmoke(intVec.ToVector3Shifted(), Map, Rand.Range(.6f, 1f));
                            if (intVec == boltPosition)
                            {
                                wallImpact = true;
                                duration = approximateDuration;
                            }
                        }
                        else
                        {
                            DamageEntities(structure, Rand.Range(40, 50), DamageDefOf.Crush);
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map, Rand.Range(.3f, .6f), Rand.Range(0, 359), 6f, 0);
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map, Rand.Range(.3f, .6f), Rand.Range(0, 359), 4f, 0);
                            MoteMaker.ThrowSmoke(intVec.ToVector3Shifted(), Map, Rand.Range(.6f, 1f));
                        }
                    }

                    pawn = intVec.GetFirstPawn(Map);
                    if (pawn != null && pawn != caster)
                    {
                        if (Rand.Chance(.2f))
                        {
                            DamageEntities(pawn, Rand.Range(4, 6), DamageDefOf.Crush);
                        }
                        HealthUtility.AdjustSeverity(pawn, HediffDef.Named("LotRW_Quake"), Rand.Range(1f, 2f));
                    }
                }
            }            
        }

        public void DrawStrike(IntVec3 start, Vector3 dest)
        {
            if (boltOrigin != default)
            {
                float magnitude = (boltPosition.ToVector3Shifted() - Find.Camera.transform.position).magnitude;
                if (age <= duration)
                {                    
                    Find.CameraDriver.shaker.DoShake(20 / magnitude);
                    Graphics.DrawMesh(boltMesh, boltOrigin.ToVector3ShiftedWithAltitude(AltitudeLayer.Floor), Quaternion.Euler(0f, angle, 0f), FadedMaterialPool.FadedVersionOf(MatPool.rendEarthMat7, 1), 0);
                }
                else
                {
                    Graphics.DrawMesh(boltMesh, boltOrigin.ToVector3ShiftedWithAltitude(AltitudeLayer.Floor), Quaternion.Euler(0f, angle, 0f), FadedMaterialPool.FadedVersionOf(MatPool.rendEarthMat7, MeshBrightness), 0);
                }                
            }
        }

        protected float MeshBrightness
        {
            get
            {
                return 1f - ((float)(age - duration) / fadeTicks);
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (objectPos - center).ToVector3();
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;            
            return direction;
        }

        public void DamageEntities(Thing e, int amt, DamageDef damageType)
        {            
            DamageInfo dinfo = new DamageInfo(damageType, amt, 0, (float)-1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            
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

        public Mesh RandomBoltMesh
        {
            get
            {
                Mesh result;
                boltMeshes.Clear();
                if (boltMeshes.Count < boltMaxCount)
                {
                    Mesh mesh = Effect_MeshMaker.NewBoltMesh(boltRange, 0);
                    boltMeshes.Add(mesh);
                    result = mesh;
                }
                else
                {
                    result = boltMeshes.RandomElement<Mesh>();
                }
                return result;
            }
        }
    }
}