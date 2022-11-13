using AbilityUser;
using RimWorld;
using Verse;
using Verse.AI;

namespace Wizardry;

public class Nienna_Verb_HealingTouch : Verb_UseAbility
{
    protected override bool TryCastShot()
    {
        if (currentTarget.Thing is Pawn)
        {
            var pawn = CasterPawn;

            if (!pawn.DestroyedOrNull() && !pawn.Dead && pawn.RaceProps.IsFlesh)
            {
                var job = new Job(WizardryDefOf.JobDriver_HealingTouch, currentTarget, pawn);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
            else
            {
                Messages.Message("pawn is incapable of being healed", MessageTypeDefOf.RejectInput);
            }
        }
        else
        {
            Messages.Message("invalid target for healing touch", MessageTypeDefOf.RejectInput);
        }

        Ability.PostAbilityAttempt();
        burstShotsLeft = 0;
        return false;
    }
}