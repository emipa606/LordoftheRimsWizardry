using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;
//using VerseBase;

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
            this.EndOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A, job.def.joyMaxParticipants);
            if (TargetB != null)
            {
                yield return Toils_Reserve.Reserve(TargetIndex.B);
            }

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
            var toil = new Toil();
            toil.PlaySustainerOrSound(DefDatabase<SoundDef>.GetNamed("Estate_SoundManualTypewriter"));
            toil.tickAction = delegate
            {
                pawn.rotationTracker.FaceCell(TargetA.Cell);
                pawn.GainComfortFromCellIfPossible();
                var statValue = TargetThingA.GetStatValue(StatDefOf.JoyGainFactor);
                JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.EndJob, statValue);
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
        }
    }
}