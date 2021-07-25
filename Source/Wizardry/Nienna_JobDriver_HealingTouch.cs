using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Wizardry
{
    internal class Nienna_JobDriver_HealingTouch : JobDriver
    {
        private const TargetIndex caster = TargetIndex.B;
        private readonly bool issueJobAgain = false;
        private readonly int ticksTillNextHeal = 30;

        private int age = -1;
        public int duration = 1200;
        private int injuryCount;
        private int lastHeal;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(TargetA, job, 1, 1, null, errorOnFailed))
            {
                return true;
            }

            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var patient = TargetA.Thing as Pawn;
            var gotoPatient = new Toil
            {
                initAction = () => { pawn.pather.StartPath(TargetA, PathEndMode.Touch); },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoPatient;
            var doHealing = new Toil
            {
                initAction = delegate
                {
                    if (age > duration)
                    {
                        EndJobWith(JobCondition.Succeeded);
                    }

                    if (patient != null && (patient.DestroyedOrNull() || patient.Dead))
                    {
                        EndJobWith(JobCondition.Incompletable);
                    }
                },
                tickAction = delegate
                {
                    if (patient != null && (patient.DestroyedOrNull() || patient.Dead))
                    {
                        EndJobWith(JobCondition.Incompletable);
                    }

                    if (Find.TickManager.TicksGame % 1 == 0)
                    {
                        if (patient != null)
                        {
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_HealingMote"), pawn.DrawPos, Map,
                                Rand.Range(.3f, .5f),
                                (Quaternion.AngleAxis(90, Vector3.up) * GetVector(pawn.Position, patient.Position))
                                .ToAngleFlat() + Rand.Range(-10, 10), 5f, 0);
                        }
                    }

                    if (age > lastHeal + ticksTillNextHeal)
                    {
                        DoHealingEffect(patient);
                        if (patient != null)
                        {
                            EffectMaker.MakeEffect(ThingDef.Named("Mote_HealingCircles"), patient.DrawPos, Map,
                                Rand.Range(.3f, .4f), 0, 0, Rand.Range(400, 500), Rand.Range(0, 360), .08f, .01f, .24f,
                                false);
                        }

                        lastHeal = age;
                        if (injuryCount == 0)
                        {
                            EndJobWith(JobCondition.Succeeded);
                        }
                    }

                    if (patient is {Drafted: false} && patient.CurJobDef != JobDefOf.Wait)
                    {
                        if (patient.jobs.posture == PawnPosture.Standing)
                        {
                            var job1 = new Job(JobDefOf.Wait, patient);
                            patient.jobs.TryTakeOrderedJob(job1, JobTag.Misc);
                        }
                    }

                    age++;
                    ticksLeftThisToil = duration - age;
                    if (age > duration)
                    {
                        EndJobWith(JobCondition.Succeeded);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = duration
            };
            doHealing.WithProgressBar(TargetIndex.B, delegate
            {
                if (pawn.DestroyedOrNull() || pawn.Dead)
                {
                    return 1f;
                }

                return 1f - ((float) doHealing.actor.jobs.curDriver.ticksLeftThisToil / duration);
            }, false, 0f);
            doHealing.AddFinishAction(delegate
            {
                var comp = pawn.GetComp<CompWizardry>();
                var pawnAbility =
                    comp.AbilityData.Powers.FirstOrDefault(x => x.Def == WizardryDefOf.LotRW_Nienna_HealingTouch);
                pawnAbility?.PostAbilityAttempt();
                patient?.jobs.EndCurrentJob(JobCondition.Succeeded);
            });
            yield return doHealing;
        }

        private void DoHealingEffect(Pawn patient)
        {
            var num = 1;
            injuryCount = 0;
            using var enumerator = patient.health.hediffSet.GetInjuredParts().GetEnumerator();
            while (enumerator.MoveNext())
            {
                var rec = enumerator.Current;
                var num2 = 1;
                if (num <= 0)
                {
                    continue;
                }

                var arg_BB_0 = patient.health.hediffSet.GetHediffs<Hediff_Injury>();

                bool ArgBb1(Hediff_Injury injury)
                {
                    return injury.Part == rec;
                }

                foreach (var current in arg_BB_0.Where(ArgBb1))
                {
                    if (num2 <= 0)
                    {
                        continue;
                    }

                    if (!current.CanHealNaturally() || current.IsPermanent())
                    {
                        continue;
                    }

                    injuryCount++;
                    current.Heal(Rand.Range(1f, 2f));
                    num--;
                    num2--;
                }
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            var heading = (objectPos - center).ToVector3();
            var distance = heading.magnitude;
            var direction = heading / distance;
            return direction;
        }
    }
}