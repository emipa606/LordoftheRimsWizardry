using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using UnityEngine;
using Verse;
using RimWorld;

namespace Wizardry
{
    class Varda_Projectile_RainOfFire : Projectile_AbilityBase
    {
        private int age = 0;
        private readonly int duration = 300;
        private int lastStrikeSmall = 0;
        private readonly int smallStartDelay = 0;
        private readonly int smallStrikeDelay = 20;
        private int lastStrike = 0;
        private int strikeDelay = 30; //random 45-90 within class
        private int expandingTick = 0;
        private bool initialized = false;
        private List<Skyfaller> skyfallers = new List<Skyfaller>();
        private List<Skyfaller> skyfallersSmall = new List<Skyfaller>();
        CellRect cellRect;

        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref age, "age", 0, false);
            Scribe_Values.Look<int>(ref lastStrikeSmall, "lastStrikeSmall", 0, false);
            Scribe_Values.Look<int>(ref lastStrike, "lastStrike", 0, false);
            Scribe_Collections.Look<Skyfaller>(ref skyfallers, "skyfallers", LookMode.Reference);
            Scribe_Collections.Look<Skyfaller>(ref skyfallersSmall, "skyfallersSmall", LookMode.Reference);
            Scribe_Values.Look<CellRect>(ref cellRect, "cellRect", default, false);
        }

        public void Initialize(Map map)
        {
            cellRect = CellRect.CenteredOn(Position, (int)(def.projectile.explosionRadius));
            cellRect.ClipInsideMap(map);
            initialized = true;
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            IntVec3 impactPos;
            if (!initialized)
            {
                Initialize(map);
            }

            impactPos = cellRect.RandomCell;
            if (age > (lastStrike + strikeDelay) && impactPos.Standable(map) && impactPos.InBounds(map))
            {
                lastStrike = age;
                strikeDelay = Rand.Range(45, 90);
                skyfallers.Add(SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("Skyfaller_RainOfFire"), impactPos, map));
                skyfallers[skyfallers.Count-1].angle = Rand.Range(-40, 0);
            }
            else if (age > (lastStrikeSmall + smallStrikeDelay) && age > smallStartDelay)
            {
                lastStrikeSmall = age;
                skyfallersSmall.Add(SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("Skyfaller_RainOfFire_Small"), impactPos, map));
                skyfallersSmall[skyfallersSmall.Count - 1].angle = Rand.Range(-40, 0);
            }

            for (int i =0; i < skyfallers.Count(); i++)
            {
                if (skyfallers[i].ticksToImpact == 0)
                {
                    expandingTick++;
                    IntVec3 centerCell = skyfallers[i].Position;
                    IntVec3 curCell;
                    IEnumerable<IntVec3> oldExplosionCells = GenRadial.RadialCellsAround(centerCell, expandingTick-1, true);
                    IEnumerable<IntVec3> newExplosionCells = GenRadial.RadialCellsAround(centerCell, expandingTick, true);
                    IEnumerable<IntVec3> explosionCells = newExplosionCells.Except(oldExplosionCells);
                    for (int j = 0; j < explosionCells.Count(); j++)
                    {
                        curCell = explosionCells.ToArray<IntVec3>()[j];
                        if (curCell.InBounds(map) && curCell.IsValid)
                        {
                            Vector3 heading = (curCell - centerCell).ToVector3();
                            float distance = heading.magnitude;
                            Vector3 direction = heading / distance;
                            EffectMaker.MakeEffect(WizardryDefOf.Mote_ExpandingFlame, curCell.ToVector3(), Map, .8f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 4f, Rand.Range(100, 200));
                            EffectMaker.MakeEffect(WizardryDefOf.Mote_RecedingFlame, curCell.ToVector3(), Map, .7f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 1f, 0);
                        }
                    }
                    if(expandingTick == 3)
                    {
                        expandingTick = 0;
                        skyfallers.Remove(skyfallers[i]);
                    }
                }                
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = age < duration;
            if (!flag)
            {
                skyfallers.Clear();
                skyfallersSmall.Clear();
                base.Destroy(mode);
            }
        }

        public override void Tick()
        {
            base.Tick();
            age++;
        }
    }
}