using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    internal class Manwe_Projectile_Vortex : Projectile_AbilityBase
    {
        private static readonly MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();

        private static readonly Material TornadoMaterial = MaterialPool.MatFrom("Things/Ethereal/Tornado",
            ShaderDatabase.Transparent, MapMaterialRenderQueues.Tornado);

        private readonly int fireDelay = 10;
        private readonly int leftFadeOutTicks = -1;
        private readonly int strikeNum = 4;
        private int age;
        private List<IntVec3> cellList;
        private Vector3 direction;
        private int duration = 1200;
        private float fireVortexValue;
        private bool initialized;
        private Pawn pawn;

        private float radius = 5;
        private Vector3 realPosition;
        private int strikeDelay = 20; //random 45-90 within class
        private Sustainer sustainer;

        private IEnumerable<IntVec3> targets;

        private float FadeInOutFactor
        {
            get
            {
                var a = Mathf.Clamp01(age / 120f);
                var b = leftFadeOutTicks >= 0 ? Mathf.Min(leftFadeOutTicks / 120f, 1f) : 1f;
                return Mathf.Min(a, b);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialized, "initialized", true);
            Scribe_Values.Look(ref age, "age", -1);
            Scribe_Values.Look(ref duration, "duration", 900);
            Scribe_Values.Look(ref strikeDelay, "strikeDelay");
            Scribe_Values.Look(ref fireVortexValue, "fireVortexValue");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Collections.Look(ref cellList, "cellList", LookMode.Value);
            Scribe_Values.Look(ref direction, "direction");
            Scribe_Values.Look(ref realPosition, "realPosition");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (!(age < duration))
            {
                base.Destroy(mode);
            }
        }

        public override void Tick()
        {
            base.Tick();
            age++;
        }

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);

            var unused = def;

            if (!initialized)
            {
                pawn = launcher as Pawn;
                radius = def.projectile.explosionRadius;
                initialized = true;
                if (pawn != null)
                {
                    direction = GetVector(pawn.Position, Position);
                }

                realPosition = Position.ToVector3();
                targets = GenRadial.RadialCellsAround(Position, strikeNum, false);
                cellList = targets.ToList();
                CreateSustainer();
            }

            if (sustainer == null)
            {
                Log.Error("Vortex sustainer is null.");
                CreateSustainer();
            }

            sustainer.Maintain();
            UpdateSustainerVolume();

            realPosition += direction * .1f;
            if (Map == null)
            {
                return;
            }

            if (!realPosition.ToIntVec3().Walkable(Map))
            {
                age = duration;
            }

            FleckMaker.ThrowTornadoDustPuff(realPosition, Map, Rand.Range(.6f, .9f), Color.white);
            //EffectMaker.MakeEffect(FleckDefOf.TornadoDustPuff, realPosition, Map, Rand.Range(.6f, .9f), Rand.Range(0, 360), Rand.Range(4f, 5f), Rand.Range(100, 200));
            IntVec3 curCell;
            Vector3 moteVector;
            for (var i = 0; i < 5; i++)
            {
                curCell = cellList.RandomElement();
                if (!curCell.IsValid || !curCell.InBounds(Map))
                {
                    continue;
                }

                moteVector = GetVector(realPosition.ToIntVec3(), curCell);
                EffectMaker.MakeEffect(ThingDef.Named("Mote_Tornado"), curCell.ToVector3(), Map,
                    Rand.Range(.4f, .8f),
                    (Quaternion.AngleAxis(Rand.Range(-35, -55), Vector3.up) * moteVector).ToAngleFlat(),
                    Rand.Range(1f, 3f), Rand.Range(-200, -500),
                    (Quaternion.AngleAxis(Rand.Range(-35, -55), Vector3.up) * moteVector).ToAngleFlat(),
                    Rand.Range(.2f, .3f), .1f, Rand.Range(.05f, .2f), true);
            }

            if (Find.TickManager.TicksGame % 50 == 0)
            {
                direction.x = Rand.Range(-.6f, .6f);
                direction.z = Rand.Range(-.6f, .6f);
            }

            if (Find.TickManager.TicksGame % strikeDelay == 0)
            {
                foreach (var intVec3 in cellList)
                {
                    curCell = intVec3;
                    if (!curCell.IsValid || !curCell.InBounds(Map))
                    {
                        continue;
                    }

                    fireVortexValue += CalculateFireAmountInArea(curCell, .4f);
                    var force = (10f / (curCell.ToVector3() - realPosition).magnitude) + 10f;
                    var hitList = curCell.GetThingList(Map);
                    foreach (var dmgThing in hitList)
                    {
                        var launchVector = GetVector(dmgThing.Position, realPosition.ToIntVec3());
                        var projectedPosition = dmgThing.Position + (force * launchVector).ToIntVec3();
                        if (dmgThing is Pawn victim)
                        {
                            if (!projectedPosition.IsValid || !projectedPosition.InBounds(Map) || victim.Dead)
                            {
                                continue;
                            }

                            if (fireVortexValue > 0)
                            {
                                DamageEntities(victim,
                                    Mathf.RoundToInt(def.projectile.GetDamageAmount(1) * force),
                                    DamageDefOf.Flame);
                                fireVortexValue -= .2f;
                            }

                            LaunchFlyingObect(projectedPosition, victim);
                        }
                        else if (dmgThing is Building)
                        {
                            if (!(fireVortexValue > 0))
                            {
                                continue;
                            }

                            DamageEntities(dmgThing,
                                Mathf.RoundToInt(def.projectile.GetDamageAmount(1) * force * 2),
                                DamageDefOf.Flame);
                            fireVortexValue -= .2f;
                        }
                        else if (dmgThing.def.EverHaulable && !(dmgThing is Corpse))
                        {
                            if (projectedPosition.IsValid && projectedPosition.InBounds(Map))
                            {
                                LaunchFlyingObect(projectedPosition, dmgThing);
                            }
                        }
                    }
                }

                targets = GenRadial.RadialCellsAround(realPosition.ToIntVec3(), strikeNum, true);
                cellList = targets.ToList();
            }

            if (!(fireVortexValue > 0))
            {
                return;
            }

            if (Find.TickManager.TicksGame % fireDelay != 0)
            {
                return;
            }

            curCell = cellList.RandomElement();
            moteVector = GetVector(realPosition.ToIntVec3(), curCell);
            EffectMaker.MakeEffect(ThingDef.Named("Mote_MicroSparks"),
                cellList.RandomElement().ToVector3Shifted(), Map, Rand.Range(.5f, 1f),
                (Quaternion.AngleAxis(Rand.Range(-35, -50), Vector3.up) * moteVector).ToAngleFlat(),
                Rand.Range(2, 3), Rand.Range(50, 200));
            EffectMaker.MakeEffect(ThingDef.Named("Mote_MicroSparks"),
                cellList.RandomElement().ToVector3Shifted(), Map, Rand.Range(.5f, 1f),
                (Quaternion.AngleAxis(Rand.Range(35, 50), Vector3.up) * moteVector).ToAngleFlat(),
                Rand.Range(1, 2), Rand.Range(50, 200));
            DoFireVortex();
        }

        private void DoFireVortex()
        {
            var targetsCellsSmall = GenRadial.RadialCellsAround(realPosition.ToIntVec3(), 3, true);
            var targetsCells = GenRadial.RadialCellsAround(realPosition.ToIntVec3(), 4, true).Except(targetsCellsSmall);
            var startCell = targetsCells.RandomElement();
            var moteVector = GetVector(realPosition.ToIntVec3(), startCell);
            var launchedThing = new Thing
            {
                def = WizardryDefOf.FlyingObject_StreamingFlame
            };
            LaunchFlames(targetsCellsSmall.RandomElement(), startCell + (moteVector * 4).ToIntVec3(), launchedThing);
            fireVortexValue -= .4f;
        }

        public void LaunchFlames(IntVec3 startCell, IntVec3 targetCell, Thing thing)
        {
            if (targetCell == default)
            {
                return;
            }

            if (thing == null)
            {
                return;
            }

            var flyingObject =
                (Varda_FlyingObject_StreamingFlame) GenSpawn.Spawn(
                    ThingDef.Named("FlyingObject_StreamingFlame"), startCell, Map);
            flyingObject.speed = 22;
            flyingObject.Launch(pawn, targetCell, thing);
        }

        public float CalculateFireAmountInArea(IntVec3 center, float radius)
        {
            float result = 0;
            var fireList = Map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            var targetCells = GenRadial.RadialCellsAround(center, radius, true);
            for (var i = 0; i < targetCells.Count(); i++)
            {
                var curCell = targetCells.ToArray()[i];
                if (!curCell.InBounds(Map) || !curCell.IsValid)
                {
                    continue;
                }

                foreach (var thing in fireList)
                {
                    if (thing.Position != curCell)
                    {
                        continue;
                    }

                    var fire = thing as Fire;
                    if (fire == null)
                    {
                        continue;
                    }

                    result += fire.fireSize;
                    FleckMaker.ThrowSmoke(curCell.ToVector3Shifted(), Map, fire.fireSize * 1.5f);
                    fire.Destroy();
                }
            }

            return result;
        }

        public void RemoveFireAtPosition(IntVec3 pos)
        {
            GenExplosion.DoExplosion(pos, Map, 1, DamageDefOf.Extinguish, launcher, 100, 0,
                SoundDef.Named("ExpandingFlames"), def, equipmentDef);
        }

        private float AdjustedDistanceFromCenter(float distanceFromCenter)
        {
            var num = Mathf.Min(distanceFromCenter / 8f, 1f);
            num *= num;
            return distanceFromCenter * num;
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
            flyingObject.Launch(pawn, targetCell, thing);
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            var heading = (objectPos - center).ToVector3();
            var distance = heading.magnitude;
            var getVector = heading / distance;
            return getVector;
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

        private void UpdateSustainerVolume()
        {
            sustainer.info.volumeFactor = FadeInOutFactor;
        }

        private void CreateSustainer()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                var soundDef = SoundDef.Named("Tornado");
                sustainer = soundDef.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
                UpdateSustainerVolume();
            });
        }
    }
}