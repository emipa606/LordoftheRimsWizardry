using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Wizardry;

[StaticConstructorOnStartup]
public class Projectile_LightChaser : Projectile_AbilityBase
{
    private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow,
        MapMaterialRenderQueues.OrbitalBeam);

    private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd",
        ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

    private static readonly Material BombardMat = MaterialPool.MatFrom("Effects/Bombardment",
        ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

    private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();
    private readonly List<float> beamSize = new List<float>();
    private readonly List<int> beamStartTick = new List<int>();
    private int age = -1;

    private float angle;
    private IntVec3 anglePos;
    private List<int> beamAge = new List<int>();
    private List<int> beamDuration = new List<int>();
    private List<float> beamMaxSize = new List<float>();
    private int beamNum;
    private List<Vector3> beamPos = new List<Vector3>();
    private List<Vector3> beamVector = new List<Vector3>();
    private Pawn caster;

    private ColorInt colorInt = new ColorInt(255, 255, 140);
    private bool initialized;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref initialized, "initialized");
        Scribe_Values.Look(ref age, "age", -1);
        Scribe_Values.Look(ref anglePos, "anglePos");
        Scribe_Collections.Look(ref beamMaxSize, "beamMaxSize", LookMode.Value);
        Scribe_Collections.Look(ref beamVector, "beamVector", LookMode.Value);
        Scribe_Collections.Look(ref beamDuration, "beamDuration", LookMode.Value);
        Scribe_Collections.Look(ref beamAge, "beamAge", LookMode.Value);
        Scribe_Collections.Look(ref beamPos, "beamPos", LookMode.Value);
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        var map = Map;
        base.Impact(hitThing, blockedByShield);
        var unused = def;
        if (!initialized)
        {
            caster = launcher as Pawn;
            if (caster != null)
            {
                anglePos = caster.Position;
                var targetCell = Position;
                GenerateBeams(targetCell, caster.Position, map);
            }

            initialized = true;
        }

        AdjustBeams();
        if (Find.TickManager.TicksGame % 30 == 0)
        {
            DoEffectsNearPosition();
        }

        RemoveExpiredBeams();
    }

    public void DoEffectsNearPosition()
    {
        var map = Map;
        for (var i = 0; i < beamNum; i++)
        {
            if (beamAge[i] <= beamStartTick[i])
            {
                continue;
            }

            Pawn victim;
            IntVec3 curCell;
            var cellList = GenRadial.RadialCellsAround(beamPos[i].ToIntVec3(), beamSize[i] + 3, true);
            for (var j = 0; j < cellList.Count(); j++)
            {
                curCell = cellList.ToArray()[j];
                if (!curCell.IsValid || !curCell.InBounds(map))
                {
                    continue;
                }

                victim = curCell.GetFirstPawn(map);
                if (victim is not { Downed: false, Dead: false } || victim.Faction == caster.Faction)
                {
                    continue;
                }

                var t = new LocalTargetInfo(victim.Position +
                                            (6 * GetVector(beamPos[i], victim.DrawPos)).ToIntVec3());
                var job = new Job(JobDefOf.Goto, t);
                victim.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }

            cellList = GenRadial.RadialCellsAround(beamPos[i].ToIntVec3(), beamSize[i], true);
            for (var j = 0; j < cellList.Count(); j++)
            {
                curCell = cellList.ToArray()[j];
                if (!curCell.IsValid || !curCell.InBounds(map))
                {
                    continue;
                }

                victim = curCell.GetFirstPawn(map);
                if (victim is not { Downed: false, Dead: false } || victim.Faction == caster.Faction)
                {
                    continue;
                }

                var distanceFromCenter =
                    Mathf.Min((beamPos[i].ToIntVec3() - victim.Position).LengthHorizontal, 1);
                DamageEntities(victim, 2 / distanceFromCenter);
                HealthUtility.AdjustSeverity(victim, HediffDef.Named("LotRW_Blindness"),
                    1f / distanceFromCenter);
            }
        }
    }

    public Vector3 GetVector(Vector3 casterPos, Vector3 targetPos)
    {
        var cellRect = CellRect.CenteredOn(targetPos.ToIntVec3(), 5);
        targetPos = cellRect.RandomVector3;
        var heading = targetPos - casterPos;
        var distance = heading.magnitude;
        var direction = heading / distance;
        return direction;
    }

    public void GenerateBeams(IntVec3 center, IntVec3 casterPos, Map map)
    {
        var cellRect = CellRect.CenteredOn(center, 2);
        beamNum = Rand.Range(3, 5);
        cellRect.ClipInsideMap(map);
        for (var i = 0; i < beamNum; i++)
        {
            beamPos.Add(cellRect.RandomCell.ToVector3());
            beamMaxSize.Add(Rand.Range(2f, 6f));
            beamSize.Add(beamMaxSize[i] / 10f);
            beamAge.Add(0);
            beamDuration.Add(Rand.Range(480, 600));
            beamStartTick.Add(Rand.Range(0, 120));
            //Random range here determines how much variation occurs while beam travels
            //Since this calculation is based on caster position relative to target position, beam behavior will be different if the target position is close 
            //and variation is large (ie beams traveling in all directions)
            //this could also be adjusted with additive values to produce larger xy variations
            beamVector.Add(GetVector(casterPos.ToVector3(), center.ToVector3()) * Rand.Range(.04f, .06f));
        }
    }

    public void AdjustBeams()
    {
        for (var i = 0; i < beamNum; i++)
        {
            if (beamAge[i] > beamStartTick[i])
            {
                beamPos[i] += beamVector[i];
                if (beamAge[i] > beamDuration[i] * .8f)
                {
                    //gracefully end beam
                    beamSize[i] -= beamMaxSize[i] / (beamDuration[i] * .2f);
                }
                else if (beamSize[i] < beamMaxSize[i])
                {
                    //expand beam until it reaches max size or until it needs to start shrinking
                    beamSize[i] += beamMaxSize[i] / (beamDuration[i] * .3f);
                }
            }

            beamAge[i]++;
        }
    }

    public void RemoveExpiredBeams()
    {
        //remove beam from list
        for (var i = 0; i < beamNum; i++)
        {
            if (beamAge[i] <= beamDuration[i])
            {
                continue;
            }

            beamAge.Remove(beamAge[i]);
            beamDuration.Remove(beamDuration[i]);
            beamPos.Remove(beamPos[i]);
            beamMaxSize.Remove(beamMaxSize[i]);
            beamVector.Remove(beamVector[i]);
            beamSize.Remove(beamSize[i]);
            beamStartTick.Remove(beamStartTick[i]);
            beamNum--;
        }
    }

    public override void Draw()
    {
        base.Draw();
        for (var i = 0; i < beamNum; i++)
        {
            if (beamAge[i] >= beamStartTick[i] && beamAge[i] < beamDuration[i])
            {
                DrawBeam(beamPos[i], beamSize[i]);
            }
        }
    }

    public void DrawBeam(Vector3 drawPos, float size)
    {
        var num = (Map.Size.z - drawPos.z) * 1.4f; //distance towards top of map from the target position
        angle = (anglePos.x - drawPos.x) *
                .3f; //beams originate from above caster (original position) head, and the angle moves, not the entire beam
        var a = Vector3Utility.FromAngleFlat(angle - 90f); //angle of beam adjusted for quaternian
        //matrix4x4 will draw the stretched beam (matrix) with center at drawPos, so it must be adjusted so that the end of the beam appears at drawPos
        //so we create a new vector and add half the length of the beam to the original position so that the end of the beam appears at drawPos
        var a2 = drawPos + (a * num * 0.5f); //original position adjusted by half the beam length
        a2.y = AltitudeLayer.MetaOverlays.AltitudeFor(); //mote depth (should be drawn in front of everything else)

        //Color arg_50_0 = colorInt.ToColor;
        //Color color = arg_50_0;
        //color.a *= num3;
        //Projectile_LightChaser.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);

        if (!(size > 0))
        {
            return;
        }

        //Draw the beam
        Matrix4x4 matrix = default;
        //Create the graphics based on translation, rotation, and scaling
        //Beam is drawn where the bottom end of the beam looks like it's at the target position
        matrix.SetTRS(a2, Quaternion.Euler(0f, angle, 0f), new Vector3(size, 1f, num));
        Graphics.DrawMesh(MeshPool.plane10, matrix, BeamMat, 0, null, 0, MatPropertyBlock);

        //The beam is a rectangle and we want to soften the end, so add a beam end mote and adjust for size and angle offsets
        var vectorPos = drawPos - (a * size * .5f);
        vectorPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
        Matrix4x4 matrix2 = default;
        matrix2.SetTRS(vectorPos, Quaternion.Euler(0f, angle, 0f), new Vector3(size, 1f, size));
        Graphics.DrawMesh(MeshPool.plane10, matrix2, BeamEndMat, 0, null, 0, MatPropertyBlock);

        //Additional softening of the end point and add more intensity by adding light "splash" at the end of the beam, so add another, circular mesh at the end            
        Matrix4x4 matrix3 = default;
        matrix3.SetTRS(drawPos, Quaternion.Euler(0f, angle, 0f), new Vector3(10f * size, 1f, 10f * size));
        Graphics.DrawMesh(MeshPool.plane10, matrix3, BombardMat, 0, null, 0, MatPropertyBlock);
    }

    public void DamageEntities(Thing e, float amt)
    {
        amt = Rand.Range(amt * .75f, amt * 1.25f);
        var dinfo = new DamageInfo(DamageDefOf.Flame, Mathf.RoundToInt(amt), 2);
        e?.TakeDamage(dinfo);
    }

    public override void Tick()
    {
        base.Tick();
        age++;
        if (age < 120 || beamNum != 0)
        {
            return;
        }

        age = 721;
        Destroy();
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!(age <= 600))
        {
            base.Destroy(mode);
        }
    }
}