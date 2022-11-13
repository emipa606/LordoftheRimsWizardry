using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry;

[StaticConstructorOnStartup]
public class Ulmo_FlyingObject_RainDance : ThingWithComps
{
    private readonly int rotationRate = 8;

    protected Thing assignedTarget;

    private int circleIndex;

    public bool damageLaunched = true;
    protected Vector3 destination;
    private bool drafted;

    public bool explosion;

    protected Thing flyingThing;

    public DamageInfo? impactDamage;

    protected Thing launcher;
    private bool midPoint;
    protected Vector3 origin;

    private Pawn pawn;
    private bool rainStarted;
    private int rotation;
    private List<IntVec3> rotationCircle;

    protected float speed = 4f;
    protected Vector3 swapVector;
    protected int ticksToImpact;
    protected Vector3 trueOrigin;

    public int weaponDmg = 0;

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
            var b = (destination - origin) * (1f - (ticksToImpact / (float)StartingTicksToImpact));
            return origin + b + (Vector3.up * def.Altitude);
        }
    }

    public virtual Quaternion ExactRotation => Quaternion.LookRotation(destination - origin);

    public override Vector3 DrawPos => ExactPosition;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref circleIndex, "circleIndex");
        Scribe_Values.Look(ref midPoint, "midPoint");
        Scribe_Values.Look(ref rainStarted, "rainStarted");
        Scribe_Collections.Look(ref rotationCircle, "rotationCircle", LookMode.Value);
        Scribe_Values.Look(ref origin, "origin");
        Scribe_Values.Look(ref destination, "destination");
        Scribe_Values.Look(ref trueOrigin, "trueOrigin");
        Scribe_Values.Look(ref ticksToImpact, "ticksToImpact");
        Scribe_Values.Look(ref damageLaunched, "damageLaunched", true);
        Scribe_Values.Look(ref explosion, "explosion");
        Scribe_References.Look(ref assignedTarget, "assignedTarget");
        Scribe_References.Look(ref launcher, "launcher");
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_References.Look(ref flyingThing, "flyingThing");
    }

    private void Initialize()
    {
        if (pawn != null)
        {
            FleckMaker.Static(pawn.TrueCenter(), pawn.Map, FleckDefOf.ExplosionFlash, 12f);
            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
            FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, Rand.Range(1.2f, 1.8f));
        }

        var innerCircle = GenRadial.RadialCellsAround(Position,
            Mathf.RoundToInt((destination - trueOrigin).MagnitudeHorizontal() + 1), true);
        var outerCircle = GenRadial.RadialCellsAround(Position,
            Mathf.RoundToInt((destination - trueOrigin).MagnitudeHorizontal() + 2), true);
        rotationCircle = outerCircle.Except(innerCircle).ToList();
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
        if (pawn != null)
        {
            drafted = pawn.Drafted;
        }

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

        if (targ.Thing != null)
        {
            assignedTarget = targ.Thing;
        }

        destination = targ.Cell.ToVector3Shifted() +
                      new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
        ticksToImpact = StartingTicksToImpact;
    }

    public override void Tick()
    {
        base.Tick();
        var unused = ExactPosition;
        ticksToImpact--;
        if (!ExactPosition.InBounds(Map))
        {
            ticksToImpact++;
            Position = ExactPosition.ToIntVec3();
            Destroy();
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

                EffectMaker.MakeEffect(WizardryDefOf.Mote_Sparks, DrawPos, Map, Rand.Range(.3f, .5f),
                    (rotation * 90) + Rand.Range(-45, 45), Rand.Range(2, 3), Rand.Range(100, 200));
            }

            if (ticksToImpact > 0)
            {
                return;
            }

            if (DestinationCell.InBounds(Map))
            {
                Position = DestinationCell;
            }

            if (midPoint)
            {
                ImpactSomething();
            }
            else
            {
                ChangeDirection();
                if (!rainStarted)
                {
                    StartWeatherEffects();
                }
            }
        }
    }

    private void StartWeatherEffects()
    {
        var map = Map;
        rainStarted = true;
        var rainMakerDef = new WeatherDef();
        if (map.mapTemperature.OutdoorTemp < 0)
        {
            if (map.weatherManager.curWeather.defName is "SnowHard" or "SnowGentle")
            {
                rainMakerDef = WeatherDef.Named("Clear");
                map.weatherManager.TransitionTo(rainMakerDef);
            }
            else
            {
                rainMakerDef = WeatherDef.Named(Rand.Chance(.5f) ? "SnowGentle" : "SnowHard");

                map.weatherDecider.DisableRainFor(0);
                map.weatherManager.TransitionTo(rainMakerDef);
            }
        }
        else
        {
            if (map.weatherManager.curWeather.defName is "Rain" or "RainyThunderstorm" or "FoggyRain")
            {
                rainMakerDef = WeatherDef.Named("Clear");
                map.weatherDecider.DisableRainFor(4000);
                map.weatherManager.TransitionTo(rainMakerDef);
            }
            else
            {
                var rnd = Rand.RangeInclusive(1, 3);
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
        if (circleIndex >= rotationCircle.Count)
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
        if (flyingThing != null)
        {
            switch (rotation)
            {
                case 0:
                    flyingThing.Rotation = Rot4.West;
                    break;
                case 1:
                    flyingThing.Rotation = Rot4.North;
                    break;
                case 2:
                    flyingThing.Rotation = Rot4.East;
                    break;
                default:
                    flyingThing.Rotation = Rot4.South;
                    break;
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

        SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
        GenSpawn.Spawn(flyingThing, Position, Map);
        if (flyingThing is Pawn { IsColonist: true } p)
        {
            p.drafter.Drafted = drafted;
        }

        Destroy();
    }
}