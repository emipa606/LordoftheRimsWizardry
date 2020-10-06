using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Wizardry
{
    [StaticConstructorOnStartup]
    public static class Effect_MeshMaker
    {
        private static List<Vector2> verts2D;

        private static Vector2 lightningTop;

        private const float LightningHeight = 200f;

        private const float LightningRootXVar = 50f;

        private const float VertexInterval = 0.25f;

        private const float MeshWidth = 2f;

        private const float UVIntervalY = 0.04f;

        private const float PerturbAmp = 12f;

        private const float PerturbFreq = 0.007f;

        public static Mesh NewBoltMesh(float distance, float amplitude)
        {
            lightningTop = new Vector2(Rand.Range(-.02f, .02f), distance);
            MakeVerticesBase();
            if (amplitude > 0)
            {
                PeturbVerticesRandomly(amplitude);
            }
            DoubleVertices();
            return MeshFromVerts();
        }

        private static void MakeVerticesBase()
        {
            int num = (int)Math.Ceiling((double)((Vector2.zero - lightningTop).magnitude / 0.25f));
            Vector2 b = lightningTop / (float)num;
            verts2D = new List<Vector2>();
            Vector2 vector = Vector2.zero;
            for (int i = 0; i < num; i++)
            {
                verts2D.Add(vector);
                vector += b;
            }
        }

        private static void PeturbVerticesRandomly(float amplitude)
        {
            Perlin perlin = new Perlin(0.0070000002160668373, 2.0, 0.5, 6, Rand.Range(0, 2147483647), QualityMode.High);
            List<Vector2> list = verts2D.ListFullCopy<Vector2>();
            verts2D.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                float d = amplitude * (float)perlin.GetValue((double)i, 0.0, 0.0);
                Vector2 item = list[i] + d * Vector2.right;
                verts2D.Add(item);
            }
        }

        private static void DoubleVertices()
        {
            List<Vector2> list = verts2D.ListFullCopy<Vector2>();
            Vector3 vector = default;
            Vector2 a = default;
            verts2D.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                if (i <= list.Count - 2)
                {
                    vector = Quaternion.AngleAxis(90f, Vector3.up) * (list[i] - list[i + 1]);
                    a = new Vector2(vector.y, vector.z);
                    a.Normalize();
                }
                Vector2 item = list[i] - 1f * a;
                Vector2 item2 = list[i] + 1f * a;
                verts2D.Add(item);
                verts2D.Add(item2);
            }
        }

        private static Mesh MeshFromVerts()
        {
            Vector3[] array = new Vector3[verts2D.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new Vector3(verts2D[i].x, 0f, verts2D[i].y);
            }
            float num = 0f;
            Vector2[] array2 = new Vector2[verts2D.Count];
            for (int j = 0; j < verts2D.Count; j += 2)
            {
                array2[j] = new Vector2(0f, num);
                array2[j + 1] = new Vector2(1f, num);
                num += 0.04f;
            }
            int[] array3 = new int[verts2D.Count * 3];
            for (int k = 0; k < verts2D.Count - 2; k += 2)
            {
                int num2 = k * 3;
                array3[num2] = k;
                array3[num2 + 1] = k + 1;
                array3[num2 + 2] = k + 2;
                array3[num2 + 3] = k + 2;
                array3[num2 + 4] = k + 1;
                array3[num2 + 5] = k + 3;
            }
            return new Mesh
            {
                vertices = array,
                uv = array2,
                triangles = array3,
                name = "MeshFromVerts()"
            };
        }
    }
}