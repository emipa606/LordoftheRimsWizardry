using AbilityUser;
using RimWorld;
using Verse;

namespace Wizardry;

public class Nienna_Verb_HealingRain : Verb_UseAbility
{
    protected override bool TryCastShot()
    {
        var map = base.CasterPawn.Map;
        var centerCell = currentTarget.Cell;
        if (centerCell.IsValid && centerCell.InBounds(map))
        {
            if (map.weatherManager.curWeather.defName is "Rain" or "RainyThunderstorm" or "FoggyRain")
            {
                map.weatherDecider.DisableRainFor(0);
                map.weatherManager.TransitionTo(WizardryDefOf.LotRW_HealingRainWD);
            }
            else
            {
                Messages.Message("unable to invoke healing rain - weather is not rain or is transitioning",
                    MessageTypeDefOf.RejectInput);
            }
        }

        Ability.PostAbilityAttempt();
        burstShotsLeft = 0;
        return false;
    }
}