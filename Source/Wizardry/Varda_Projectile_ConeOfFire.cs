using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry;

internal class Varda_Projectile_ConeOfFire : Projectile_AbilityBase
{
    private readonly float branchingFlameDropoff = 0.14f;

    private readonly int
        duration = 180; //maximum duration, should expend fireAmount before this occurs; this is a backstop/failsafe

    private readonly float fireStartChance = .25f;
    private readonly float mainFlameDropoff = 0.2f;
    private readonly int ticksPerStrike = 1; //how fast flames propogate, lower is faster

    private int age = -1;
    private Pawn caster;
    private IntVec3 centerCell;
    private Vector3 currentPos;
    private Vector3 direction;
    private Vector3 directionP;
    private float distance;
    private float fireAmount = 12; //amount of fire to expend, subtracts dropoff amounts for each cell traversed
    private bool initialized;

    //local, unsaved variables
    private int nextStrike;
    private int strikeInt;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref initialized, "initialized");
        Scribe_Values.Look(ref age, "age", -1);
        Scribe_Values.Look(ref strikeInt, "strikeInt");
        Scribe_Values.Look(ref fireAmount, "fireAmount", 20);
        Scribe_Values.Look(ref distance, "distance");
        Scribe_Values.Look(ref centerCell, "centerCell");
        Scribe_Values.Look(ref direction, "direction");
        Scribe_Values.Look(ref directionP, "directionP");
        Scribe_Values.Look(ref currentPos, "currentPos");
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!(age < duration))
        {
            base.Destroy(mode);
        }
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        base.Impact(hitThing, blockedByShield);
        caster = launcher as Pawn;
        var map = caster?.Map;

        if (!initialized)
        {
            if (caster != null)
            {
                centerCell = caster.Position;
                direction = GetVector(Position, false);
                nextStrike = age + ticksPerStrike;
                currentPos = caster.Position.ToVector3();
            }

            currentPos.y = 0;
            initialized = true;
        }

        if (age <= nextStrike || !(fireAmount > 0))
        {
            return;
        }

        currentPos += direction;
        nextStrike = age + ticksPerStrike;
        if (currentPos.ToIntVec3().GetTerrain(map).passability != Traversability.Impassable &&
            currentPos.ToIntVec3().Walkable(map))
        {
            if (caster != null && (currentPos.ToIntVec3() == caster.Position || Map == null))
            {
                return;
            }

            EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, currentPos, Map, 1f,
                (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 3f, Rand.Range(200, 500));
            EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, currentPos, Map, .8f,
                (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
            var hitList = currentPos.ToIntVec3().GetThingList(map);
            Thing burnThing;
            foreach (var thing in hitList)
            {
                burnThing = thing;
                DamageEntities(burnThing);
            }

            if (Rand.Chance(fireStartChance))
            {
                FireUtility.TryStartFireIn(currentPos.ToIntVec3(), map, .2f);
            }

            fireAmount -= mainFlameDropoff;
            strikeInt++;
            var tempVec1 = currentPos;
            IntVec3 lastVec1Pos = default;
            var tempVec2 = currentPos;
            IntVec3 lastVec2Pos = default;
            distance = Mathf.Max(5f, distance);
            for (var i = strikeInt / distance; i > .3f; i -= .5f)
            {
                tempVec1 += directionP;
                if (tempVec1.ToIntVec3() != currentPos.ToIntVec3() && tempVec1.ToIntVec3() != lastVec1Pos)
                {
                    lastVec1Pos = tempVec1.ToIntVec3();
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, tempVec1, Map, .8f,
                        (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 2f,
                        Rand.Range(200, 500));
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, tempVec1, Map, .7f,
                        (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                    hitList = lastVec1Pos.GetThingList(map);
                    foreach (var thing in hitList)
                    {
                        burnThing = thing;
                        DamageEntities(burnThing);
                    }

                    if (Rand.Chance(fireStartChance))
                    {
                        FireUtility.TryStartFireIn(lastVec1Pos, map, .2f);
                    }

                    fireAmount -= branchingFlameDropoff;
                }

                tempVec2 -= directionP;
                if (tempVec2.ToIntVec3() != currentPos.ToIntVec3() && tempVec2.ToIntVec3() != lastVec2Pos)
                {
                    lastVec2Pos = tempVec2.ToIntVec3();
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, tempVec2, Map, .8f,
                        (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 2f,
                        Rand.Range(200, 500));
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, tempVec2, Map, .7f,
                        (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                    hitList = lastVec2Pos.GetThingList(map);
                    foreach (var thing in hitList)
                    {
                        burnThing = thing;
                        DamageEntities(burnThing);
                    }

                    if (Rand.Chance(fireStartChance))
                    {
                        FireUtility.TryStartFireIn(lastVec2Pos, map, .2f);
                    }

                    fireAmount -= branchingFlameDropoff;
                }

                if (fireAmount < 0)
                {
                    i = 0;
                }
            }
        }
        else
        {
            //main branch of fire cone hit impassable or unwalkable terrain
            age = duration;
        }
    }

    public Vector3 GetVector(IntVec3 closestTarget, bool reverseDirection)
    {
        var heading = (closestTarget - caster.Position).ToVector3();
        if (reverseDirection)
        {
            heading = Quaternion.AngleAxis(180, Vector3.up) * heading;
        }

        distance = heading.magnitude;
        var dirVec = heading / distance;
        directionP = Quaternion.AngleAxis(90, Vector3.up) * dirVec;
        return dirVec;
    }

    public void DamageEntities(Thing e)
    {
        var amt = Mathf.RoundToInt(Rand.Range(def.projectile.GetDamageAmount(1) * .75f,
            def.projectile.GetDamageAmount(1) * 1.25f) + fireAmount);
        var dinfo = new DamageInfo(DamageDefOf.Flame, amt);
        e?.TakeDamage(dinfo);
    }

    public override void Tick()
    {
        base.Tick();
        age++;
    }
}