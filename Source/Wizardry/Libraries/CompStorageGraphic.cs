﻿using Verse;

namespace Wizardry;

public class CompStorageGraphic : ThingComp
{
    private Graphic cachedGraphic;
    public CompProperties_StorageGraphic Props => props as CompProperties_StorageGraphic;

    public Graphic CurStorageGraphic
    {
        get
        {
            if (cachedGraphic != null)
            {
                return cachedGraphic;
            }

            if (parent.TryGetInnerInteractableThingOwner() is not { Count: var count })
            {
                return cachedGraphic;
            }

            if (count >= Props.countFullCapacity)
            {
                cachedGraphic = Props.graphicFull.GraphicColoredFor(parent);
            }
            else if (count >= Props.countSparseThreshhold)
            {
                cachedGraphic = Props.graphicSparse.GraphicColoredFor(parent);
            }
            else
            {
                cachedGraphic = Props.graphicEmpty.GraphicColoredFor(parent);
            }

            return cachedGraphic;
        }
    }

    public void UpdateGraphics()
    {
        cachedGraphic = null;
    }
}