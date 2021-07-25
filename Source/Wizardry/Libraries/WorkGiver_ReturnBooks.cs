using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Wizardry
{
    public class WorkGiver_ReturnBooks : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerThings.AllThings.FindAll(x => x is ThingBook);
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.listerThings.AllThings.Any(x => x is ThingBook);
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is ThingBook book))
            {
                return null;
            }

            if (!HaulAIUtility.PawnCanAutomaticallyHaul(pawn, t, forced))
            {
                return null;
            }

            var Building_InternalStorage = FindBestStorage(pawn, book);
            if (Building_InternalStorage != null)
            {
                return new Job(DefDatabase<JobDef>.GetNamed("Estate_ReturnBook"), t, Building_InternalStorage)
                {
                    count = book.stackCount
                };
            }

            JobFailReason.Is("NoEmptyGraveLower".Translate());
            return null;
        }

        private Building_InternalStorage FindBestStorage(Pawn p, ThingBook book)
        {
            bool Predicate(Thing m)
            {
                return !m.IsForbidden(p) && p.CanReserveNew(m) && ((Building_InternalStorage) m).Accepts(book);
            }

            float PriorityGetter(Thing t)
            {
                var result = 0f;
                result += (float) ((IStoreSettingsParent) t).GetStoreSettings().Priority;
                if (t is Building_InternalStorage bS && bS.TryGetInnerInteractableThingOwner()?.Count > 0)
                {
                    result -= bS.TryGetInnerInteractableThingOwner().Count;
                }

                return result;
            }

            var position = book.Position;
            var map = book.Map;
            var searchSet = book.Map.listerThings.AllThings.FindAll(x => x is Building_InternalStorage);
            var peMode = PathEndMode.ClosestTouch;
            var traverseParams = TraverseParms.For(p);
            var validator = (Predicate<Thing>) Predicate;
            return (Building_InternalStorage) GenClosest.ClosestThing_Global_Reachable(position, map, searchSet, peMode,
                traverseParams, 9999f, validator, PriorityGetter);
        }
    }
}