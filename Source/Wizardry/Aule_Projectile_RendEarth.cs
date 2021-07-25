using System.Collections.Generic;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    internal class Aule_Projectile_RendEarth : Projectile_AbilityBase
    {
        private static readonly List<Mesh> boltMeshes = new List<Mesh>();
        private readonly float boltTravelRate = 2f;
        private readonly int fadeTicks = 300;
        private readonly int ticksPerStrike = 5;
        private int age = -1;
        private float angle;
        private int approximateDuration;

        private int boltMaxCount = 20;

        private Mesh boltMesh;
        private IntVec3 boltOrigin;
        private IntVec3 boltPosition;
        private float boltRange = 100;
        private Pawn caster;
        private Vector3 direction;
        private float distance;

        //local, unsaved variables
        private int duration = 600; //maximum duration as a failsafe
        private bool initialized;
        private float maxRange;
        private int nextStrike;
        private int strikeCount;
        private bool wallImpact;

        protected float MeshBrightness => 1f - ((float) (age - duration) / fadeTicks);

        public Mesh RandomBoltMesh
        {
            get
            {
                Mesh result;
                boltMeshes.Clear();
                if (boltMeshes.Count < boltMaxCount)
                {
                    var mesh = Effect_MeshMaker.NewBoltMesh(boltRange, 0);
                    boltMeshes.Add(mesh);
                    result = mesh;
                }
                else
                {
                    result = boltMeshes.RandomElement();
                }

                return result;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialized, "initialized");
            Scribe_Values.Look(ref wallImpact, "wallImpact");
            Scribe_Values.Look(ref age, "age", -1);
            Scribe_Values.Look(ref duration, "duration");
            Scribe_Values.Look(ref approximateDuration, "approximateDuration");
            Scribe_Values.Look(ref strikeCount, "strikeCount");
            Scribe_Values.Look(ref distance, "distance");
            Scribe_Values.Look(ref angle, "angle");
            Scribe_Values.Look(ref boltOrigin, "boltOrigin");
            Scribe_Values.Look(ref boltPosition, "boltPosition");
            Scribe_Values.Look(ref direction, "direction");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (age < duration + fadeTicks)
            {
                return;
            }

            boltMeshes.Clear();
            base.Destroy(mode);
        }

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            var unused = def;
            caster = launcher as Pawn;
            if (Map == null)
            {
                return;
            }

            if (!initialized)
            {
                if (caster != null)
                {
                    direction = GetVector(caster.Position, Position);
                    angle = (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat();
                    boltOrigin = caster.Position + (2f * direction).ToIntVec3();
                }

                maxRange = (boltOrigin - Position).LengthHorizontal;
                boltMaxCount = Mathf.RoundToInt(maxRange);
                approximateDuration = (int) (maxRange / boltTravelRate * ticksPerStrike * 1.25f);
                boltMesh = RandomBoltMesh;
                initialized = true;
            }

            if (nextStrike < age && age < duration && !wallImpact)
            {
                strikeCount++;
                boltMesh = null;
                boltPosition = boltOrigin + (boltTravelRate * strikeCount * direction).ToIntVec3();
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
                        smallMeshDestination = boltPosition +
                                               (Quaternion.AngleAxis(Rand.Range(30, 60), Vector3.up) * direction *
                                                Rand.Range(2f, 5f)).ToIntVec3();
                    }
                    else
                    {
                        smallMeshDestination = boltPosition +
                                               (Quaternion.AngleAxis(Rand.Range(-30, -60), Vector3.up) * direction *
                                                Rand.Range(2f, 5f)).ToIntVec3();
                    }

                    Map.weatherManager.eventHandler.AddEvent(new MeshMaker(Map, MatPool.rendEarthMat7, boltPosition,
                        smallMeshDestination, Rand.Range(2f, 6f), AltitudeLayer.Floor, approximateDuration - age,
                        fadeTicks, 10));
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

        private void DoQuakeDamages(float radius, IntVec3 location)
        {
            var num = GenRadial.NumCellsInRadius(radius);
            for (var i = 0; i < num; i++)
            {
                var intVec = location + GenRadial.RadialPattern[i];
                if (!intVec.IsValid || !intVec.InBounds(Map))
                {
                    continue;
                }

                if (Rand.Chance(.4f))
                {
                    EffectMaker.MakeEffect(ThingDef.Named("Mote_ThickDust"), intVec.ToVector3Shifted(), Map,
                        Rand.Range(.2f, 2f), Rand.Range(0, 360), Rand.Range(.5f, 1f), Rand.Range(10, 250), 0,
                        Rand.Range(.3f, .9f), Rand.Range(.05f, .3f), Rand.Range(.6f, 2.4f), true);
                }

                var structure = intVec.GetFirstBuilding(Map);
                if (structure != null)
                {
                    if (structure.def.designationCategory == DesignationCategoryDefOf.Structure)
                    {
                        DamageEntities(structure, structure.def.BaseMaxHitPoints, DamageDefOf.Crush);
                        var moteDirection = GetVector(origin.ToIntVec3(), intVec);
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map,
                            Rand.Range(.3f, .5f),
                            (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 8f, 0);
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map,
                            Rand.Range(.3f, .6f),
                            (Quaternion.AngleAxis(Rand.Range(-70, -110), Vector3.up) * moteDirection).ToAngleFlat(),
                            6f, 0);
                        GenExplosion.DoExplosion(intVec, Map, .4f, WizardryDefOf.LotRW_RockFragments, launcher,
                            Rand.Range(6, 16), 0, SoundDefOf.Pawn_Melee_Punch_HitBuilding, null, null, null,
                            ThingDef.Named("Filth_RubbleRock"), .4f);
                        FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), Map, Rand.Range(.6f, 1f));
                        if (intVec == boltPosition)
                        {
                            wallImpact = true;
                            duration = approximateDuration;
                        }
                    }
                    else if (structure.def.building.isResourceRock)
                    {
                        var yieldThing = structure.def.building.mineableThing;
                        var yieldAmount = (int) (structure.def.building.mineableYield * Rand.Range(.7f, .9f));
                        DamageEntities(structure, structure.def.BaseMaxHitPoints, DamageDefOf.Crush);
                        var moteDirection = GetVector(origin.ToIntVec3(), intVec);
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map,
                            Rand.Range(.3f, .5f),
                            (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 8f, 0);
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map,
                            Rand.Range(.3f, .6f),
                            (Quaternion.AngleAxis(Rand.Range(-70, -110), Vector3.up) * moteDirection).ToAngleFlat(),
                            6f, 0);
                        GenExplosion.DoExplosion(intVec, Map, .4f, WizardryDefOf.LotRW_RockFragments, launcher,
                            Rand.Range(6, 16), 0, SoundDefOf.Crunch, null, null, null, yieldThing, 1f, yieldAmount);
                        FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), Map, Rand.Range(.6f, 1f));
                        if (intVec == boltPosition)
                        {
                            wallImpact = true;
                            duration = approximateDuration;
                        }
                    }
                    else if (structure.def.building.isNaturalRock)
                    {
                        var yieldThing = structure.def.building.mineableThing;
                        DamageEntities(structure, structure.def.BaseMaxHitPoints, DamageDefOf.Crush);
                        var moteDirection = GetVector(origin.ToIntVec3(), intVec);
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map,
                            Rand.Range(.3f, .5f),
                            (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 8f, 0);
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map,
                            Rand.Range(.3f, .6f),
                            (Quaternion.AngleAxis(Rand.Range(-70, -110), Vector3.up) * moteDirection).ToAngleFlat(),
                            6f, 0);
                        GenExplosion.DoExplosion(intVec, Map, .4f, WizardryDefOf.LotRW_RockFragments, launcher,
                            Rand.Range(6, 16), 0, SoundDefOf.Crunch, null, null, null, yieldThing, .2f);
                        FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), Map, Rand.Range(.6f, 1f));
                        if (intVec == boltPosition)
                        {
                            wallImpact = true;
                            duration = approximateDuration;
                        }
                    }
                    else
                    {
                        DamageEntities(structure, Rand.Range(40, 50), DamageDefOf.Crush);
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map,
                            Rand.Range(.3f, .6f), Rand.Range(0, 359), 6f, 0);
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), intVec.ToVector3Shifted(), Map,
                            Rand.Range(.3f, .6f), Rand.Range(0, 359), 4f, 0);
                        FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), Map, Rand.Range(.6f, 1f));
                    }
                }

                var pawn = intVec.GetFirstPawn(Map);
                if (pawn == null || pawn == caster)
                {
                    continue;
                }

                if (Rand.Chance(.2f))
                {
                    DamageEntities(pawn, Rand.Range(4, 6), DamageDefOf.Crush);
                }

                HealthUtility.AdjustSeverity(pawn, HediffDef.Named("LotRW_Quake"), Rand.Range(1f, 2f));
            }
        }

        public void DrawStrike(IntVec3 start, Vector3 dest)
        {
            if (boltOrigin == default)
            {
                return;
            }

            var magnitude = (boltPosition.ToVector3Shifted() - Find.Camera.transform.position).magnitude;
            if (age <= duration)
            {
                Find.CameraDriver.shaker.DoShake(20 / magnitude);
                Graphics.DrawMesh(boltMesh, boltOrigin.ToVector3ShiftedWithAltitude(AltitudeLayer.Floor),
                    Quaternion.Euler(0f, angle, 0f), FadedMaterialPool.FadedVersionOf(MatPool.rendEarthMat7, 1), 0);
            }
            else
            {
                Graphics.DrawMesh(boltMesh, boltOrigin.ToVector3ShiftedWithAltitude(AltitudeLayer.Floor),
                    Quaternion.Euler(0f, angle, 0f),
                    FadedMaterialPool.FadedVersionOf(MatPool.rendEarthMat7, MeshBrightness), 0);
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            var heading = (objectPos - center).ToVector3();
            var headingMagnitude = heading.magnitude;
            var magnitude = heading / headingMagnitude;
            return magnitude;
        }

        public void DamageEntities(Thing e, int amt, DamageDef damageType)
        {
            var dinfo = new DamageInfo(damageType, amt);

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