using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    public class Mandos_FlyingObject_Haunt : ThingWithComps
    {
        private readonly int duration = 1200;
        private int age = -1;
        protected Thing assignedTarget;
        protected Pawn assignedTargetPawn;
        private int attackFrequency = 30;
        private float attackRadius = 1f;
        private bool curveDir;
        private List<Vector3> curvePoints = new List<Vector3>();
        public float curveVariance; // 0 = no curve

        public bool damageLaunched = true;
        protected Vector3 destination;
        private int destinationCurvePoint;

        private bool earlyImpact;
        public bool explosion;
        private int explosionDamage;
        protected Thing flyingThing;
        public float force = 1f;

        public DamageInfo? impactDamage;
        private DamageDef impactDamageType;
        private float impactForce;
        private float impactRadius;
        private bool isCircling;
        private bool isExplosive;
        protected Thing launcher;

        public ThingDef moteDef;
        public int moteFrequency;
        protected Vector3 origin;

        private Pawn pawn;
        private bool shiftRight;

        public float speed = 30f;

        public bool spinning;
        protected int ticksToImpact;
        protected Vector3 trueDestination;
        protected Vector3 trueOrigin;
        public int variancePoints = 20;
        public int weaponDmg = 0;

        //Magic related

        protected int StartingTicksToImpact
        {
            get
            {
                var num = Mathf.RoundToInt((origin - destination).magnitude / (speed / 100f));
                if (num < 1)
                {
                    num = 1;
                }

                return num;
            }
        }

        protected IntVec3 DestinationCell => new IntVec3(destination);

        public virtual Vector3 ExactPosition
        {
            get
            {
                var b = (destination - origin) * (1f - (ticksToImpact / (float) StartingTicksToImpact));
                return origin + b + (Vector3.up * def.Altitude);
            }
        }

        public virtual Quaternion ExactRotation => Quaternion.LookRotation(destination - origin);

        public virtual float ExactRotationAngle =>
            (Quaternion.AngleAxis(90, Vector3.up) * (destination - origin)).ToAngleFlat();

        public override Vector3 DrawPos => ExactPosition;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref origin, "origin");
            Scribe_Values.Look(ref destination, "destination");
            Scribe_Values.Look(ref ticksToImpact, "ticksToImpact");
            Scribe_Values.Look(ref age, "age", -1);
            Scribe_Values.Look(ref damageLaunched, "damageLaunched", true);
            Scribe_Values.Look(ref explosion, "explosion");
            Scribe_References.Look(ref assignedTarget, "assignedTarget");
            Scribe_References.Look(ref assignedTargetPawn, "assignedTargetPawn");
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_Deep.Look(ref flyingThing, "flyingThing");
            Scribe_References.Look(ref pawn, "pawn");
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, Rand.Range(1.2f, 1.8f));
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
            Launch(launcher, Position.ToVector3Shifted(), targ, flyingThing);
        }

        public void AdvancedLaunch(Thing launcher, ThingDef effectMote, int moteFrequencyTicks, float curveAmount,
            int variancePoints, bool shouldSpin, Vector3 origin, LocalTargetInfo targ, Thing flyingThing,
            int flyingSpeed, bool isExplosion, int attackFrequency, float attackRadius, int _impactDamage,
            float _impactRadius, DamageDef damageType, DamageInfo? newDamageInfo = null)
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

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing,
            DamageInfo? newDamageInfo = null)
        {
            var spawned = flyingThing.Spawned;
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
            if (targ.Thing != null)
            {
                assignedTarget = targ.Thing;
                if (assignedTarget is Pawn)
                {
                    assignedTargetPawn = targ.Thing as Pawn;
                }
            }

            if (targ.Cell.x > launcher.Position.x)
            {
                shiftRight = true;
            }

            speed = speed * force;
            if (Rand.Chance(.5f))
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
            var initialVector = GetVector(start, end);
            initialVector.y = 0;
            var unused = initialVector.ToAngleFlat();
            float curveAngle;
            destinationCurvePoint = 0;
            curvePoints.Clear();
            if (curveDir)
            {
                curveAngle = variance;
            }
            else
            {
                curveAngle = -1 * variance;
            }

            //calculate extra distance bolt travels around the ellipse
            var a = .5f * Vector3.Distance(start, end);
            var b = a * Mathf.Sin(.5f * Mathf.Deg2Rad * variance);
            var p = .5f * Mathf.PI * ((3 * (a + b)) - Mathf.Sqrt(((3 * a) + b) * (a + (3 * b))));

            var incrementalDistance = p / variancePoints;
            var incrementalAngle = curveAngle / variancePoints * 2;
            curvePoints.Add(start);
            for (var i = 1; i < variancePoints; i++)
            {
                curvePoints.Add(curvePoints[i - 1] +
                                (Quaternion.AngleAxis(curveAngle, Vector3.up) * initialVector * incrementalDistance));
                curveAngle -= incrementalAngle;
            }
        }

        public Vector3 GetVector(Vector3 center, Vector3 objectPos)
        {
            var heading = objectPos - center;
            var distance = heading.magnitude;
            var direction = heading / distance;
            return direction;
        }

        public override void Tick()
        {
            base.Tick();
            var exactPosition = ExactPosition;
            if (ticksToImpact >= 0 && moteDef != null && Find.TickManager.TicksGame % moteFrequency == 0)
            {
                DrawEffects(exactPosition);
            }

            if (isCircling && attackFrequency != 0 && Find.TickManager.TicksGame % attackFrequency == 0)
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
            if (!ExactPosition.InBounds(Map))
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy();
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

                if (ticksToImpact <= 0)
                {
                    if (curveVariance > 0)
                    {
                        if (curvePoints.Count - 1 > destinationCurvePoint)
                        {
                            origin = curvePoints[destinationCurvePoint];
                            destinationCurvePoint++;
                            destination = curvePoints[destinationCurvePoint];
                            ticksToImpact = StartingTicksToImpact;
                        }
                        else
                        {
                            if (DestinationCell.InBounds(Map))
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
                        if (DestinationCell.InBounds(Map))
                        {
                            Position = DestinationCell;
                        }

                        ImpactSomething();
                    }
                }
            }

            if (age > duration)
            {
                Destroy();
            }
        }

        public void NewSemiCircle()
        {
            origin = destination;
            var targetShifted = assignedTarget.DrawPos;
            if (shiftRight)
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
            var center = ExactPosition.ToIntVec3();
            var num = GenRadial.NumCellsInRadius(attackRadius);
            for (var i = 0; i < num; i++)
            {
                var intVec = center + GenRadial.RadialPattern[i];
                if (!intVec.IsValid || !intVec.InBounds(Map))
                {
                    continue;
                }

                var hitList = intVec.GetThingList(Map);
                foreach (var thing in hitList)
                {
                    if (thing is not Pawn)
                    {
                        continue;
                    }

                    if (thing.Faction != pawn.Faction)
                    {
                        DamageEntities(thing, WizardryDefOf.LotRW_HauntDD.defaultDamage,
                            WizardryDefOf.LotRW_HauntDD);
                    }
                }
            }
        }

        public override void Draw()
        {
            if (flyingThing != null)
            {
                if (flyingThing is Pawn)
                {
                    if (!DrawPos.ToIntVec3().IsValid)
                    {
                        return;
                    }

                    var thing = flyingThing as Pawn;
                    thing?.Drawer.DrawAt(DrawPos);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, DrawPos, ExactRotation, flyingThing.def.DrawMatSingle, 0);
                }
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, DrawPos, ExactRotation, flyingThing?.def.DrawMatSingle, 0);
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
                    EffectMaker.MakeEffect(moteDef, effectVec, Map, Rand.Range(.2f, .3f), ExactRotationAngle + 90,
                        Rand.Range(1, 1.5f), Rand.Range(-200, 200), .1f, 0f, Rand.Range(.2f, .25f), false);
                }
                else
                {
                    EffectMaker.MakeEffect(moteDef, effectVec, Map, Rand.Range(.2f, .3f), ExactRotationAngle - 90,
                        Rand.Range(1, 1.5f), Rand.Range(-200, 200), .1f, 0f, Rand.Range(.2f, .25f), false);
                }
            }
            else
            {
                EffectMaker.MakeEffect(moteDef, effectVec, Map, Rand.Range(.1f, .2f), ExactRotationAngle,
                    Rand.Range(10, 15), Rand.Range(-100, 100), .15f, 0f, Rand.Range(.2f, .3f), false);
            }
        }

        private void ImpactSomething()
        {
            if (assignedTarget != null)
            {
                if (assignedTarget is Pawn pawn1 && pawn1.GetPosture() != PawnPosture.Standing &&
                    (origin - destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f)
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
            if (hitThing == null)
            {
                Pawn firstOrDefault;
                if ((firstOrDefault = Position.GetThingList(Map).FirstOrDefault(x => x == assignedTarget) as Pawn) !=
                    null)
                {
                    hitThing = firstOrDefault;
                }
            }

            var hasValue = impactDamage.HasValue;
            if (hasValue)
            {
                hitThing?.TakeDamage(impactDamage.Value);
            }

            if (flyingThing is Pawn thing)
            {
                try
                {
                    SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);

                    GenSpawn.Spawn(thing, Position, Map);
                    if (earlyImpact)
                    {
                        DamageEntities(thing, impactForce, DamageDefOf.Blunt);
                        DamageEntities(thing, 2 * impactForce, DamageDefOf.Stun);
                    }

                    Destroy();
                }
                catch
                {
                    GenSpawn.Spawn(flyingThing, Position, Map);
                    var unused = flyingThing as Pawn;

                    Destroy();
                }
            }
            else
            {
                if (impactRadius > 0)
                {
                    if (isExplosive)
                    {
                        GenExplosion.DoExplosion(ExactPosition.ToIntVec3(), Map, impactRadius, impactDamageType,
                            launcher as Pawn, explosionDamage, -1, impactDamageType.soundExplosion, def, null, null,
                            null, 0f, 1, false, null, 0f, 0, 0.0f, true);
                    }
                    else
                    {
                        var num = GenRadial.NumCellsInRadius(impactRadius);
                        for (var i = 0; i < num; i++)
                        {
                            var curCell = ExactPosition.ToIntVec3() + GenRadial.RadialPattern[i];
                            var hitList = curCell.GetThingList(Map);
                            foreach (var thing1 in hitList)
                            {
                                if (thing1 is Pawn && thing1 != pawn)
                                {
                                    DamageEntities(thing1, explosionDamage, impactDamageType);
                                }
                            }
                        }
                    }
                }

                Destroy();
            }
        }

        public void DamageEntities(Thing e, float d, DamageDef type)
        {
            var amt = Mathf.RoundToInt(Rand.Range(.75f, 1.25f) * d);
            var dinfo = new DamageInfo(type, amt, 0, -1, pawn);
            if (e != null)
            {
                e.TakeDamage(dinfo);
            }
        }
    }
}