using System;
using System.Collections.Generic;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    public class Aule_Projectile_RockWall : Projectile_AbilityBase
    {
        //unsaved variables
        private readonly int wallLengthMax = 20;
        private readonly float wallSpawnChance = .9f;
        private int age = -1;
        private Pawn caster;
        private List<Thing> despawnedThingList = new List<Thing>();
        private int duration = 300;
        private bool initialized;
        private ThingDef spawnDef = ThingDef.Named("Sandstone");
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

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            if (!initialized)
            {
                caster = launcher as Pawn;
                GetSecondTarget();
                //todo: determine thingdef based on ground type, not random
                var rnd = Rand.Range(0f, 3f);
                if (rnd < 1)
                {
                    spawnDef = ThingDef.Named("Sandstone");
                }
                else if (rnd < 2)
                {
                    spawnDef = ThingDef.Named("Granite");
                }
                else if (rnd < 3)
                {
                    spawnDef = ThingDef.Named("Slate");
                }
                else if (rnd < 4)
                {
                    spawnDef = ThingDef.Named("Limestone");
                }
                else
                {
                    spawnDef = ThingDef.Named("Marble");
                }

                initialized = true;
            }

            var comp = caster.GetComp<CompWizardry>();
            if (!wallActive && comp.SecondTarget != null)
            {
                age = 0;
                duration = 2400;
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
                    EffectMaker.MakeEffect(ThingDef.Named("Mote_ThickDust"), Position.ToVector3Shifted(), Map,
                        Rand.Range(.4f, .6f), Rand.Range(0, 360), Rand.Range(.8f, 1.6f), Rand.Range(-20, 20), 0,
                        Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), false);
                }
            }
            else
            {
                if (wallLength >= wallLengthMax)
                {
                    return;
                }

                var magnitude = (Position.ToVector3Shifted() - Find.Camera.transform.position).magnitude;
                Find.CameraDriver.shaker.DoShake(10 / magnitude);
                for (var k = 0; k < wallLengthMax; k++)
                {
                    var cellList = wallPos.ToIntVec3().GetThingList(caster.Map);
                    var hasWall = false;
                    for (var i = 0; i < cellList.Count; i++)
                    {
                        if (cellList[i].def.designationCategory == DesignationCategoryDefOf.Structure)
                        {
                            hasWall = true;
                        }
                    }

                    if (!hasWall)
                    {
                        var spawnWall = true;
                        for (var i = 0; i < cellList.Count; i++)
                        {
                            if (!cellList[i].def.EverHaulable && !(cellList[i] is Pawn))
                            {
                                if (cellList[i].def.altitudeLayer == AltitudeLayer.Building ||
                                    cellList[i].def.altitudeLayer == AltitudeLayer.Item ||
                                    cellList[i].def.altitudeLayer == AltitudeLayer.ItemImportant)
                                {
                                    //Log.Message("bypassing object and setting wall spawn to false");
                                    spawnWall = false;
                                }
                                else
                                {
                                    if (cellList[i].def.defName.Contains("Mote") ||
                                        cellList[i].def.defName == "LotRW_Projectile_RockWall")
                                    {
                                        //Log.Message("avoided storing " + cellList[i].def.defName);
                                    }
                                    else
                                    {
                                        despawnedThingList.Add(cellList[i]);
                                        cellList[i].DeSpawn();
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
                                    cellList[i].Position + (Quaternion.AngleAxis(launchDir, Vector3.up) * wallDir)
                                    .ToIntVec3(), cellList[i]);
                            }
                        }

                        if (spawnWall && Rand.Chance(wallSpawnChance))
                        {
                            var tempSpawn = new SpawnThings
                            {
                                def = spawnDef,
                                spawnCount = 1
                            };
                            SingleSpawnLoop(tempSpawn, wallPos.ToIntVec3(), caster.Map);
                            wallLength++;
                            wallPositions.Add(wallPos.ToIntVec3());
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_ThickDust"), wallPos, Map,
                                Rand.Range(.6f, .8f), Rand.Range(0, 360), Rand.Range(1f, 2f), Rand.Range(-20, 20),
                                0, Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), false);
                        }
                    }

                    wallPos += wallDir;
                    EffectMaker.MakeEffect(ThingDef.Named("Mote_ThickDust"), wallPos, Map, Rand.Range(.6f, 1f),
                        Rand.Range(0, 360), Rand.Range(.6f, 1f), Rand.Range(-20, 20), 0, Rand.Range(.4f, .6f),
                        Rand.Range(.05f, .3f), Rand.Range(.4f, 1f), false);

                    if (wallPos.ToIntVec3() != wallEnd)
                    {
                        continue;
                    }

                    wallPos -= wallDir;
                    wallLength = wallLengthMax;
                }
            }
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Thing thing)
        {
            if (targetCell == default)
            {
                return;
            }

            if (thing == null || !thing.Position.IsValid || Destroyed || !thing.Spawned || thing.Map == null)
            {
                return;
            }

            var flyingObject = (FlyingObject_Spinning) GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"),
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

            for (var j = 0; j < wallPositions.Count; j++)
            {
                var structure = wallPositions[j].GetFirstBuilding(Map);
                if (structure == null)
                {
                    continue;
                }

                structure.Destroy();
                EffectMaker.MakeEffect(ThingDef.Named("Mote_ThickDust"), wallPositions[j].ToVector3Shifted(),
                    Map, Rand.Range(.6f, .8f), Rand.Range(0, 360), Rand.Range(.8f, 1.6f), Rand.Range(-20, 20),
                    0, Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), false);
            }

            foreach (var newThing in despawnedThingList)
            {
                GenSpawn.Spawn(newThing, newThing.Position, Map);
            }

            base.Destroy(mode);
        }
    }
}