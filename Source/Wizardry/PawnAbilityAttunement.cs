using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using AbilityUser;

namespace Wizardry
{
    public class PawnAbilityAttunement : PawnAbility
    {
        public CompWizardry Wizard
        {
            get
            {
                return base.Pawn.GetComp<CompWizardry>();
            }
        }

        public WizardAbilityDef AbilityDef
        {
            get
            {
                return base.Def as WizardAbilityDef;
            }
        }

        public PawnAbilityAttunement()
        {
        }

        public PawnAbilityAttunement(CompAbilityUser abilityUser) : base(abilityUser)
        {
            this.abilityUser = (abilityUser as CompWizardry);
        }

        public PawnAbilityAttunement(Pawn user, AbilityUser.AbilityDef pdef) : base(user, pdef)
        {
        }

        public PawnAbilityAttunement(AbilityData data) : base(data)
        {
        }

        public override void PostAbilityAttempt()
        {
            base.PostAbilityAttempt();
        }

        public override string PostAbilityVerbCompDesc(VerbProperties_Ability verbDef)
        {
            string text = "";
            bool flag = verbDef == null;
            string result;
            if (flag)
            {
                result = text;
            }
            else
            {
                bool flag2 = (_ = ((verbDef?.abilityDef) as WizardAbilityDef)) != null;
                if (flag2)
                {
                    StringBuilder stringBuilder = new StringBuilder();
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
            bool flag = base.CanCastPowerCheck(context, out reason);
            bool result;
            if (flag)
            {
                reason = "";
                bool flag2 = base.Def != null && (_ = (base.Def as WizardAbilityDef)) != null;
                if (flag2)
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
}
