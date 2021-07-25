using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry
{
    internal class Varda_Projectile_RainOfFire : Projectile_AbilityBase
    {
        private readonly int duration = 300;
        private readonly int smallStartDelay = 0;
        private readonly int smallStrikeDelay = 20;
        private int age;
        private CellRect cellRect;
        private int expandingTick;
        private bool initialized;
        private int lastStrike;
        private int lastStrikeSmall;
        private List<Skyfaller> skyfallers = new List<Skyfaller>();
        private List<Skyfaller> skyfallersSmall = new List<Skyfaller>();
        private int strikeDelay = 30; //random 45-90 within class

        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref lastStrikeSmall, "lastStrikeSmall");
            Scribe_Values.Look(ref lastStrike, "lastStrike");
            Scribe_Collections.Look(ref skyfallers, "skyfallers", LookMode.Reference);
            Scribe_Collections.Look(ref skyfallersSmall, "skyfallersSmall", LookMode.Reference);
            Scribe_Values.Look(ref cellRect, "cellRect");
        }

        public void Initialize(Map map)
        {
            cellRect = CellRect.CenteredOn(Position, (int) def.projectile.explosionRadius);
            cellRect.ClipInsideMap(map);
            initialized = true;
        }

        protected override void Impact(Thing hitThing)
        {
            var map = Map;
            base.Impact(hitThing);
            var unused = def;
            if (!initialized)
            {
                Initialize(map);
            }

            var impactPos = cellRect.RandomCell;
            if (age > lastStrike + strikeDelay && impactPos.Standable(map) && impactPos.InBounds(map))
            {
                lastStrike = age;
                strikeDelay = Rand.Range(45, 90);
                skyfallers.Add(SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("Skyfaller_RainOfFire"), impactPos, map));
                skyfallers[skyfallers.Count - 1].angle = Rand.Range(-40, 0);
            }
            else if (age > lastStrikeSmall + smallStrikeDelay && age > smallStartDelay)
            {
                lastStrikeSmall = age;
                skyfallersSmall.Add(SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("Skyfaller_RainOfFire_Small"),
                    impactPos, map));
                skyfallersSmall[skyfallersSmall.Count - 1].angle = Rand.Range(-40, 0);
            }

            for (var i = 0; i < skyfallers.Count; i++)
            {
                if (skyfallers[i].ticksToImpact != 0)
                {
                    continue;
                }

                expandingTick++;
                var centerCell = skyfallers[i].Position;
                var oldExplosionCells = GenRadial.RadialCellsAround(centerCell, expandingTick - 1, true);
                var newExplosionCells = GenRadial.RadialCellsAround(centerCell, expandingTick, true);
                var explosionCells = newExplosionCells.Except(oldExplosionCells);
                for (var j = 0; j < explosionCells.Count(); j++)
                {
                    var curCell = explosionCells.ToArray()[j];
                    if (!curCell.InBounds(map) || !curCell.IsValid)
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
                }

                if (expandingTick != 3)
                {
                    continue;
                }

                expandingTick = 0;
                skyfallers.Remove(skyfallers[i]);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (age < duration)
            {
                return;
            }

            skyfallers.Clear();
            skyfallersSmall.Clear();
            base.Destroy(mode);
        }

        public override void Tick()
        {
            base.Tick();
            age++;
        }
    }
}