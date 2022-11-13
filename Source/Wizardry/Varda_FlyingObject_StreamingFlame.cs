using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Wizardry;

[StaticConstructorOnStartup]
public class Varda_FlyingObject_StreamingFlame : ThingWithComps
{
    private int age = -1;
    protected Thing assignedTarget;
    public bool damageLaunched = true;
    protected Vector3 destination;
    public int duration = 600;
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
            var b = (destination - origin) * (1f - (ticksToImpact / (float)StartingTicksToImpact));
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
            impactForce = (DestinationCell - ExactPosition.ToIntVec3()).LengthHorizontal + (speed * .2f);
            ImpactSomething();
        }
        else
        {
            Position = ExactPosition.ToIntVec3();
            if (Find.TickManager.TicksGame % 3 == 0)
            {
                EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, ExactPosition, Map, .8f,
                    (Quaternion.AngleAxis(90, Vector3.up) * flyingDirection).ToAngleFlat(), 2f, 0);
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
            var centerCell = Position;
            WizardryDefOf.SoftExplosion.PlayOneShot(new TargetInfo(Position, pawn.Map));
            for (var k = 0; k < 1; k++)
            {
                var oldExplosionCells = GenRadial.RadialCellsAround(centerCell, k, true);
                var newExplosionCells = GenRadial.RadialCellsAround(centerCell, k + 1, true);
                var explosionCells = newExplosionCells.Except(oldExplosionCells);
                for (var i = 0; i < explosionCells.Count(); i++)
                {
                    var curCell = explosionCells.ToArray()[i];
                    if (!curCell.InBounds(Map) || !curCell.IsValid)
                    {
                        continue;
                    }

                    var heading = (curCell - centerCell).ToVector3();
                    var distance = heading.magnitude;
                    var direction = heading / distance;
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, curCell.ToVector3(), Map, .8f,
                        (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 4f,
                        Rand.Range(100, 200));
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, curCell.ToVector3(), Map, .7f,
                        (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                    var hitList = curCell.GetThingList(Map);
                    foreach (var burnThing in hitList)
                    {
                        DamageEntities(burnThing, Rand.Range(6, 16), DamageDefOf.Flame);
                    }

                    if (Rand.Chance(.5f))
                    {
                        FireUtility.TryStartFireIn(curCell, Map, Rand.Range(.1f, .25f));
                    }
                }
            }

            Destroy();
        }
        catch
        {
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
            foreach (var thing in hitList)
            {
                switch (thing)
                {
                    case Pawn:
                        DamageEntities(thing, Rand.Range(5, 9), DamageDefOf.Flame);
                        FleckMaker.ThrowMicroSparks(thing.DrawPos, thing.Map);
                        break;
                    case Building:
                        DamageEntities(thing, Rand.Range(10, 22), DamageDefOf.Flame);
                        FleckMaker.ThrowMicroSparks(thing.DrawPos, thing.Map);
                        break;
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
        e?.TakeDamage(dinfo);
    }
}