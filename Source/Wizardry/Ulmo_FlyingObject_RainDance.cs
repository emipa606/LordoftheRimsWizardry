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
    public class Ulmo_FlyingObject_RainDance : ThingWithComps
    {
        protected Vector3 trueOrigin;
        protected Vector3 origin;
        protected Vector3 destination;
        protected Vector3 swapVector;

        protected float speed = 4f;
        private readonly int rotationRate = 8;
        private bool midPoint = false;
        private bool drafted = false;
        private int rotation = 0;
        private bool rainStarted = false;

        private int circleIndex =0;
        List<IntVec3> rotationCircle;
        protected int ticksToImpact;

        protected Thing launcher;

        protected Thing assignedTarget;

        protected Thing flyingThing;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion = false;

        public int weaponDmg = 0;

        Pawn pawn;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref circleIndex, "circleIndex", 0, false);
            Scribe_Values.Look<bool>(ref midPoint, "midPoint", false, false);
            Scribe_Values.Look<bool>(ref rainStarted, "rainStarted", false, false);
            Scribe_Collections.Look<IntVec3>(ref rotationCircle, "rotationCircle", LookMode.Value);
            Scribe_Values.Look<Vector3>(ref origin, "origin", default, false);
            Scribe_Values.Look<Vector3>(ref destination, "destination", default, false);
            Scribe_Values.Look<Vector3>(ref trueOrigin, "trueOrigin", default, false);
            Scribe_Values.Look<int>(ref ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<bool>(ref damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref explosion, "explosion", false, false);
            Scribe_References.Look<Thing>(ref assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Thing>(ref launcher, "launcher", false);
            Scribe_References.Look<Pawn>(ref pawn, "pawn", false);
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
            }
            IEnumerable<IntVec3> innerCircle = GenRadial.RadialCellsAround(Position, Mathf.RoundToInt((destination - trueOrigin).MagnitudeHorizontal()+1), true);
            IEnumerable<IntVec3> outerCircle = GenRadial.RadialCellsAround(Position, Mathf.RoundToInt((destination - trueOrigin).MagnitudeHorizontal()+2), true);
            rotationCircle = outerCircle.Except(innerCircle).ToList<IntVec3>();
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
            drafted = pawn.Drafted;
            Initialize();
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            this.launcher = launcher;
            trueOrigin = origin;
            this.origin = origin;
            impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;

            bool flag = targ.Thing != null;
            if (flag)
            {
                assignedTarget = targ.Thing;
            }
            destination = targ.Cell.ToVector3Shifted() + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
            ticksToImpact = StartingTicksToImpact;

        }

        public override void Tick()
        {
            base.Tick();
            Vector3 exactPosition = ExactPosition;
            ticksToImpact--;
            bool flag = !ExactPosition.InBounds(Map);
            if (flag)
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy(DestroyMode.Vanish);
            }
            else
            {
                Position = ExactPosition.ToIntVec3();
                if (Find.TickManager.TicksGame % rotationRate == 0)
                {
                    rotation++;
                    if (rotation >= 4)
                    {
                        rotation = 0;
                    }                   
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_Sparks, DrawPos, Map, Rand.Range(.3f,.5f), (rotation *90) + Rand.Range(-45, 45), Rand.Range(2, 3), Rand.Range(100, 200));
                }

                bool flag2 = ticksToImpact <= 0;
                if (flag2)
                {
                    bool flag3 = DestinationCell.InBounds(Map);
                    if (flag3)
                    {
                        Position = DestinationCell;
                    }

                    if(midPoint)
                    {
                        ImpactSomething();
                    }
                    else
                    {
                        ChangeDirection();
                        if(!rainStarted)
                        {
                            StartWeatherEffects();
                        }
                    }
                }

            }
        }

        private void StartWeatherEffects()
        {
            Map map = Map;
            rainStarted = true;
            WeatherDef rainMakerDef = new WeatherDef();
            if (map.mapTemperature.OutdoorTemp < 0)
            {
                if (map.weatherManager.curWeather.defName == "SnowHard" || map.weatherManager.curWeather.defName == "SnowGentle")
                {
                    rainMakerDef = WeatherDef.Named("Clear");
                    map.weatherManager.TransitionTo(rainMakerDef);
                }
                else
                {
                    if (Rand.Chance(.5f))
                    {
                        rainMakerDef = WeatherDef.Named("SnowGentle");
                    }
                    else
                    {
                        rainMakerDef = WeatherDef.Named("SnowHard");
                    }
                    map.weatherDecider.DisableRainFor(0);
                    map.weatherManager.TransitionTo(rainMakerDef);
                }
            }
            else
            {
                if (map.weatherManager.curWeather.defName == "Rain" || map.weatherManager.curWeather.defName == "RainyThunderstorm" || map.weatherManager.curWeather.defName == "FoggyRain")
                {
                    rainMakerDef = WeatherDef.Named("Clear");
                    map.weatherDecider.DisableRainFor(4000);
                    map.weatherManager.TransitionTo(rainMakerDef);
                }
                else
                {
                    int rnd = Rand.RangeInclusive(1, 3);
                    switch (rnd)
                    {
                        case 1:
                            rainMakerDef = WeatherDef.Named("Rain");
                            break;
                        case 2:
                            rainMakerDef = WeatherDef.Named("RainyThunderstorm");
                            break;
                        case 3:
                            rainMakerDef = WeatherDef.Named("FoggyRain");
                            break;
                    }
                    map.weatherDecider.DisableRainFor(0);
                    map.weatherManager.TransitionTo(rainMakerDef);
                }
            }
        }

        private void ChangeDirection()
        {
            circleIndex++;
            if(circleIndex >= rotationCircle.Count())
            {
                origin = destination;
                destination = trueOrigin;
                ticksToImpact = StartingTicksToImpact;
                midPoint = true;
            }
            else
            {
                swapVector = destination;
                destination = rotationCircle.ToArray<IntVec3>()[circleIndex].ToVector3Shifted();
                origin = swapVector;
                ticksToImpact = StartingTicksToImpact;
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
            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
            GenSpawn.Spawn(flyingThing, Position, Map);
            Pawn p = flyingThing as Pawn;
            if (p.IsColonist)
            {                    
                p.drafter.Drafted = drafted;
            }
            Destroy(DestroyMode.Vanish);
        }
    }
}
