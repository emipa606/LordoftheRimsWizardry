using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    public class Ulmo_FlyingObject_WolfSong : ThingWithComps
    {
        private float angle;
        protected Thing assignedTarget;
        public bool damageLaunched = true;
        protected Vector3 destination;
        private Vector3 direction;
        private Vector3 directionP;
        public bool explosion;
        protected Thing flyingThing;

        public DamageInfo? impactDamage;
        private IEnumerable<IntVec3> lastRadial;

        protected Thing launcher;
        protected Vector3 origin;
        private Pawn pawn;

        protected float speed = 20f;
        protected int ticksToImpact;

        protected int StartingTicksToImpact
        {
            get
            {
                var num = Mathf.RoundToInt((origin - destination).magnitude / (speed / 100f));
                var flag = num < 1;
                if (flag)
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
            Scribe_Values.Look(ref direction, "direction");
            Scribe_Values.Look(ref ticksToImpact, "ticksToImpact");
            Scribe_Values.Look(ref angle, "angle");
            Scribe_Values.Look(ref speed, "speed", 20);
            Scribe_Values.Look(ref damageLaunched, "damageLaunched", true);
            Scribe_Values.Look(ref explosion, "explosion");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref assignedTarget, "assignedTarget");
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_References.Look(ref flyingThing, "flyingThing");
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(pawn.TrueCenter(), pawn.Map, FleckDefOf.ExplosionFlash, 12f);
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, Rand.Range(1.2f, 1.8f));
                GetVector();
                angle = (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat();
            }
        }

        public void GetVector()
        {
            var heading = destination - ExactPosition;
            var distance = heading.magnitude;
            direction = heading / distance;
            directionP = Quaternion.AngleAxis(90, Vector3.up) * direction;
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
            var spawned = flyingThing.Spawned;
            pawn = launcher as Pawn;
            if (spawned)
            {
                flyingThing.DeSpawn();
            }

            this.launcher = launcher;
            this.origin = origin;
            impactDamage = newDamageInfo;
            speed = def.projectile.speed;
            this.flyingThing = flyingThing;
            if (targ.Thing != null)
            {
                assignedTarget = targ.Thing;
            }

            destination = targ.Cell.ToVector3Shifted();
            ticksToImpact = StartingTicksToImpact;
            Initialize();
        }

        public override void Tick()
        {
            base.Tick();
            if (ticksToImpact >= 0)
            {
                var effectRadial = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 2, true);
                DrawEffects(effectRadial);
                DoEffects(effectRadial);
                lastRadial = effectRadial;
            }

            ticksToImpact--;
            Position = ExactPosition.ToIntVec3();
            if (!ExactPosition.InBounds(Map))
            {
                ticksToImpact++;
                Destroy();
            }
            else
            {
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

        public void DrawEffects(IEnumerable<IntVec3> effectRadial)
        {
            if (lastRadial == null)
            {
                return;
            }

            var curCell = effectRadial.Except(lastRadial).RandomElement();

            var fadeIn = .2f;
            var fadeOut = .25f;
            var solidTime = .05f;
            if (direction.ToAngleFlat() >= -135 && direction.ToAngleFlat() < -45)
            {
                EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_North, curCell.ToVector3(), Map, .8f,
                    angle + Rand.Range(-20, 20), Rand.Range(5, 8), 0, solidTime, fadeOut, fadeIn, true);
                if (Find.TickManager.TicksGame % 3 == 0)
                {
                    var effectRadialSmall = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 1, true);
                    curCell = effectRadialSmall.RandomElement();
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_North, curCell.ToVector3(), Map, .8f,
                        angle + Rand.Range(-20, 20), Rand.Range(10, 15), 0, solidTime, fadeOut, fadeIn, true);
                }
            }
            else if (direction.ToAngleFlat() >= 45 && direction.ToAngleFlat() < 135)
            {
                EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_South, curCell.ToVector3(), Map, .8f,
                    angle + Rand.Range(-20, 20), Rand.Range(5, 8), 0, solidTime, fadeOut, fadeIn, true);
                if (Find.TickManager.TicksGame % 3 == 0)
                {
                    var effectRadialSmall = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 1, true);
                    curCell = effectRadialSmall.RandomElement();
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_South, curCell.ToVector3(), Map, .8f,
                        angle + Rand.Range(-20, 20), Rand.Range(10, 15), 0, solidTime, fadeOut, fadeIn, true);
                }
            }
            else if (direction.ToAngleFlat() >= -45 && direction.ToAngleFlat() < 45)
            {
                EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_East, curCell.ToVector3(), Map, .8f,
                    angle + Rand.Range(-20, 20), Rand.Range(5, 8), 0, solidTime, fadeOut, fadeIn, true);
                if (Find.TickManager.TicksGame % 3 == 0)
                {
                    var effectRadialSmall = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 1, true);
                    curCell = effectRadialSmall.RandomElement();
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_East, curCell.ToVector3(), Map, .8f,
                        angle + Rand.Range(-20, 20), Rand.Range(10, 15), 0, solidTime, fadeOut, fadeIn, true);
                }
            }
            else
            {
                EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_West, curCell.ToVector3(), Map, .8f,
                    angle + Rand.Range(-20, 20), Rand.Range(5, 8), 0, solidTime, fadeOut, fadeIn, true);
                if (Find.TickManager.TicksGame % 3 == 0)
                {
                    var effectRadialSmall = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 1, true);
                    curCell = effectRadialSmall.RandomElement();
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_West, curCell.ToVector3(), Map, .8f,
                        angle + Rand.Range(-20, 20), Rand.Range(10, 15), 0, solidTime, fadeOut, fadeIn, true);
                }
            }

            curCell = lastRadial.RandomElement();
            if (!curCell.InBounds(Map) || !curCell.IsValid)
            {
                return;
            }

            FleckMaker.ThrowSmoke(curCell.ToVector3(), Map, Rand.Range(.4f, .6f));
            //var moteSmoke = ThingDef.Named("Mote_Smoke");
            //if (Rand.Chance(.5f))
            //{
            //    EffectMaker.MakeEffect(moteSmoke, curCell.ToVector3(), Map, Rand.Range(.8f, 1.3f),
            //        direction.ToAngleFlat(), Rand.Range(.4f, .6f), Rand.Range(-2, 2));
            //}
            //else
            //{
            //    EffectMaker.MakeEffect(moteSmoke, curCell.ToVector3(), Map, Rand.Range(.8f, 1.3f),
            //        180 + direction.ToAngleFlat(), Rand.Range(.4f, .6f), Rand.Range(-2, 2));
            //}
        }

        public void DoEffects(IEnumerable<IntVec3> effectRadial1)
        {
            var effectRadial2 = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 1, true);
            var effectRadial = effectRadial1.Except(effectRadial2);
            for (var i = 0; i < effectRadial.Count(); i++)
            {
                var curCell = effectRadial.ToArray()[i];
                if (!curCell.InBounds(Map) || !curCell.IsValid)
                {
                    continue;
                }

                AddSnowRadial(curCell, Map, 1, Rand.Range(.1f, .5f));
                var hitList = curCell.GetThingList(Map);
                foreach (var thing in hitList)
                {
                    if (thing is Pawn && thing != pawn)
                    {
                        DamageEntities(thing);
                    }
                }
            }
        }

        public void DamageEntities(Thing e)
        {
            var amt = Mathf.RoundToInt(Rand.Range(def.projectile.GetDamageAmount(1) * .75f,
                def.projectile.GetDamageAmount(1) * 1.25f));
            var dinfo = new DamageInfo(DamageDefOf.Stun, amt);
            var dinfo2 = new DamageInfo(DamageDefOf.Frostbite, Mathf.RoundToInt(amt * .1f));
            if (e == null)
            {
                return;
            }

            e.TakeDamage(dinfo);
            e.TakeDamage(dinfo2);
        }

        public static void AddSnowRadial(IntVec3 center, Map map, float radius, float depth)
        {
            var num = GenRadial.NumCellsInRadius(radius);
            for (var i = 0; i < num; i++)
            {
                var intVec = center + GenRadial.RadialPattern[i];
                if (!intVec.InBounds(map))
                {
                    continue;
                }

                var lengthHorizontal = (center - intVec).LengthHorizontal;
                var num2 = 1f - (lengthHorizontal / radius);
                map.snowGrid.AddDepth(intVec, num2 * depth);
            }
        }

        public override void Draw()
        {
            if (flyingThing == null)
            {
                return;
            }

            Graphics.DrawMesh(MeshPool.plane10, DrawPos, ExactRotation, flyingThing.def.DrawMatSingle, 0);
            Comps_PostDraw();
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

            Destroy();
        }
    }
}