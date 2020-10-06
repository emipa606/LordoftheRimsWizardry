using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using AbilityUser;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    public class Ulmo_FlyingObject_WolfSong : ThingWithComps
    {
        protected Vector3 origin;
        protected Vector3 destination;
        private float angle;
        Vector3 direction = default;
        Vector3 directionP = default;
        IEnumerable<IntVec3> lastRadial;

        protected float speed = 20f;
        protected int ticksToImpact;

        protected Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;
        Pawn pawn;

        public DamageInfo? impactDamage;
        public bool damageLaunched = true;
        public bool explosion = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector3>(ref origin, "origin", default, false);
            Scribe_Values.Look<Vector3>(ref destination, "destination", default, false);
            Scribe_Values.Look<Vector3>(ref direction, "direction", default, false);
            Scribe_Values.Look<int>(ref ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<float>(ref angle, "angle", 0, false);
            Scribe_Values.Look<float>(ref speed, "speed", 20, false);
            Scribe_Values.Look<bool>(ref damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref explosion, "explosion", false, false);
            Scribe_References.Look<Pawn>(ref pawn, "pawn", false);
            Scribe_References.Look<Thing>(ref assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Thing>(ref launcher, "launcher", false);
            Scribe_References.Look<Thing>(ref flyingThing, "flyingThing", false);
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
                MoteMaker.MakeStaticMote(pawn.TrueCenter(), pawn.Map, ThingDefOf.Mote_ExplosionFlash, 12f);
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                MoteMaker.ThrowDustPuff(pawn.Position, pawn.Map, Rand.Range(1.2f, 1.8f));
                GetVector();
                angle = (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat();
            }
        }

        public void GetVector()
        {
            Vector3 heading = (destination - ExactPosition);
            float distance = heading.magnitude;
            direction = heading / distance;
            directionP = Quaternion.AngleAxis(90, Vector3.up) * direction;
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
            bool spawned = flyingThing.Spawned;
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
            bool flag = targ.Thing != null;
            if (flag)
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
                IEnumerable<IntVec3> effectRadial = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 2, true);
                DrawEffects(effectRadial);
                DoEffects(effectRadial);
                lastRadial = effectRadial;
            }
            ticksToImpact--;
            Position = ExactPosition.ToIntVec3();
            bool flag = !ExactPosition.InBounds(Map);
            if (flag)
            {
                ticksToImpact++;
                Destroy(DestroyMode.Vanish);
            }
            else
            {
                bool flag2 = ticksToImpact <= 0;
                if (flag2)
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

        public void DrawEffects(IEnumerable<IntVec3> effectRadial)
        {
            if (lastRadial != null)
            {
                IntVec3 curCell = effectRadial.Except(lastRadial).RandomElement();
                
                bool flag2 = Find.TickManager.TicksGame % 3 == 0;                
                float fadeIn = .2f;
                float fadeOut = .25f;
                float solidTime = .05f;
                if (direction.ToAngleFlat() >= -135 && direction.ToAngleFlat() < -45)
                {
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_North, curCell.ToVector3(), Map, .8f, angle + Rand.Range(-20, 20), Rand.Range(5, 8), 0, solidTime, fadeOut, fadeIn, true);
                    if(flag2)
                    {
                        IEnumerable<IntVec3> effectRadialSmall = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 1, true);
                        curCell = effectRadialSmall.RandomElement();
                        EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_North, curCell.ToVector3(), Map, .8f, angle + Rand.Range(-20, 20), Rand.Range(10, 15), 0, solidTime, fadeOut, fadeIn, true);
                    }
                }
                else if (direction.ToAngleFlat() >= 45 && direction.ToAngleFlat() < 135)
                {
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_South, curCell.ToVector3(), Map, .8f, angle + Rand.Range(-20, 20), Rand.Range(5, 8), 0, solidTime, fadeOut, fadeIn, true);
                    if (flag2)
                    {
                        IEnumerable<IntVec3> effectRadialSmall = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 1, true);
                        curCell = effectRadialSmall.RandomElement();
                        EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_South, curCell.ToVector3(), Map, .8f, angle + Rand.Range(-20, 20), Rand.Range(10, 15), 0, solidTime, fadeOut, fadeIn, true);
                    }
                }
                else if (direction.ToAngleFlat() >= -45 && direction.ToAngleFlat() < 45)
                {
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_East, curCell.ToVector3(), Map, .8f, angle + Rand.Range(-20, 20), Rand.Range(5, 8), 0, solidTime, fadeOut, fadeIn, true);
                    if (flag2)
                    {
                        IEnumerable<IntVec3> effectRadialSmall = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 1, true);
                        curCell = effectRadialSmall.RandomElement();
                        EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_East, curCell.ToVector3(), Map, .8f, angle + Rand.Range(-20, 20), Rand.Range(10, 15), 0, solidTime, fadeOut, fadeIn, true);
                    }
                }
                else
                {
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_West, curCell.ToVector3(), Map, .8f, angle + Rand.Range(-20, 20), Rand.Range(5, 8), 0, solidTime, fadeOut, fadeIn, true);
                    if (flag2)
                    {
                        IEnumerable<IntVec3> effectRadialSmall = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 1, true);
                        curCell = effectRadialSmall.RandomElement();
                        EffectMaker.MakeEffect(WizardryDefOf.Mote_WolfSong_West, curCell.ToVector3(), Map, .8f, angle + Rand.Range(-20, 20), Rand.Range(10, 15), 0, solidTime, fadeOut, fadeIn, true);
                    }
                }
            
                curCell = lastRadial.RandomElement();
                if (curCell.InBounds(Map) && curCell.IsValid)
                {
                    ThingDef moteSmoke = ThingDef.Named("Mote_Smoke");
                    if (Rand.Chance(.5f))
                    {
                        EffectMaker.MakeEffect(moteSmoke, curCell.ToVector3(), Map, Rand.Range(.8f, 1.3f), direction.ToAngleFlat(), Rand.Range(.4f, .6f), Rand.Range(-2, 2));
                    }
                    else
                    {
                        EffectMaker.MakeEffect(moteSmoke, curCell.ToVector3(), Map, Rand.Range(.8f, 1.3f), 180+direction.ToAngleFlat(), Rand.Range(.4f, .6f), Rand.Range(-2, 2));
                    }
                }                
            }
        }

        public void DoEffects(IEnumerable<IntVec3> effectRadial1)
        {
            IEnumerable<IntVec3> effectRadial2 = GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), 1, true);
            IEnumerable<IntVec3> effectRadial = effectRadial1.Except(effectRadial2);
            IntVec3 curCell;
            List<Thing> hitList = new List<Thing>();
            for (int i = 0; i < effectRadial.Count(); i++)
            {
                curCell = effectRadial.ToArray<IntVec3>()[i];
                if (curCell.InBounds(Map) && curCell.IsValid)
                {
                    AddSnowRadial(curCell, Map, 1, Rand.Range(.1f, .5f));
                    hitList = curCell.GetThingList(Map);
                    for (int j = 0; j < hitList.Count; j++)
                    {
                        if(hitList[j] is Pawn && hitList[j] != pawn)
                        {
                            DamageEntities(hitList[j]);
                        }                        
                    }
                }
            }
        }

        public void DamageEntities(Thing e)
        {
            int amt = Mathf.RoundToInt(Rand.Range(def.projectile.GetDamageAmount(1, null) * .75f, def.projectile.GetDamageAmount(1, null) * 1.25f));
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Stun, amt, 0, (float)-1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            DamageInfo dinfo2 = new DamageInfo(DamageDefOf.Frostbite, Mathf.RoundToInt(amt * .1f), 0, (float)-1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
                e.TakeDamage(dinfo2);
            }
        }

        public static void AddSnowRadial(IntVec3 center, Map map, float radius, float depth)
        {
            int num = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[i];
                if (intVec.InBounds(map))
                {
                    float lengthHorizontal = (center - intVec).LengthHorizontal;
                    float num2 = 1f - lengthHorizontal / radius;
                    map.snowGrid.AddDepth(intVec, num2 * depth);

                }
            }
        }

        public override void Draw()
        {
            bool flag = flyingThing != null;
            if (flag)
            {                
                Graphics.DrawMesh(MeshPool.plane10, DrawPos, ExactRotation, flyingThing.def.DrawMatSingle, 0);
                Comps_PostDraw();
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
            Destroy(DestroyMode.Vanish);
        }
    }
}
