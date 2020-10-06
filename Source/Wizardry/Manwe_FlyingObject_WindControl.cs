using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using AbilityUser;
using UnityEngine;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    public class Manwe_FlyingObject_WindControl : ThingWithComps
    {
        protected Vector3 origin;
        protected Vector3 destination;
        private Vector3 flyingDirection = default;
        private int age = -1;
        protected int ticksToImpact;        
        protected Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;
        public bool damageLaunched = true;
        Pawn pawn;

        public DamageInfo? impactDamage;
        public int duration = 600;        
        public float speed = 25f;
        private int floatDir = 0;
        private int rotation = 0;
        private float impactForce = 0;
        private bool earlyImpact = false;
        private bool secondTarget = false;        

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector3>(ref origin, "origin", default, false);
            Scribe_Values.Look<Vector3>(ref destination, "destination", default, false);
            Scribe_Values.Look<Vector3>(ref flyingDirection, "flyingDirection", default, false);
            Scribe_Values.Look<int>(ref ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<int>(ref age, "age", 0, false);
            Scribe_Values.Look<bool>(ref damageLaunched, "damageLaunched", true, false);
            Scribe_References.Look<Thing>(ref assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Thing>(ref launcher, "launcher", false);
            Scribe_References.Look<Thing>(ref flyingThing, "flyingThing", false);
            Scribe_References.Look<Pawn>(ref pawn, "pawn", false);
        }

        protected int StartingTicksToImpact
        {
            get
            {
                int num = Mathf.RoundToInt((origin - destination).magnitude / (speed / 100f));
                bool flag = num < 1;
                if (flag)
                {
                    num = 1;
                }
                return num;
            }
        }

        protected IntVec3 DestinationCell
        {
            get
            {
                return new IntVec3(destination);
            }
        }

        public virtual Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (destination - origin) * (1f - (float)ticksToImpact / (float)StartingTicksToImpact);
                return origin + b + Vector3.up * def.Altitude;
            }
        }

        public virtual Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(destination - origin);
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                return ExactPosition;
            }
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                MoteMaker.ThrowDustPuff(pawn.Position, pawn.Map, Rand.Range(1.2f, 1.8f));
            }

            DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, (int)Mathf.Min(5f, StatDefOf.Mass.defaultBaseValue), 0, -1, pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            impactDamage = dinfo;
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            Launch(launcher, Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            Launch(launcher, Position.ToVector3Shifted(), targ, flyingThing, null);
        }

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {
            Initialize();
            bool spawned = flyingThing.Spawned;
            pawn = launcher as Pawn;
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            
            this.launcher = launcher;
            this.origin = origin;
            impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;

            CompWizardry comp = pawn.GetComp<CompWizardry>();
            if (comp.SecondTarget != null)
            {
                if (comp.SecondTarget.Thing != null)
                {
                    destination = comp.SecondTarget.Thing.Position.ToVector3Shifted();
                    assignedTarget = comp.SecondTarget.Thing;
                }
                else
                {
                    destination = comp.SecondTarget.CenterVector3;
                }
            }
            else
            {
                destination = targ.Cell.ToVector3Shifted();
            }

            ticksToImpact = StartingTicksToImpact;
        }

        public override void Tick()
        {
            base.Tick();
            age++;
            Vector3 exactPosition = ExactPosition;
            ticksToImpact--;
            bool flag = !ExactPosition.InBounds(Map);
            if (flag)
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy(DestroyMode.Vanish);
            }
            else if (!ExactPosition.ToIntVec3().Walkable(Map))
            {
                earlyImpact = true;
                impactForce = (DestinationCell - ExactPosition.ToIntVec3()).LengthHorizontal + (speed * .2f);
                ImpactSomething();
            }
            else
            {
                Position = ExactPosition.ToIntVec3();
                if (Find.TickManager.TicksGame % 3 == 0)
                {
                    MoteMaker.ThrowDustPuff(Position, Map, Rand.Range(0.8f, 1f));
                    rotation++;
                    if (rotation >= 4)
                    {
                        rotation = 0;
                    }

                    CompWizardry comp = pawn.GetComp<CompWizardry>();
                    if (comp.SecondTarget != null && !secondTarget)
                    {
                        origin = ExactPosition;
                        if (comp.SecondTarget.Thing != null)
                        {
                            destination = comp.SecondTarget.Thing.Position.ToVector3Shifted();
                            assignedTarget = comp.SecondTarget.Thing;
                        }
                        else
                        {
                            destination = comp.SecondTarget.CenterVector3;
                        }                        
                        speed = 22f;
                        ticksToImpact = StartingTicksToImpact;
                        flyingDirection = GetVector(origin.ToIntVec3(), destination.ToIntVec3());
                        comp.SecondTarget = null;
                        secondTarget = true;
                    }
                }
                if (Find.TickManager.TicksGame % 12 == 0 && secondTarget == true)
                {
                    DoFlyingObjectDamage();
                }

                 bool flag2 = ticksToImpact <= 0;
                if (flag2)
                {
                    
                    bool flag4 = age > duration;
                    if (flag4 || secondTarget)
                    {
                        bool flag3 = DestinationCell.InBounds(Map);
                        if (flag3)
                        {
                            Position = DestinationCell;
                        }
                        ImpactSomething();
                    }
                    else
                    {
                        origin = destination;
                        speed = 5f;
                        if (floatDir ==0)
                        {
                            destination.x += -.25f;
                            destination.z += .25f;
                        }
                        else if (floatDir == 1)
                        {
                            destination.x += .25f;
                            destination.z += .25f;
                        }
                        else if(floatDir == 2)
                        {
                            destination.x += .25f;
                            destination.z += -.25f;
                        }
                        else
                        {
                            destination.x += -.25f;
                            destination.z += -.25f;
                        }
                        floatDir++;
                        if(floatDir > 3)
                        {
                            floatDir = 0;
                        }
                        ticksToImpact = StartingTicksToImpact;
                    }
                }
            }
        }

        public void DoFlyingObjectDamage()
        {
            float radius = 1f;
            IntVec3 center = ExactPosition.ToIntVec3();
            int num = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[i];
                if (intVec.IsValid && intVec.InBounds(Map))
                {
                    List<Thing> hitList = intVec.GetThingList(Map);
                    for(int j =0; j < hitList.Count(); j++)
                    {
                        if(hitList[j] is Pawn)
                        {
                            DamageEntities(hitList[j], Rand.Range(6, 9), DamageDefOf.Crush);
                            MoteMaker.ThrowMicroSparks(hitList[j].DrawPos, hitList[j].Map);
                        }
                        else if(hitList[j] is Building)
                        {
                            DamageEntities(hitList[j], Rand.Range(8, 16), DamageDefOf.Crush);
                            MoteMaker.ThrowMicroSparks(hitList[j].DrawPos, hitList[j].Map);
                        }
                        
                    }
                }
            }
        }

        public override void Draw()
        {
            bool flag = flyingThing != null;
            if (flag)
            {
                if (rotation == 0)
                {
                    flyingThing.Rotation = Rot4.West;
                }
                else if (rotation == 1)
                {
                    flyingThing.Rotation = Rot4.North;
                }
                else if (rotation == 2)
                {
                    flyingThing.Rotation = Rot4.East;
                }
                else
                {
                    flyingThing.Rotation = Rot4.South;
                }

                bool flag2 = flyingThing is Pawn;
                if (flag2)
                {
                    Vector3 arg_2B_0 = DrawPos;
                    bool flag4 = !DrawPos.ToIntVec3().IsValid;
                    if (flag4)
                    {
                        return;
                    }
                    Pawn pawn = flyingThing as Pawn;
                    pawn.Drawer.DrawAt(DrawPos);

                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, DrawPos, ExactRotation, flyingThing.def.DrawMatSingle, 0);
                }
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, DrawPos, ExactRotation, flyingThing.def.DrawMatSingle, 0);
            }
            Comps_PostDraw();
        }

        private void DrawEffects(Vector3 pawnVec, Pawn flyingPawn, int magnitude)
        {
            bool flag = !pawn.Dead && !pawn.Downed;
            if (flag)
            {

            }
        }

        private void ImpactSomething()
        {
            bool flag = assignedTarget != null;
            if (flag)
            {
                bool flag2 = assignedTarget is Pawn pawn && pawn.GetPosture() != PawnPosture.Standing && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f;
                if (flag2)
                {
                    Impact(null);
                }
                else
                {
                    Impact(assignedTarget);
                }
            }
            else
            {
                Impact(null);
            }
        }

        protected virtual void Impact(Thing hitThing)
        {
            bool flag = hitThing == null;
            if (flag)
            {
                Pawn pawn;
                bool flag2 = (pawn = (Position.GetThingList(Map).FirstOrDefault((Thing x) => x == assignedTarget) as Pawn)) != null;
                if (flag2)
                {
                    hitThing = pawn;
                }
            }
            bool hasValue = impactDamage.HasValue;
            if (hasValue)
            {
                hitThing.TakeDamage(impactDamage.Value);
            }
            try
            {
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);

                GenSpawn.Spawn(flyingThing, Position, Map);
                if (flyingThing is Pawn)
                {
                    Pawn p = flyingThing as Pawn;
                    if (earlyImpact)
                    {
                        DamageEntities(p, impactForce, DamageDefOf.Blunt);
                        DamageEntities(p, 2 * impactForce, DamageDefOf.Stun);
                    }
                }
                else if (flyingThing.def.thingCategories != null && (flyingThing.def.thingCategories.Contains(ThingCategoryDefOf.Chunks) || flyingThing.def.thingCategories.Contains(ThingCategoryDef.Named("StoneChunks"))))
                {
                    float radius = 3f;
                    Vector3 center = ExactPosition;
                    if (earlyImpact)
                    {
                        bool wallFlag90neg = false;
                        IntVec3 wallCheck = (center + (Quaternion.AngleAxis(-90, Vector3.up) * flyingDirection)).ToIntVec3();
                        MoteMaker.ThrowMicroSparks(wallCheck.ToVector3Shifted(), Map);
                        wallFlag90neg = wallCheck.Walkable(Map);

                        wallCheck = (center + (Quaternion.AngleAxis(90, Vector3.up) * flyingDirection)).ToIntVec3();
                        MoteMaker.ThrowMicroSparks(wallCheck.ToVector3Shifted(), Map);
                        bool wallFlag90 = wallCheck.Walkable(Map);

                        if ((!wallFlag90 && !wallFlag90neg) || (wallFlag90 && wallFlag90neg))
                        {
                            //fragment energy bounces in reverse direction of travel
                            center = center + ((Quaternion.AngleAxis(180, Vector3.up) * flyingDirection) * 3);
                        }
                        else if(wallFlag90)
                        {
                            center = center + ((Quaternion.AngleAxis(90, Vector3.up) * flyingDirection) * 3);
                        }
                        else if(wallFlag90neg)
                        {
                            center = center + ((Quaternion.AngleAxis(-90, Vector3.up) * flyingDirection) * 3);
                        }
                        
                    }

                    int num = GenRadial.NumCellsInRadius(radius);
                    for (int i = 0; i < num/2; i++)
                    {
                        IntVec3 intVec = center.ToIntVec3() + GenRadial.RadialPattern[Rand.Range(1, num)];
                        if (intVec.IsValid && intVec.InBounds(Map))
                        {
                            Vector3 moteDirection = GetVector(ExactPosition.ToIntVec3(), intVec);
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), ExactPosition, Map, Rand.Range(.3f, .5f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 12f, 0);
                            GenExplosion.DoExplosion(intVec, Map, .4f, WizardryDefOf.LotRW_RockFragments, pawn, Rand.Range(6, 16), 0, SoundDefOf.Pawn_Melee_Punch_HitBuilding, null, null, null, ThingDef.Named("Filth_RubbleRock"), .6f, 1, false, null, 0f, 1, 0, false);
                            MoteMaker.ThrowSmoke(intVec.ToVector3Shifted(), Map, Rand.Range(.6f, 1f));
                        }
                    }
                    Thing p = flyingThing;
                    DamageEntities(p, 305, DamageDefOf.Blunt);
                }                
                
                Destroy(DestroyMode.Vanish);
            }
            catch
            {
                if (!flyingThing.Spawned)
                {
                    GenSpawn.Spawn(flyingThing, Position, Map);
                }

                Destroy(DestroyMode.Vanish);
            }
        }

        public void DamageEntities(Thing e, float d, DamageDef type)
        {
            int amt = Mathf.RoundToInt(Rand.Range(.75f, 1.25f) * d);
            DamageInfo dinfo = new DamageInfo(type, amt, 0, (float)-1, pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (objectPos - center).ToVector3();
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }
    }
}
