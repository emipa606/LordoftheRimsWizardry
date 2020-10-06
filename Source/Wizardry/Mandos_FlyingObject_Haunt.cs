using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using AbilityUser;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    public class Mandos_FlyingObject_Haunt : ThingWithComps
    {
        protected Vector3 origin;
        protected Vector3 destination;
        protected Vector3 trueOrigin;
        protected Vector3 trueDestination;

        public float speed = 30f;
        protected int ticksToImpact;
        protected Thing launcher;
        protected Thing assignedTarget;
        protected Pawn assignedTargetPawn = null;
        protected Thing flyingThing;

        public ThingDef moteDef = null;
        public int moteFrequency = 0;

        public bool spinning = false;
        public float curveVariance = 0; // 0 = no curve
        public int variancePoints = 20;
        private List<Vector3> curvePoints = new List<Vector3>();
        public float force = 1f;
        private int destinationCurvePoint = 0;
        private float impactRadius = 0;
        private int explosionDamage;
        private bool isExplosive = false;
        private DamageDef impactDamageType = null;
        private int attackFrequency = 30;
        private float attackRadius = 1f;        

        private bool earlyImpact = false;
        private float impactForce = 0;
        private bool isCircling = false;
        private readonly int duration = 1200;
        private int age = -1;
        private bool shiftRight = false;
        private bool curveDir = false;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;
        public bool explosion = false;
        public int weaponDmg = 0;

        Pawn pawn;

        //Magic related

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

        public virtual float ExactRotationAngle
        {
            get
            {
                return (Quaternion.AngleAxis(90, Vector3.up) * (destination - origin)).ToAngleFlat();
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                return ExactPosition;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector3>(ref origin, "origin", default, false);
            Scribe_Values.Look<Vector3>(ref destination, "destination", default, false);
            Scribe_Values.Look<int>(ref ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<int>(ref age, "age", -1, false);
            Scribe_Values.Look<bool>(ref damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref explosion, "explosion", false, false);
            Scribe_References.Look<Thing>(ref assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Pawn>(ref assignedTargetPawn, "assignedTargetPawn", false);
            Scribe_References.Look<Thing>(ref launcher, "launcher", false);
            Scribe_Deep.Look<Thing>(ref flyingThing, "flyingThing", new object[0]);
            Scribe_References.Look<Pawn>(ref pawn, "pawn", false);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                MoteMaker.ThrowDustPuff(pawn.Position, pawn.Map, Rand.Range(1.2f, 1.8f));
            }
            else
            {
                flyingThing.ThingID += Rand.Range(0, 214).ToString();
            }
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            Launch(launcher, Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            Launch(launcher, Position.ToVector3Shifted(), targ, flyingThing, null);
        }

        public void AdvancedLaunch(Thing launcher, ThingDef effectMote, int moteFrequencyTicks, float curveAmount, int variancePoints, bool shouldSpin, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, int flyingSpeed, bool isExplosion, int attackFrequency, float attackRadius, int _impactDamage, float _impactRadius, DamageDef damageType, DamageInfo? newDamageInfo = null)
        {
            explosionDamage = _impactDamage;
            isExplosive = isExplosion;
            impactRadius = _impactRadius;
            impactDamageType = damageType;
            this.attackFrequency = attackFrequency;
            this.attackRadius = attackRadius;
            moteFrequency = moteFrequencyTicks;
            moteDef = effectMote;
            curveVariance = curveAmount;
            this.variancePoints = variancePoints;
            spinning = shouldSpin;
            speed = flyingSpeed;
            curvePoints = new List<Vector3>();
            curvePoints.Clear();
            Launch(launcher, origin, targ, flyingThing, newDamageInfo);
        }

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {
            bool spawned = flyingThing.Spawned;
            pawn = launcher as Pawn;
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            this.launcher = launcher;
            trueOrigin = origin;
            trueDestination = targ.Cell.ToVector3();
            impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                assignedTarget = targ.Thing;
                if(assignedTarget is Pawn)
                {
                    assignedTargetPawn = targ.Thing as Pawn;
                }                       
            }
            if(targ.Cell.x > launcher.Position.x)
            {
                shiftRight = true;
            }
            speed = speed * force;
            if(Rand.Chance(.5f))
            {
                curveDir = true;
            }
            this.origin = origin;
            if (curveVariance > 0)
            {
                CalculateCurvePoints(trueOrigin, trueDestination, curveVariance);
                destinationCurvePoint++;
                destination = curvePoints[destinationCurvePoint];
            }
            else
            {
                destination = trueDestination;
            }
            ticksToImpact = StartingTicksToImpact;
            Initialize();
        }

        public void CalculateCurvePoints(Vector3 start, Vector3 end, float variance)
        {            
            Vector3 initialVector = GetVector(start, end);
            initialVector.y = 0;
            float initialAngle = (initialVector).ToAngleFlat();
            float curveAngle = 0;
            destinationCurvePoint = 0;
            curvePoints.Clear();            
            if (curveDir)
            {
                curveAngle = variance;
            }
            else
            {
                curveAngle = (-1) * variance;
            }
            //calculate extra distance bolt travels around the ellipse
            float a = .5f * Vector3.Distance(start, end);
            float b = a * Mathf.Sin(.5f * Mathf.Deg2Rad * variance);
            float p = .5f * Mathf.PI * (3 * (a + b) - (Mathf.Sqrt((3 * a + b) * (a + 3 * b))));

            float incrementalDistance = p / variancePoints;
            float incrementalAngle = (curveAngle / variancePoints) * 2;
            curvePoints.Add(start);
            for (int i = 1; i < variancePoints; i++)
            {
                curvePoints.Add(curvePoints[i - 1] + ((Quaternion.AngleAxis(curveAngle, Vector3.up) * initialVector) * incrementalDistance));
                curveAngle -= incrementalAngle;
            }
        }

        public Vector3 GetVector(Vector3 center, Vector3 objectPos)
        {
            Vector3 heading = (objectPos - center);
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }

        public override void Tick()
        {
            base.Tick();
            Vector3 exactPosition = ExactPosition;
            if (ticksToImpact >= 0 && moteDef != null && Find.TickManager.TicksGame % moteFrequency == 0)
            {
                DrawEffects(exactPosition);
            }
            if(isCircling && attackFrequency != 0 && Find.TickManager.TicksGame % attackFrequency == 0)
            {
                if (pawn.Destroyed || pawn.Dead)
                {
                    age = duration;
                }
                else
                {
                    DoFlyingObjectDamage();
                    if (assignedTargetPawn.Dead)
                    {
                        age = duration;
                    }
                }
            }
            ticksToImpact--;
            age++;
            bool flag = !ExactPosition.InBounds(Map);
            if (flag)
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy(DestroyMode.Vanish);
            }
            else if (!ExactPosition.ToIntVec3().Walkable(Map) && !isCircling)
            {
                earlyImpact = true;
                impactForce = (DestinationCell - ExactPosition.ToIntVec3()).LengthHorizontal + (speed * .2f);
                ImpactSomething();
            }
            else
            {
                Position = ExactPosition.ToIntVec3();

                bool flag2 = ticksToImpact <= 0;
                if (flag2)
                {
                    if (curveVariance > 0)
                    {
                        if ((curvePoints.Count() - 1) > destinationCurvePoint)
                        {
                            origin = curvePoints[destinationCurvePoint];
                            destinationCurvePoint++;
                            destination = curvePoints[destinationCurvePoint];
                            ticksToImpact = StartingTicksToImpact;
                        }
                        else
                        {
                            bool flag3 = DestinationCell.InBounds(Map);
                            if (flag3)
                            {
                                Position = DestinationCell;
                            }                            
                            isCircling = true;
                            variancePoints = 10;
                            curveVariance = 60;
                            speed = 10;
                            moteFrequency = 4;
                            NewSemiCircle();                            
                        }
                    }
                    else
                    {
                        bool flag3 = DestinationCell.InBounds(Map);
                        if (flag3)
                        {
                            Position = DestinationCell;
                        }
                        ImpactSomething();
                    }
                }
            }
            if(age > duration)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        public void NewSemiCircle()
        {
            origin = destination;
            Vector3 targetShifted = assignedTarget.DrawPos;
            if(shiftRight)
            {
                targetShifted.x += 1f;
            }
            else
            {
                targetShifted.x -= 1f;
            }
            shiftRight = !shiftRight;
            CalculateCurvePoints(origin, targetShifted, curveVariance);
            destinationCurvePoint++;
            destination = curvePoints[destinationCurvePoint];
            ticksToImpact = StartingTicksToImpact;
        }

        public void DoFlyingObjectDamage()
        {
            IntVec3 center = ExactPosition.ToIntVec3();
            int num = GenRadial.NumCellsInRadius(attackRadius);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[i];
                if (intVec.IsValid && intVec.InBounds(Map))
                {
                    List<Thing> hitList = intVec.GetThingList(Map);
                    for (int j = 0; j < hitList.Count(); j++)
                    {
                        if (hitList[j] is Pawn)
                        {
                            if (hitList[j].Faction != pawn.Faction)
                            {
                                DamageEntities(hitList[j], WizardryDefOf.LotRW_HauntDD.defaultDamage, WizardryDefOf.LotRW_HauntDD);
                            }
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

        private void DrawEffects(Vector3 effectVec)
        {
            effectVec.x += Rand.Range(-0.4f, 0.4f);
            effectVec.z += Rand.Range(-0.4f, 0.4f);
            if (isCircling)
            {
                if (shiftRight)
                {
                    EffectMaker.MakeEffect(moteDef, effectVec, Map, Rand.Range(.2f, .3f), ExactRotationAngle + 90, Rand.Range(1, 1.5f), Rand.Range(-200, 200), .1f, 0f, Rand.Range(.2f, .25f), false);
                }
                else
                {
                    EffectMaker.MakeEffect(moteDef, effectVec, Map, Rand.Range(.2f, .3f), ExactRotationAngle - 90, Rand.Range(1, 1.5f), Rand.Range(-200, 200), .1f, 0f, Rand.Range(.2f, .25f), false);
                }
            }
            else
            {
                EffectMaker.MakeEffect(moteDef, effectVec, Map, Rand.Range(.1f, .2f), ExactRotationAngle, Rand.Range(10, 15), Rand.Range(-100, 100), .15f, 0f, Rand.Range(.2f, .3f), false);
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
            if (flyingThing is Pawn)
            {
                try
                {
                    SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);

                    GenSpawn.Spawn(flyingThing, Position, Map);
                    Pawn p = flyingThing as Pawn;
                    if (earlyImpact)
                    {
                        DamageEntities(p, impactForce, DamageDefOf.Blunt);
                        DamageEntities(p, 2 * impactForce, DamageDefOf.Stun);
                    }
                    Destroy(DestroyMode.Vanish);
                }
                catch
                {
                    GenSpawn.Spawn(flyingThing, Position, Map);
                    Pawn p = flyingThing as Pawn;

                    Destroy(DestroyMode.Vanish);
                }
            }
            else
            {
                if (impactRadius > 0)
                {
                    if (isExplosive)
                    {
                        GenExplosion.DoExplosion(ExactPosition.ToIntVec3(), Map, impactRadius, impactDamageType, launcher as Pawn, explosionDamage, -1, impactDamageType.soundExplosion, def, null, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);
                    }
                    else
                    {
                        int num = GenRadial.NumCellsInRadius(impactRadius);
                        IntVec3 curCell;
                        for (int i = 0; i < num; i++)
                        {
                            curCell = ExactPosition.ToIntVec3() + GenRadial.RadialPattern[i];
                            List<Thing> hitList = new List<Thing>();
                            hitList = curCell.GetThingList(Map);
                            for (int j = 0; j < hitList.Count; j++)
                            {
                                if (hitList[j] is Pawn && hitList[j] != pawn)
                                {
                                    DamageEntities(hitList[j], explosionDamage, impactDamageType);
                                }
                            }
                        }
                    }
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
    }
}