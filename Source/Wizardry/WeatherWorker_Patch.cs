using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Wizardry
{
    [HarmonyPatch(typeof(WeatherWorker), "WeatherTick", null)]
    public class WeatherWorker_Patch
    {
        public static void Postfix(WeatherManager __instance, Map map)
        {
            if (map.weatherManager.curWeather.defName != "LotRW_HealingRainWD")
            {
                return;
            }

            if (Find.TickManager.TicksGame % 10 != 0)
            {
                return;
            }

            var pawn = map.mapPawns.AllPawnsSpawned.RandomElement();
            if (pawn.Position.Roofed(map))
            {
                return;
            }

            var injuries = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
            if (injuries == null || !injuries.Any())
            {
                return;
            }

            var injury = injuries.RandomElement();
            if (!injury.CanHealNaturally() || injury.IsPermanent())
            {
                return;
            }

            injury.Heal(Rand.Range(.2f, 2f));
            if (Rand.Chance(.5f))
            {
                EffectMaker.MakeEffect(ThingDef.Named("Mote_HealingWaves"), pawn.DrawPos, map,
                    Rand.Range(.4f, .6f), 180, 1f, 0);
            }
            else
            {
                EffectMaker.MakeEffect(ThingDef.Named("Mote_HealingWaves"), pawn.DrawPos, map,
                    Rand.Range(.4f, .6f), 180, 1f, 0, 180, .1f, .02f, .19f, false);
            }
        }
    }
}