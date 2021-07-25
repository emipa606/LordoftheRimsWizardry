using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Wizardry
{
    public class MeshMaker : WeatherEvent
    {
        private static readonly List<Mesh> boltMeshes = new List<Mesh>();

        private static readonly SkyColorSet MeshSkyColors = new SkyColorSet(new Color(0.9f, 0.95f, 1f),
            new Color(0.784313738f, 0.8235294f, 0.847058833f), new Color(0.9f, 0.95f, 1f), 1.15f);

        private readonly AltitudeLayer altitudeLayer;
        private readonly int boltMaxCount = 25;
        private readonly int durationSolid;
        private readonly int fadeInTicks = 10;
        private readonly int fadeOutTicks = 20;
        private readonly float meshContortionMagnitude = 12f;
        private readonly Material meshMat;
        private readonly bool startIsVec = false;
        private int age;

        private float angle;
        private Mesh boltMesh;
        private readonly Vector3 meshEnd;
        private readonly Vector3 meshStart;

        private Vector2 shadowVector; //currently unused

        public MeshMaker(Map map, Material meshMat, IntVec3 meshStart, IntVec3 meshEnd, float meshContortionMagnitude,
            AltitudeLayer altitudeLayer, int durationSolid, int fadeOutTicks, int fadeInTicks) : base(map)
        {
            this.map = map;
            this.meshMat = meshMat;
            this.meshStart = meshStart.ToVector3ShiftedWithAltitude(altitudeLayer);
            this.meshEnd = meshEnd.ToVector3ShiftedWithAltitude(altitudeLayer);
            this.meshContortionMagnitude = meshContortionMagnitude;
            this.altitudeLayer = altitudeLayer;
            this.durationSolid = durationSolid;
            this.fadeOutTicks = fadeOutTicks;
            this.fadeInTicks = fadeInTicks;
            //this.shadowVector = new Vector2(Rand.Range(-5f, 5f), Rand.Range(-5f, 0f));
        }

        public MeshMaker(Map map, Material meshMat, Vector3 meshStart, Vector3 meshEnd, float meshContortionMagnitude,
            AltitudeLayer altitudeLayer, int durationSolid, int fadeOutTicks, int fadeInTicks) : base(map)
        {
            this.map = map;
            this.meshMat = meshMat;
            this.meshStart = meshStart;
            this.meshEnd = meshEnd;
            this.meshContortionMagnitude = meshContortionMagnitude;
            this.altitudeLayer = altitudeLayer;
            this.durationSolid = durationSolid;
            this.fadeOutTicks = fadeOutTicks;
            this.fadeInTicks = fadeInTicks;
            //this.shadowVector = new Vector2(Rand.Range(-5f, 5f), Rand.Range(-5f, 0f));
        }

        public override bool Expired => age > durationSolid + fadeOutTicks;

        public Mesh RandomBoltMesh
        {
            get
            {
                Mesh result;
                if (boltMeshes.Count < boltMaxCount)
                {
                    Mesh mesh;
                    if (meshStart != default)
                    {
                        mesh = Effect_MeshMaker.NewBoltMesh(Vector3.Distance(meshStart, meshEnd),
                            meshContortionMagnitude);
                    }
                    else
                    {
                        mesh = Effect_MeshMaker.NewBoltMesh(200, meshContortionMagnitude);
                    }

                    boltMeshes.Add(mesh);
                    result = mesh;
                }
                else
                {
                    result = boltMeshes.RandomElement();
                }

                return result;
            }
        }

        protected float MeshBrightness
        {
            get
            {
                float result;
                if (age <= fadeInTicks)
                {
                    result = (float) age / fadeInTicks;
                }
                else if (age < durationSolid)
                {
                    result = 1f;
                }
                else
                {
                    result = 1f - ((age - durationSolid) / (float) fadeOutTicks);
                }

                return result;
            }
        }

        public override SkyTarget SkyTarget => new SkyTarget(1f, MeshSkyColors, 1f, 1f);

        public override Vector2? OverrideShadowVector => shadowVector;

        public override void FireEvent()
        {
            if (meshStart != default)
            {
                GetVector(meshStart, meshEnd);
            }

            boltMesh = RandomBoltMesh;
        }

        public override void WeatherEventDraw()
        {
            if (meshStart != default)
            {
                Graphics.DrawMesh(boltMesh, meshStart, Quaternion.Euler(0f, angle, 0f),
                    FadedMaterialPool.FadedVersionOf(meshMat, MeshBrightness), 0);
            }
            else
            {
                Graphics.DrawMesh(boltMesh, meshEnd, Quaternion.identity,
                    FadedMaterialPool.FadedVersionOf(meshMat, MeshBrightness), 0);
            }
        }

        public override void WeatherEventTick()
        {
            age++;
        }

        public Vector3 GetVector(Vector3 start, Vector3 end)
        {
            var heading = end - start;
            var distance = heading.magnitude;
            var direction = heading / distance;
            angle = (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat();
            return direction;
        }
    }
}