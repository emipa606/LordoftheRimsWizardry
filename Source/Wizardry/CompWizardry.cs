using AbilityUser;
using Verse;

namespace Wizardry;

/// CompWizardry
public class CompWizardry : CompAbilityUser
{
    private bool doOnce = true;
    public LocalTargetInfo SecondTarget = null;

    public bool IsWizard => Pawn.IsIstari() || Pawn.IsMage();

    public override bool TryTransformPawn()
    {
        return Pawn.IsIstari() || Pawn.IsMage();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (Find.TickManager.TicksGame % 30 == 0 && doOnce)
        {
            TempResolvePowers();
        }
    }

    private void TempResolvePowers()
    {
        if (!Pawn.IsIstari() || !doOnce)
        {
            return;
        }

        RemovePawnAbility(WizardryDefOf.LotRW_Varda_FocusFlames);
        RemovePawnAbility(WizardryDefOf.LotRW_Varda_ConeOfFire);
        RemovePawnAbility(WizardryDefOf.LotRW_Varda_RainOfFire);
        RemovePawnAbility(WizardryDefOf.LotRW_Ulmo_RainDance);
        RemovePawnAbility(WizardryDefOf.LotRW_Ulmo_WolfSong);
        RemovePawnAbility(WizardryDefOf.LotRW_Ulmo_FlameSong);
        RemovePawnAbility(WizardryDefOf.LotRW_StormCalling);
        RemovePawnAbility(WizardryDefOf.LotRW_LightChaser);
        RemovePawnAbility(WizardryDefOf.LotRW_Manwe_WindControl);
        RemovePawnAbility(WizardryDefOf.LotRW_Manwe_Vortex);
        RemovePawnAbility(WizardryDefOf.LotRW_Manwe_AirWall);
        RemovePawnAbility(WizardryDefOf.LotRW_Nienna_HealingRain);
        RemovePawnAbility(WizardryDefOf.LotRW_Nienna_HealingTouch);
        RemovePawnAbility(WizardryDefOf.LotRW_Aule_RockWall);
        RemovePawnAbility(WizardryDefOf.LotRW_Aule_RendEarth);
        RemovePawnAbility(WizardryDefOf.LotRW_Mandos_Haunt);
        RemovePawnAbility(WizardryDefOf.LotRW_Mandos_Doom);
        RemovePawnAbility(WizardryDefOf.LotRW_Mandos_Darkness);

        AddPawnAbility(WizardryDefOf.LotRW_Varda_FocusFlames);
        AddPawnAbility(WizardryDefOf.LotRW_Varda_ConeOfFire);
        AddPawnAbility(WizardryDefOf.LotRW_Varda_RainOfFire);
        AddPawnAbility(WizardryDefOf.LotRW_Ulmo_RainDance);
        AddPawnAbility(WizardryDefOf.LotRW_Ulmo_WolfSong);
        AddPawnAbility(WizardryDefOf.LotRW_Ulmo_FlameSong);
        AddPawnAbility(WizardryDefOf.LotRW_LightChaser);
        AddPawnAbility(WizardryDefOf.LotRW_StormCalling);
        AddPawnAbility(WizardryDefOf.LotRW_Manwe_WindControl);
        AddPawnAbility(WizardryDefOf.LotRW_Manwe_Vortex);
        AddPawnAbility(WizardryDefOf.LotRW_Manwe_AirWall);
        AddPawnAbility(WizardryDefOf.LotRW_Nienna_HealingRain);
        AddPawnAbility(WizardryDefOf.LotRW_Nienna_HealingTouch);
        AddPawnAbility(WizardryDefOf.LotRW_Aule_RockWall);
        AddPawnAbility(WizardryDefOf.LotRW_Aule_RendEarth);
        AddPawnAbility(WizardryDefOf.LotRW_Mandos_Haunt);
        AddPawnAbility(WizardryDefOf.LotRW_Mandos_Doom);
        AddPawnAbility(WizardryDefOf.LotRW_Mandos_Darkness);
        doOnce = false;
    }
}