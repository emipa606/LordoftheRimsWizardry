using Verse;
using Verse.Sound;
using RimWorld;
using AbilityUser;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse.AI;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    public class Projectile_LightChaser : Projectile_AbilityBase
    {
        private bool initialized = false;
        private int beamNum;
        private int age = -1;
        List<float> beamMaxSize = new List<float>();
        readonly List<float> beamSize = new List<float>();
        List<int> beamDuration = new List<int>();
        List<int> beamAge = new List<int>();
        readonly List<int> beamStartTick = new List<int>();
        List<Vector3> beamPos = new List<Vector3>();
        List<Vector3> beamVector = new List<Vector3>();
        IntVec3 anglePos;
        Pawn caster;

        ColorInt colorInt = new ColorInt(255, 255, 140);

        private float angle = 0;

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BombardMat = MaterialPool.MatFrom("Effects/Bombardment", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref age, "age", -1, false);
            Scribe_Values.Look<IntVec3>(ref anglePos, "anglePos", default, false);
            Scribe_Collections.Look<float>(ref beamMaxSize, "beamMaxSize", LookMode.Value);
            Scribe_Collections.Look<Vector3>(ref beamVector, "beamVector", LookMode.Value);
            Scribe_Collections.Look<int>(ref beamDuration, "beamDuration", LookMode.Value);
            Scribe_Collections.Look<int>(ref beamAge, "beamAge", LookMode.Value);
            Scribe_Collections.Look<Vector3>(ref beamPos, "beamPos", LookMode.Value);
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            if (!initialized)
            {
                caster = launcher as Pawn;
                anglePos = caster.Position;
                IntVec3 targetCell = Position;
                GenerateBeams(targetCell, caster.Position, map);
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
            Map map = Map;
            for (int i = 0; i < beamNum; i++)
            {
                if (beamAge[i] > beamStartTick[i])
                {
                    Pawn victim = null;
                    IntVec3 curCell;
                    IEnumerable<IntVec3> cellList = GenRadial.RadialCellsAround(beamPos[i].ToIntVec3(), beamSize[i] + 3, true);
                    for (int j = 0; j < cellList.Count(); j++)
                    {
                        curCell = cellList.ToArray<IntVec3>()[j];
                        if (curCell.IsValid && curCell.InBounds(map))
                        {
                            victim = curCell.GetFirstPawn(map);
                            if (victim != null && !victim.Downed && !victim.Dead && victim.Faction != caster.Faction)
                            {
                                LocalTargetInfo t = new LocalTargetInfo(victim.Position + (6 * GetVector(beamPos[i], victim.DrawPos)).ToIntVec3());
                                Job job = new Job(JobDefOf.Goto, t);
                                victim.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            }
                            else
                            {
                                victim = null;
                            }
                        }
                    }
                    cellList = GenRadial.RadialCellsAround(beamPos[i].ToIntVec3(), beamSize[i], true);
                    for (int j = 0; j < cellList.Count(); j++)
                    {
                        curCell = cellList.ToArray<IntVec3>()[j];
                        if (curCell.IsValid && curCell.InBounds(map))
                        {
                            victim = curCell.GetFirstPawn(map);
                            if (victim != null && !victim.Downed && !victim.Dead && victim.Faction != caster.Faction)
                            {
                                float distanceFromCenter = Mathf.Min((beamPos[i].ToIntVec3() - victim.Position).LengthHorizontal, 1);
                                DamageEntities(victim, 2 / distanceFromCenter);
                                HealthUtility.AdjustSeverity(victim, HediffDef.Named("LotRW_Blindness"), 1f / distanceFromCenter);
                            }
                            else
                            {
                                victim = null;
                            }
                        }
                    }
                }
            }
        }

        public Vector3 GetVector(Vector3 casterPos, Vector3 targetPos)
        {
            CellRect cellRect = CellRect.CenteredOn(targetPos.ToIntVec3(), 5);
            targetPos = cellRect.RandomVector3;
            Vector3 heading = (targetPos - casterPos);
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }

        public void GenerateBeams(IntVec3 center, IntVec3 casterPos, Map map)
        {
            CellRect cellRect = CellRect.CenteredOn(center, 2);
            beamNum = Rand.Range(3, 5);
            cellRect.ClipInsideMap(map);
            for (int i = 0; i < beamNum; i++)
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
            for (int i = 0; i < beamNum; i++)
            {
                if (beamAge[i] > beamStartTick[i])
                {
                    beamPos[i] += beamVector[i];
                    if (beamAge[i] > beamDuration[i] * .8f)
                    {
                        //gracefully end beam
                        beamSize[i] -= (beamMaxSize[i] / (beamDuration[i] * .2f));
                    }
                    else if (beamSize[i] < beamMaxSize[i])
                    {
                        //expand beam until it reaches max size or until it needs to start shrinking
                        beamSize[i] += (beamMaxSize[i] / (beamDuration[i] * .3f));
                    }
                }
                beamAge[i]++;
            }
        }

        public void RemoveExpiredBeams()
        {
            //remove beam from list
            for (int i = 0; i < beamNum; i++)
            {
                if(beamAge[i] > beamDuration[i])
                {
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
        }

        public override void Draw()
        {
            base.Draw();
            for (int i = 0; i < beamNum; i++)
            {
                if (beamAge[i] >= beamStartTick[i] && beamAge[i] < beamDuration[i])
                {
                    DrawBeam(beamPos[i], beamSize[i]);
                }
            }
        }

        public void DrawBeam(Vector3 drawPos, float size)
        {
            float num = ((float)Map.Size.z - drawPos.z) * 1.4f;        //distance towards top of map from the target position
            angle = ((anglePos.x - drawPos.x) * .3f);                  //beams originate from above caster (original position) head, and the angle moves, not the entire beam
            Vector3 a = Vector3Utility.FromAngleFlat(angle - 90f);     //angle of beam adjusted for quaternian
            //matrix4x4 will draw the stretched beam (matrix) with center at drawPos, so it must be adjusted so that the end of the beam appears at drawPos
            //so we create a new vector and add half the length of the beam to the original position so that the end of the beam appears at drawPos
            Vector3 a2 = drawPos + a * num * 0.5f;                          //original position adjusted by half the beam length
            a2.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);       //mote depth (should be drawn in front of everything else)

            //Color arg_50_0 = colorInt.ToColor;
            //Color color = arg_50_0;
            //color.a *= num3;
            //Projectile_LightChaser.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);

            if (size > 0) //failsafe to prevent graphic anomolies not already handled
            {
                //Draw the beam
                Matrix4x4 matrix = default;
                //Create the graphics based on translation, rotation, and scaling
                //Beam is drawn where the bottom end of the beam looks like it's at the target position
                matrix.SetTRS(a2, Quaternion.Euler(0f, angle, 0f), new Vector3(size, 1f, num));
                Graphics.DrawMesh(MeshPool.plane10, matrix, BeamMat, 0, null, 0, MatPropertyBlock);

                //The beam is a rectangle and we want to soften the end, so add a beam end mote and adjust for size and angle offsets
                Vector3 vectorPos = drawPos - (a * size *.5f);
                vectorPos.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
                Matrix4x4 matrix2 = default;
                matrix2.SetTRS(vectorPos, Quaternion.Euler(0f, angle, 0f), new Vector3(size, 1f, size));
                Graphics.DrawMesh(MeshPool.plane10, matrix2, BeamEndMat, 0, null, 0, MatPropertyBlock);

                //Additional softening of the end point and add more intensity by adding light "splash" at the end of the beam, so add another, circular mesh at the end            
                Matrix4x4 matrix3 = default;
                matrix3.SetTRS(drawPos, Quaternion.Euler(0f, angle, 0f), new Vector3(10f * size, 1f, 10f * size));
                Graphics.DrawMesh(MeshPool.plane10, matrix3, BombardMat, 0, null, 0, MatPropertyBlock);
            }
        }

        public void DamageEntities(Thing e, float amt)
        {
            amt = Rand.Range(amt * .75f, amt * 1.25f);
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, Mathf.RoundToInt(amt), 2, (float)-1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }

        public override void Tick()
        {
            base.Tick();
            age++;
            if(age >= 120 && beamNum == 0)
            {
                age = 721;
                Destroy(DestroyMode.Vanish);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = age <= 600;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }
    }
}
