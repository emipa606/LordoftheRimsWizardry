using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Wizardry;

internal class JobDriver_StormCalling : JobDriver
{
    private const TargetIndex building = TargetIndex.A;
    private readonly int duration = 1200;
    private readonly List<Pawn> targetList = new List<Pawn>();

    private int age = -1;
    private int lastStrike;
    private int ticksTillNextStrike = 120;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        var commandStorm = new Toil
        {
            initAction = delegate
            {
                if (age > duration)
                {
                    EndJobWith(JobCondition.Succeeded);
                }

                var map = pawn.Map;
                if (!(map.weatherManager.curWeather.defName is "Rain" or "RainyThunderstorm"
                        or "FoggyRain" or "SnowHard" or "SnowGentle" or "DryThunderstorm"))
                {
                    EndJobWith(JobCondition.Succeeded);
                }

                GetTargetList();
                if (targetList.Count < 1)
                {
                    EndJobWith(JobCondition.Succeeded);
                }
            },
            tickAction = delegate
            {
                if (age > lastStrike + ticksTillNextStrike)
                {
                    DoWeatherEffect();
                    ticksTillNextStrike = Rand.Range(20, 200);
                    lastStrike = age;
                }

                if (Find.TickManager.TicksGame % 4 == 0)
                {
                    float direction = Rand.Range(0, 360);
                    EffectMaker.MakeEffect(WizardryDefOf.Mote_CastingBeam, pawn.DrawPos, pawn.Map,
                        Rand.Range(.1f, .4f),
                        direction, Rand.Range(8, 10), 0, direction, 0.2f, .02f, .1f, false);
                }

                age++;
                ticksLeftThisToil = duration - age;
                if (age > duration)
                {
                    EndJobWith(JobCondition.Succeeded);
                }

                if (Map.weatherManager.curWeather.defName == "Clear")
                {
                    EndJobWith(JobCondition.Succeeded);
                }
            },
            defaultCompleteMode = ToilCompleteMode.Delay,
            defaultDuration = duration
        };
        commandStorm.WithProgressBar(TargetIndex.A, delegate
        {
            if (pawn.DestroyedOrNull() || pawn.Dead || pawn.Downed)
            {
                return 1f;
            }

            return 1f - ((float)commandStorm.actor.jobs.curDriver.ticksLeftThisToil / duration);
        }, false, 0f);
        commandStorm.AddFinishAction(delegate
        {
            //Log.Message("ending storm calling");
            //do soemthing?
        });
        yield return commandStorm;
    }

    private void GetTargetList()
    {
        var mapPawns = Map.mapPawns.AllPawnsSpawned;
        foreach (var item in mapPawns)
        {
            if (!item.DestroyedOrNull() && !item.Dead && !item.Downed &&
                item.HostileTo(pawn))
            {
                targetList.Add(item);
            }
        }
    }

    private void DoWeatherEffect()
    {
        var randomElement = targetList.RandomElement();
        var rnd = Rand.Range(0f, 1f);
        switch (rnd)
        {
            case > .8f:
                rnd = 3;
                break;
            case > .5f:
                rnd = 2;
                break;
            default:
                rnd = 1;
                break;
        }

        for (var i = 0; i < rnd; i++)
        {
            var strikeLoc = randomElement.Position;
            strikeLoc.x += Rand.Range(-2, 2);
            strikeLoc.z += Rand.Range(-2, 2);
            Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(Map, strikeLoc));
            //want a larger explosion or more effects like stun?
            //GenExplosion.DoExplosion(this.centerLocation.ToIntVec3, this.Map, this.areaRadius, DamageDefOf.Bomb, null, Rand.Range(6, 16), SoundDefOf.Thunder_OffMap, null, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);
        }
    }
}