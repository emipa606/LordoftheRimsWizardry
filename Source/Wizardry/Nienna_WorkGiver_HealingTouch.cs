using System;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using Verse.AI;

namespace Wizardry
{
    public class Nienna_WorkGiver_HealingTouch : WorkGiver_Tend
    {

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!pawn.story.traits.HasTrait(WizardryDefOf.LotRW_Istari))
            {
                return false;
            }
            CompWizardry comp = pawn.GetComp<CompWizardry>();
            PawnAbility pawnAbility = comp.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == WizardryDefOf.LotRW_Nienna_HealingTouch);
            if (!(t is Pawn pawn2))
            {
                return false;
            }
            if (def.tendToHumanlikesOnly && !pawn2.RaceProps.Humanlike)
            {
                return false;
            }
            if (def.tendToAnimalsOnly && !pawn2.RaceProps.Animal)
            {
                return false;
            }
            if (!GoodLayingStatusForTend(pawn2, pawn))
            {
                return false;
            }
            if (!HealthAIUtility.ShouldBeTendedNowByPlayer(pawn2))
            {
                return false;
            }
            if (!HasHediffInjuries(pawn2))
            {
                return false;
            }
            if (pawn == pawn2)
            {
                return false;
            }
            if (pawnAbility.CooldownTicksLeft > 0)
            {
                return false;
            }
            LocalTargetInfo target = pawn2;
            return pawn.CanReserve(target, 1, -1, null, forced);
        }

        public static bool HasHediffInjuries(Pawn pawn)
        {
            IEnumerable<Hediff_Injury> pawnInjuries = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
            return pawnInjuries.Count() > 0;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn pawn2 = t as Pawn;
            return new Job(WizardryDefOf.JobDriver_HealingTouch, pawn2, pawn);
        }
    }
}
