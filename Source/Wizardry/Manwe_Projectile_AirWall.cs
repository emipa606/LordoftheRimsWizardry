using System;
using System.Collections.Generic;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry;

[StaticConstructorOnStartup]
public class Manwe_Projectile_AirWall : Projectile_AbilityBase
{
    //unsaved variables
    private readonly int wallLengthMax = 20;
    private int age = -1;
    private Pawn caster;
    private List<Thing> despawnedThingList = new List<Thing>();
    private int duration = 300;
    private bool initialized;
    private bool wallActive;
    private Vector3 wallDir;
    private IntVec3 wallEnd;
    private int wallLength;
    private Vector3 wallPos;
    private List<IntVec3> wallPositions = new List<IntVec3>();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref initialized, "initialized");
        Scribe_Values.Look(ref wallActive, "wallActive");
        Scribe_Values.Look(ref age, "age", -1);
        Scribe_Values.Look(ref duration, "duration", 300);
        Scribe_Values.Look(ref wallLength, "wallLength");
        Scribe_Values.Look(ref wallPos, "wallPos");
        Scribe_Values.Look(ref wallDir, "wallDir");
        Scribe_Values.Look(ref wallEnd, "wallEnd");
        Scribe_References.Look(ref caster, "caster");
        Scribe_Collections.Look(ref wallPositions, "wallPositions", LookMode.Value);
        Scribe_Collections.Look(ref despawnedThingList, "despawnedThingList", LookMode.Value);
    }

    public void BeginTargetingWithVerb(WizardAbilityDef verbToAdd, TargetingParameters targetParams,
        Action<LocalTargetInfo> action, Pawn caster = null, Action actionWhenFinished = null,
        Texture2D mouseAttachment = null)
    {
        Find.Targeter.targetingSource = null;
        Find.Targeter.targetingSourceAdditionalPawns = null;
        AccessTools.Field(typeof(Targeter), "action").SetValue(Find.Targeter, action);
        AccessTools.Field(typeof(Targeter), "targetParams").SetValue(Find.Targeter, targetParams);
        AccessTools.Field(typeof(Targeter), "caster").SetValue(Find.Targeter, caster);
        AccessTools.Field(typeof(Targeter), "actionWhenFinished").SetValue(Find.Targeter, actionWhenFinished);
        AccessTools.Field(typeof(Targeter), "mouseAttachment").SetValue(Find.Targeter, mouseAttachment);
    }

    private void GetSecondTarget()
    {
        Find.Targeter.StopTargeting();
        BeginTargetingWithVerb(WizardryDefOf.CompVerb, WizardryDefOf.CompVerb.MainVerb.targetParams,
            delegate(LocalTargetInfo info)
            {
                var comp = caster.GetComp<CompWizardry>();
                comp.SecondTarget = info;
            }, caster);
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        var unused = Map;
        base.Impact(hitThing, blockedByShield);
        var unused1 = def;
        if (!initialized)
        {
            caster = launcher as Pawn;
            GetSecondTarget();
            initialized = true;
        }

        var comp = caster?.GetComp<CompWizardry>();
        if (comp != null && !wallActive && comp.SecondTarget != null)
        {
            age = 0;
            duration = 1200;
            wallActive = true;
            wallPos = Position.ToVector3Shifted();
            wallDir = GetVector(Position.ToVector3Shifted(), comp.SecondTarget.Cell.ToVector3Shifted());
            wallEnd = comp.SecondTarget.Cell;
            comp.SecondTarget = null;
        }

        if (!wallActive)
        {
            if (Find.TickManager.TicksGame % 6 == 0)
            {
                FleckMaker.ThrowDustPuff(Position, caster.Map, Rand.Range(.6f, .9f));
            }
        }
        else
        {
            if (Find.TickManager.TicksGame % 3 != 0)
            {
                return;
            }

            if (wallLength < wallLengthMax)
            {
                var cellList = wallPos.ToIntVec3().GetThingList(caster.Map);
                var hasWall = false;
                foreach (var thing in cellList)
                {
                    if (thing.def.defName == "LotRW_WindWall")
                    {
                        hasWall = true;
                    }
                }

                if (!hasWall)
                {
                    var spawnWall = true;
                    foreach (var thing in cellList)
                    {
                        if (!thing.def.EverHaulable)
                        {
                            if (thing.def.altitudeLayer is AltitudeLayer.Building
                                or AltitudeLayer.Item or AltitudeLayer.ItemImportant)
                            {
                                //Log.Message("bypassing object and setting wall spawn to false");
                                spawnWall = false;
                            }
                            else
                            {
                                if (thing.def.defName.Contains("Mote") ||
                                    thing.def.defName == "LotRW_Projectile_AirWall")
                                {
                                    //Log.Message("avoided storing " + cellList[i].def.defName);
                                }
                                else
                                {
                                    despawnedThingList.Add(thing);
                                    thing.DeSpawn();
                                }
                            }
                        }
                        else
                        {
                            var launchDir = -90;
                            if (Rand.Chance(.5f))
                            {
                                launchDir = 90;
                            }

                            LaunchFlyingObect(
                                thing.Position + (Quaternion.AngleAxis(launchDir, Vector3.up) * wallDir)
                                .ToIntVec3(), thing);
                        }
                    }

                    if (spawnWall)
                    {
                        var tempSpawn = new SpawnThings
                        {
                            def = ThingDef.Named("LotRW_WindWall"),
                            spawnCount = 1
                        };
                        SingleSpawnLoop(tempSpawn, wallPos.ToIntVec3(), caster.Map);
                        wallLength++;
                        wallPositions.Add(wallPos.ToIntVec3());
                    }
                }

                wallPos += wallDir;

                if (!wallPos.ToIntVec3().Walkable(caster.Map) || wallPos.ToIntVec3() == wallEnd)
                {
                    wallPos -= wallDir;
                    wallLength = wallLengthMax;
                }
            }

            for (var j = 0; j < wallPositions.Count; j++)
            {
                var launchDir = Rand.Range(-100, -80);
                if (Rand.Chance(.5f))
                {
                    launchDir = Rand.Range(80, 100);
                }

                EffectMaker.MakeEffect(ThingDef.Named("Mote_DustPuff"),
                    wallPositions.RandomElement().ToVector3Shifted(), caster.Map, Rand.Range(.6f, .8f),
                    (Quaternion.AngleAxis(launchDir, Vector3.up) * wallDir).ToAngleFlat(), Rand.Range(2f, 5f),
                    Rand.Range(100, 200), .04f, .03f, .8f, false);
            }
        }
    }

    public void LaunchFlyingObect(IntVec3 targetCell, Thing thing)
    {
        if (targetCell == default)
        {
            return;
        }

        if (thing is not { Position.IsValid: true } || Destroyed || !thing.Spawned || thing.Map == null)
        {
            return;
        }

        var flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"),
            thing.Position, thing.Map);
        flyingObject.speed = 22;
        flyingObject.Launch(caster, targetCell, thing);
    }

    public void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
    {
        if (spawnables.def == null)
        {
            return;
        }

        var unused = caster.Faction;
        var spawnablesDef = spawnables.def;
        ThingDef stuff = null;
        var madeFromStuff = spawnablesDef.MadeFromStuff;
        if (madeFromStuff)
        {
            stuff = ThingDefOf.BlocksGranite;
        }

        var thing = ThingMaker.MakeThing(spawnablesDef, stuff);
        GenSpawn.Spawn(thing, position, map, Rot4.North);
    }

    public Vector3 GetVector(Vector3 casterPos, Vector3 targetPos)
    {
        var heading = targetPos - casterPos;
        var distance = heading.magnitude;
        var direction = heading / distance;
        return direction;
    }

    public override void Tick()
    {
        base.Tick();
        age++;
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (age <= duration)
        {
            return;
        }

        foreach (var newThing in despawnedThingList)
        {
            GenSpawn.Spawn(newThing, newThing.Position, Map);
        }

        base.Destroy(mode);
    }
}