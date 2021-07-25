using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    public class FlyingObject_Spinning : ThingWithComps
    {
        private int age = -1;
        protected Thing assignedTarget;
        public bool damageLaunched = true;
        protected Vector3 destination;
        public int duration = 600;
        private bool earlyImpact;
        private Vector3 flyingDirection;
        protected Thing flyingThing;

        public DamageInfo? impactDamage;
        private float impactForce;
        protected Thing launcher;
        protected Vector3 origin;
        private Pawn pawn;
        private int rotation;
        public float speed = 25f;
        protected int ticksToImpact;

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

        public override Vector3 DrawPos => ExactPosition;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref origin, "origin");
            Scribe_Values.Look(ref destination, "destination");
            Scribe_Values.Look(ref flyingDirection, "flyingDirection");
            Scribe_Values.Look(ref ticksToImpact, "ticksToImpact");
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref damageLaunched, "damageLaunched", true);
            Scribe_References.Look(ref assignedTarget, "assignedTarget");
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_References.Look(ref flyingThing, "flyingThing");
            Scribe_References.Look(ref pawn, "pawn");
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, Rand.Range(1.2f, 1.8f));
            }
            //this.speed;
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            Launch(launcher, Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            Launch(launcher, Position.ToVector3Shifted(), targ, flyingThing);
        }

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing,
            DamageInfo? newDamageInfo = null)
        {
            Initialize();
            var spawned = flyingThing.Spawned;
            pawn = launcher as Pawn;
            if (spawned)
            {
                flyingThing.DeSpawn();
            }

            this.launcher = launcher;
            this.origin = origin;
            impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;


            if (targ.Thing != null)
            {
                assignedTarget = targ.Thing;
            }

            destination = targ.Cell.ToVector3Shifted();
            flyingDirection = GetVector(this.origin.ToIntVec3(), destination.ToIntVec3());
            ticksToImpact = StartingTicksToImpact;
        }

        public override void Tick()
        {
            base.Tick();
            age++;
            ticksToImpact--;
            if (!ExactPosition.InBounds(Map))
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy();
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
                    FleckMaker.ThrowDustPuff(Position, Map, Rand.Range(0.6f, .8f));
                    rotation++;
                    if (rotation >= 4)
                    {
                        rotation = 0;
                    }
                }

                if (Find.TickManager.TicksGame % 12 == 0)
                {
                    DoFlyingObjectDamage();
                }

                if (ticksToImpact > 0)
                {
                    return;
                }

                if (DestinationCell.InBounds(Map))
                {
                    Position = DestinationCell;
                }

                ImpactSomething();
            }
        }

        public override void Draw()
        {
            if (flyingThing != null)
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

        private void DrawEffects(Vector3 pawnVec, Pawn flyingPawn, int magnitude)
        {
            if (!pawn.Dead && !pawn.Downed)
            {
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

            try
            {
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);

                GenSpawn.Spawn(flyingThing, Position, Map);
                var p = flyingThing as Pawn;
                if (earlyImpact)
                {
                    DamageEntities(p, impactForce, DamageDefOf.Blunt);
                    DamageEntities(p, 2 * impactForce, DamageDefOf.Stun);
                }

                Destroy();
            }
            catch
            {
                GenSpawn.Spawn(flyingThing, Position, Map);

                Destroy();
            }
        }

        public void DoFlyingObjectDamage()
        {
            var radius = 1f;
            var center = ExactPosition.ToIntVec3();
            var num = GenRadial.NumCellsInRadius(radius);
            for (var i = 0; i < num; i++)
            {
                var intVec = center + GenRadial.RadialPattern[i];
                if (!intVec.IsValid || !intVec.InBounds(Map))
                {
                    continue;
                }

                var hitList = intVec.GetThingList(Map);
                for (var j = 0; j < hitList.Count; j++)
                {
                    if (hitList[j] is Pawn)
                    {
                        DamageEntities(hitList[j], Rand.Range(6, 9), DamageDefOf.Crush);
                        FleckMaker.ThrowMicroSparks(hitList[j].DrawPos, hitList[j].Map);
                    }
                    else if (hitList[j] is Building)
                    {
                        DamageEntities(hitList[j], Rand.Range(9, 16), DamageDefOf.Crush);
                        FleckMaker.ThrowMicroSparks(hitList[j].DrawPos, hitList[j].Map);
                    }
                }
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            var heading = (objectPos - center).ToVector3();
            var distance = heading.magnitude;
            var direction = heading / distance;
            return direction;
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