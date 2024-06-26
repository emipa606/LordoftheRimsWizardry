﻿using System.Collections.Generic;
using Verse.AI;

namespace Wizardry;

public class JobDriver_Write : JobDriver
{
    //What should we do?
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // Toil 1:
        // Goto Target (TargetPack A is selected (It has the info where the target cell is))
        yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);


        // Toil 2:
        // Write at the table.
        var arrivalDraft = new Toil
        {
            initAction = () =>
            {
                // Here you can insert your own code about what should be done
                // At the time when this toil is executed, the pawn is at the goto-cell from the first toil
                pawn.drafter.Drafted = true;
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return arrivalDraft;


        // Toil X:
        // You can add more and more toils, the pawn will do them one after the other. And everything is just one job..
        // End every toil with a "yield return toilName"
    }
}
