using AbilityUser;
using RimWorld;
using Verse;
using Verse.AI;

namespace Wizardry
{
    public class Verb_StormCalling : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            var map = base.CasterPawn.Map;
            var pawn = base.CasterPawn;

            if (map.weatherManager.curWeather.defName == "Rain" ||
                map.weatherManager.curWeather.defName == "RainyThunderstorm" ||
                map.weatherManager.curWeather.defName == "FoggyRain" ||
                map.weatherManager.curWeather.defName == "SnowHard" ||
                map.weatherManager.curWeather.defName == "SnowGentle" ||
                map.weatherManager.curWeather.defName == "DryThunderstorm")
            {
                var job = new Job(WizardryDefOf.JobDriver_StormCalling);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
            else
            {
                Messages.Message("unable to call lightning under these weather conditions",
                    MessageTypeDefOf.RejectInput);
            }

            Ability.PostAbilityAttempt();
            burstShotsLeft = 0;
            return false;
        }
    }
}