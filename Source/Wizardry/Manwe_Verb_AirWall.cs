using System;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wizardry
{
    public class Manwe_Verb_AirWall : Verb_UseAbility
    {
        private LocalTargetInfo action;
        private bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) &&
                !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                validTarg = (root - targ.Cell).LengthHorizontal < verbProps.range;
            }
            else
            {
                validTarg = false;
            }

            return validTarg;
        }

        protected override bool TryCastShot()
        {
            base.TryCastShot();
            PostCastShot(true, out var outResult);
            return outResult;
        }

        public override void PostCastShot(bool inResult, out bool outResult)
        {
            var comp = base.CasterPawn.GetComp<CompWizardry>();
            comp.SecondTarget = null;

            Find.Targeter.StopTargeting();
            BeginTargetingWithVerb(WizardryDefOf.CompVerb, WizardryDefOf.CompVerb.MainVerb.targetParams,
                delegate(LocalTargetInfo info)
                {
                    action = info;
                    comp = CasterPawn.GetComp<CompWizardry>();
                    comp.SecondTarget = info;
                }, CasterPawn);
            outResult = inResult;
        }

        public void BeginTargetingWithVerb(WizardAbilityDef verbToAdd, TargetingParameters targetParams,
            Action<LocalTargetInfo> action, Pawn caster = null, Action actionWhenFinished = null,
            Texture2D mouseAttachment = null)
        {
            Find.Targeter.targetingSource = null;
            Find.Targeter.targetingSourceAdditionalPawns = null;
            AccessTools.Field(typeof(Targeter), "action").SetValue(Find.Targeter, action);
            AccessTools.Field(typeof(Targeter), "targetParams").SetValue(Find.Targeter, targetParams);
            AccessTools.Field(typeof(Targeter), "caster").SetValue(Find.Targeter, caster);
            AccessTools.Field(typeof(Targeter), "actionWhenFinished").SetValue(Find.Targeter, actionWhenFinished);
            AccessTools.Field(typeof(Targeter), "mouseAttachment").SetValue(Find.Targeter, mouseAttachment);
        }
    }
}