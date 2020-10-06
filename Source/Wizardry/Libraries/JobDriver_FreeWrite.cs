using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using UnityEngine;
//using VerseBase;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace Wizardry
{
    public class JobDriver_FreeWrite : JobDriver
    {
        private readonly HediffDef sanityLossHediff;
        private readonly float sanityRestoreRate = 0.1f;

        public override bool TryMakePreToilReservations(bool la)
        {
            return true;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);
            yield return Toils_Reserve.Reserve(TargetIndex.A, job.def.joyMaxParticipants);
            if (TargetB != null)
                yield return Toils_Reserve.Reserve(TargetIndex.B, 1);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
            Toil toil = new Toil();
            toil.PlaySustainerOrSound(DefDatabase<SoundDef>.GetNamed("Estate_SoundManualTypewriter"));
            toil.tickAction = delegate
            {
                pawn.rotationTracker.FaceCell(TargetA.Cell);
                pawn.GainComfortFromCellIfPossible();
                float statValue = TargetThingA.GetStatValue(StatDefOf.JoyGainFactor, true);
                float extraJoyGainFactor = statValue;
                JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.EndJob, extraJoyGainFactor);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = job.def.joyDuration;
//            toil.AddFinishAction(delegate
//            {
//                if (Cthulhu.Utility.IsCosmicHorrorsLoaded())
//                {
//                    try
//                    {
//                        if (Cthulhu.Utility.HasSanityLoss(this.pawn))
//                        {
//                            Cthulhu.Utility.ApplySanityLoss(this.pawn, -sanityRestoreRate, 1);
//                            Messages.Message(this.pawn.ToString() + " has restored some sanity using the " + this.TargetA.Thing.def.label + ".", new TargetInfo(this.pawn.Position, this.pawn.Map), MessageTypeDefOf.NeutralEvent);// .Standard);
//                        }
//                    }
//                    catch
//                    {
//                        Log.Message("Error loading Sanity Hediff.");    
//                    }
//                }
//
//                JoyUtility.TryGainRecRoomThought(this.pawn);
//            });
            yield return toil;
            yield break;
        }
    }

}
