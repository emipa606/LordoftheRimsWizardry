using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    public class Manwe_FlyingObject_WindControl : ThingWithComps
    {
        private int age = -1;
        protected Thing assignedTarget;
        public bool damageLaunched = true;
        protected Vector3 destination;
        public int duration = 600;
        private bool earlyImpact;
        private int floatDir;
        private Vector3 flyingDirection;
        protected Thing flyingThing;

        public DamageInfo? impactDamage;
        private float impactForce;
        protected Thing launcher;
        protected Vector3 origin;
        private Pawn pawn;
        private int rotation;
        private bool secondTarget;
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

            var dinfo = new DamageInfo(DamageDefOf.Blunt, (int) Mathf.Min(5f, StatDefOf.Mass.defaultBaseValue), 0, -1,
                pawn);
            impactDamage = dinfo;
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

            var comp = pawn?.GetComp<CompWizardry>();
            if (comp != null && comp.SecondTarget != null)
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
            var unused = ExactPosition;
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
                    FleckMaker.ThrowDustPuff(Position, Map, Rand.Range(0.8f, 1f));
                    rotation++;
                    if (rotation >= 4)
                    {
                        rotation = 0;
                    }

                    var comp = pawn.GetComp<CompWizardry>();
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

                if (Find.TickManager.TicksGame % 12 == 0 && secondTarget)
                {
                    DoFlyingObjectDamage();
                }

                if (ticksToImpact > 0)
                {
                    return;
                }

                if (age > duration || secondTarget)
                {
                    if (DestinationCell.InBounds(Map))
                    {
                        Position = DestinationCell;
                    }

                    ImpactSomething();
                }
                else
                {
                    origin = destination;
                    speed = 5f;
                    if (floatDir == 0)
                    {
                        destination.x += -.25f;
                        destination.z += .25f;
                    }
                    else if (floatDir == 1)
                    {
                        destination.x += .25f;
                        destination.z += .25f;
                    }
                    else if (floatDir == 2)
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
                    if (floatDir > 3)
                    {
                        floatDir = 0;
                    }

                    ticksToImpact = StartingTicksToImpact;
                }
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
                foreach (var thing in hitList)
                {
                    if (thing is Pawn)
                    {
                        DamageEntities(thing, Rand.Range(6, 9), DamageDefOf.Crush);
                        FleckMaker.ThrowMicroSparks(thing.DrawPos, thing.Map);
                    }
                    else if (thing is Building)
                    {
                        DamageEntities(thing, Rand.Range(8, 16), DamageDefOf.Crush);
                        FleckMaker.ThrowMicroSparks(thing.DrawPos, thing.Map);
                    }
                }
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
                if (flyingThing is Pawn thing)
                {
                    if (earlyImpact)
                    {
                        DamageEntities(thing, impactForce, DamageDefOf.Blunt);
                        DamageEntities(thing, 2 * impactForce, DamageDefOf.Stun);
                    }
                }
                else if (flyingThing.def.thingCategories != null &&
                         (flyingThing.def.thingCategories.Contains(ThingCategoryDefOf.Chunks) ||
                          flyingThing.def.thingCategories.Contains(ThingCategoryDef.Named("StoneChunks"))))
                {
                    var radius = 3f;
                    var center = ExactPosition;
                    if (earlyImpact)
                    {
                        var wallCheck = (center + (Quaternion.AngleAxis(-90, Vector3.up) * flyingDirection))
                            .ToIntVec3();
                        FleckMaker.ThrowMicroSparks(wallCheck.ToVector3Shifted(), Map);
                        var walkable = wallCheck.Walkable(Map);

                        wallCheck = (center + (Quaternion.AngleAxis(90, Vector3.up) * flyingDirection)).ToIntVec3();
                        FleckMaker.ThrowMicroSparks(wallCheck.ToVector3Shifted(), Map);
                        var isWalkable = wallCheck.Walkable(Map);

                        if (!isWalkable && !walkable || isWalkable && walkable)
                        {
                            //fragment energy bounces in reverse direction of travel
                            center = center + (Quaternion.AngleAxis(180, Vector3.up) * flyingDirection * 3);
                        }
                        else if (isWalkable)
                        {
                            center = center + (Quaternion.AngleAxis(90, Vector3.up) * flyingDirection * 3);
                        }
                        else
                        {
                            center = center + (Quaternion.AngleAxis(-90, Vector3.up) * flyingDirection * 3);
                        }
                    }

                    var num = GenRadial.NumCellsInRadius(radius);
                    for (var i = 0; i < num / 2; i++)
                    {
                        var intVec = center.ToIntVec3() + GenRadial.RadialPattern[Rand.Range(1, num)];
                        if (!intVec.IsValid || !intVec.InBounds(Map))
                        {
                            continue;
                        }

                        var moteDirection = GetVector(ExactPosition.ToIntVec3(), intVec);
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_Rubble"), ExactPosition, Map,
                            Rand.Range(.3f, .5f),
                            (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 12f, 0);
                        GenExplosion.DoExplosion(intVec, Map, .4f, WizardryDefOf.LotRW_RockFragments, pawn,
                            Rand.Range(6, 16), 0, SoundDefOf.Pawn_Melee_Punch_HitBuilding, null, null, null,
                            ThingDef.Named("Filth_RubbleRock"), .6f);
                        FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), Map, Rand.Range(.6f, 1f));
                    }

                    var p = flyingThing;
                    DamageEntities(p, 305, DamageDefOf.Blunt);
                }

                Destroy();
            }
            catch
            {
                if (!flyingThing.Spawned)
                {
                    GenSpawn.Spawn(flyingThing, Position, Map);
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

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            var heading = (objectPos - center).ToVector3();
            var distance = heading.magnitude;
            var direction = heading / distance;
            return direction;
        }
    }
}