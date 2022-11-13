using System.Text;
using AbilityUser;
using Verse;

namespace Wizardry;

public class PawnAbilityAttunement : PawnAbility
{
    public PawnAbilityAttunement()
    {
    }

    public PawnAbilityAttunement(CompAbilityUser abilityUser) : base(abilityUser)
    {
        this.abilityUser = abilityUser as CompWizardry;
    }

    public PawnAbilityAttunement(Pawn user, AbilityDef pdef) : base(user, pdef)
    {
    }

    public PawnAbilityAttunement(AbilityData data) : base(data)
    {
    }

    public CompWizardry Wizard => Pawn.GetComp<CompWizardry>();

    public WizardAbilityDef AbilityDef => Def as WizardAbilityDef;

    public override string PostAbilityVerbCompDesc(VerbProperties_Ability verbDef)
    {
        var text = "";
        string result;
        if (verbDef == null)
        {
            result = text;
        }
        else
        {
            if ((_ = verbDef.abilityDef as WizardAbilityDef) != null)
            {
                var stringBuilder = new StringBuilder();
                text = stringBuilder.ToString();
            }

            result = text;
        }

        return result;
    }

    public override bool ShouldShowGizmo()
    {
        return true;
    }

    public override void Notify_AbilityFailed(bool refund)
    {
        base.Notify_AbilityFailed(refund);
        if (refund)
        {
        }
    }

    public override bool CanCastPowerCheck(AbilityContext context, out string reason)
    {
        var canCastPowerCheck = base.CanCastPowerCheck(context, out reason);
        bool result;
        if (canCastPowerCheck)
        {
            reason = "";
            var b = Def != null && (_ = Def as WizardAbilityDef) != null;
            if (b)
            {
            }

            result = true;
        }
        else
        {
            result = false;
        }

        return result;
    }
}