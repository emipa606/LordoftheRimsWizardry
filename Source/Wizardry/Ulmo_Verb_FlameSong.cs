using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;

namespace Wizardry;

public class Ulmo_Verb_FlameSong : Verb_UseAbility
{
    private float fireAmount;

    protected override bool TryCastShot()
    {
        var map = base.CasterPawn.Map;
        var unused = base.CasterPawn;
        var centerCell = currentTarget.Cell;
        fireAmount = CalculateFireAmountInArea(centerCell, Projectile.projectile.explosionRadius, map);
        if (centerCell.IsValid && centerCell.InBounds(map))
        {
            Thing thing = null;
            switch (fireAmount)
            {
                case > 8f:
                    thing = ThingMaker.MakeThing(ThingDef.Named("LotRW_Flamesong_Orb"));
                    break;
                case > 1f:
                    thing = ThingMaker.MakeThing(ThingDef.Named("LotRW_Flamesong_Orb_Small"));
                    break;
            }

            if (thing != null)
            {
                GenPlace.TryPlaceThing(thing, centerCell, map, ThingPlaceMode.Near);
            }
        }
        else
        {
            Messages.Message("failed to spawn orb of flamesong", MessageTypeDefOf.RejectInput);
        }

        Ability.PostAbilityAttempt();
        burstShotsLeft = 0;
        return false;
    }

    public float CalculateFireAmountInArea(IntVec3 center, float radius, Map map)
    {
        float result = 0;
        var fireList = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
        var targetCells = GenRadial.RadialCellsAround(center, radius, true);
        for (var i = 0; i < targetCells.Count(); i++)
        {
            var curCell = targetCells.ToArray()[i];
            if (!curCell.InBounds(map) || !curCell.IsValid)
            {
                continue;
            }

            foreach (var thing in fireList)
            {
                if (thing.Position != curCell)
                {
                    continue;
                }

                if (thing is Fire fire)
                {
                    result += fire.fireSize;
                }

                RemoveFireAtPosition(curCell, map);
            }
        }

        return result;
    }

    public void RemoveFireAtPosition(IntVec3 pos, Map map)
    {
        GenExplosion.DoExplosion(pos, map, 1, DamageDefOf.Extinguish, CasterPawn, 100, 0,
            SoundDef.Named("ExpandingFlames"));
    }
}