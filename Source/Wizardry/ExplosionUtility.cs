using RimWorld;
using Verse;

namespace Wizardry;

public static class ExplosionUtility
{
    public static Explosion CreateExplosion(IntVec3 center, Map map, float radius, DamageDef damType,
        Thing instigator, int damAmount = -1, SoundDef explosionSound = null, ThingDef weapon = null,
        ThingDef projectile = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f,
        int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false,
        ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f,
        int preExplosionSpawnThingCount = 1, float chanceToStartFire = 0f, bool dealMoreDamageAtCenter = false)
    {
        if (map == null)
        {
            Log.Warning("Tried to do explosion in a null map.");
            return null;
        }

        var explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, center, map);
        explosion.radius = radius;
        explosion.damType = damType;
        explosion.instigator = instigator;
        explosion.damAmount = damAmount;
        explosion.weapon = weapon;
        explosion.projectile = projectile;
        explosion.preExplosionSpawnThingDef = preExplosionSpawnThingDef;
        explosion.preExplosionSpawnChance = preExplosionSpawnChance;
        explosion.preExplosionSpawnThingCount = preExplosionSpawnThingCount;
        explosion.postExplosionSpawnThingDef = postExplosionSpawnThingDef;
        explosion.postExplosionSpawnChance = postExplosionSpawnChance;
        explosion.postExplosionSpawnThingCount = postExplosionSpawnThingCount;
        explosion.applyDamageToExplosionCellsNeighbors = applyDamageToExplosionCellsNeighbors;
        explosion.chanceToStartFire = chanceToStartFire;
        explosion.damageFalloff = dealMoreDamageAtCenter;
        return explosion;
        //explosion.StartExplosion(explosionSound);
    }
}