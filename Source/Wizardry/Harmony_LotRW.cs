using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    internal class Harmony_LotRW
    {
        static Harmony_LotRW()
        {
            var harmonyInstance = new Harmony("rimworld.lotrw.wizardry");
            harmonyInstance.Patch(AccessTools.Method(typeof(Projectile), "Launch", new[]
            {
                typeof(Thing),
                typeof(Vector3),
                typeof(LocalTargetInfo),
                typeof(LocalTargetInfo),
                typeof(ProjectileHitFlags),
                typeof(bool),
                typeof(Thing),
                typeof(ThingDef)
            }), new HarmonyMethod(typeof(Harmony_LotRW), "Projectile_Launch_Prefix"));
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static bool Projectile_Launch_Prefix(Projectile __instance, Thing launcher, Vector3 origin,
            ref LocalTargetInfo usedTarget, ref LocalTargetInfo intendedTarget)
        {
            if (launcher is not Pawn launcherPawn)
            {
                return true;
            }

            if (!launcherPawn.health.hediffSet.HasHediff(HediffDef.Named("LotRW_DoomHD")))
            {
                return true;
            }

            if (launcherPawn.equipment.PrimaryEq == null || !launcherPawn.equipment.Primary.def.IsRangedWeapon)
            {
                return true;
            }

            var maxRange = launcherPawn.equipment.Primary.def.Verbs.FirstOrDefault().range;
            var doomTargets = new List<Pawn>();
            var mapPawns = launcherPawn.Map.mapPawns.AllPawnsSpawned;
            doomTargets.Clear();
            foreach (var pawn in mapPawns)
            {
                if (pawn.Faction == launcherPawn.Faction &&
                    (pawn.Position - launcherPawn.Position).LengthHorizontal < maxRange)
                {
                    doomTargets.Add(pawn);
                }
            }

            if (doomTargets.Count <= 0)
            {
                return true;
            }

            LocalTargetInfo doomTarget = doomTargets.RandomElement();
            if (doomTarget == launcherPawn)
            {
                doomTarget = usedTarget;
            }
            else
            {
                if (Rand.Chance(.5f))
                {
                    HealthUtility.AdjustSeverity(doomTarget.Thing as Pawn,
                        HediffDef.Named("LotRW_DoomHD"), 1f);
                    for (var i = 0; i < 4; i++)
                    {
                        EffectMaker.MakeEffect(ThingDef.Named("Mote_BlackSmoke"),
                            doomTarget.Thing.DrawPos, doomTarget.Thing.Map, Rand.Range(.4f, .6f),
                            Rand.Range(0, 360), Rand.Range(.2f, .4f), Rand.Range(-200, 200), .15f, 2f,
                            Rand.Range(.2f, .3f), true);
                    }
                }
            }

            var drawPos = launcherPawn.DrawPos;
            ThingDef moteDef;
            if (doomTarget.Cell.x < launcherPawn.Position.x)
            {
                drawPos.x += .6f;
                moteDef = ThingDef.Named("Mote_ReaperWest");
            }
            else
            {
                drawPos.x -= .6f;
                moteDef = ThingDef.Named("Mote_ReaperEast");
            }

            drawPos.z += .5f;
            usedTarget = doomTarget;
            intendedTarget = doomTarget;
            EffectMaker.MakeEffect(moteDef, drawPos, launcherPawn.Map, .8f, 0, 0, 0, .2f, .1f, .4f,
                false);

            return true;
        }
    }
}