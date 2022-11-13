using RimWorld;
using Verse;

namespace Wizardry;

public class ThingBook : ThingWithComps
{
    private CompArt compArt;

    private CompArt CompArt
    {
        get
        {
            if (compArt == null)
            {
                compArt = this.TryGetComp<CompArt>();
            }

            return compArt;
        }
    }

    public override string Label
    {
        get
        {
            if (CompArt != null)
            {
                return "Estate_BookTitle".Translate(CompArt.Title, CompArt.AuthorName) + " (" +
                       base.Label + ")";
            }

            return base.Label;
        }
    }
}