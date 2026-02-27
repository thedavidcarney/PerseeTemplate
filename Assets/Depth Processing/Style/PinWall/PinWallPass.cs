using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class PinWallPass : StylePass
    {
        private const float WORLD_WIDTH  = 20.5f;
        private const float WORLD_HEIGHT = 12.0f;

        private ComputeShader compute;
        private Material pinMaterial;
        private Mesh pinMesh;

        private ComputeBuffer transformBuffer;
        private ComputeBuffer argsBuffer;

        private int kernel;
        private int gridWidth;
        private int gridHeight;
        private float pinScale;
        private float displacementScale;
        private float displacementOffset;

        private int currentGridWidth  = -1;
        private int currentGridHeight = -1;

        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public int GridWidth
        {
            get => gridWidth;
            set { gridWidth = Mathf.Max(1, value); }
        }

        public int GridHeight
        {
            get => gridHeight;
            set { gridHeight = Mathf.Max(1, value); }
        }

        public float PinScale
        {
            get => pinScale;
            set { pinScale = Mathf.Max(0.001f, value); }
        }

        public float DisplacementScale
        {
            get => displacementScale;
            set { displacementScale = value; }
        }

        public float DisplacementOffset
        {
            get => displacementOffset;
            set { displacementOffset = value; }
        }

        public PinWallPass(string passName = "PinWall")
        {
            name        = passName;
            compute     = Resources.Load<ComputeShader>("PinWallCompute");
            pinMaterial = Resources.Load<Material>("PinWallMat");
            pinMesh     = CreateIcosphere(1);
            kernel      = compute.FindKernel("CSMain");

            LoadPrefs();
            BuildParameters();
        }

        private void BuildParameters()
        {
            parameters = new List<DepthParameter>
            {
                new IntParameter  ("Grid Width",   () => gridWidth,          v => { GridWidth = v; },          4,    200),
                new IntParameter  ("Grid Height",  () => gridHeight,         v => { GridHeight = v; },         4,    200),
                new FloatParameter("Pin Scale",    () => pinScale,           v => { PinScale = v; },           0.01f, 0.01f, 2f),
                new FloatParameter("Displacement", () => displacementScale,  v => { DisplacementScale = v; },  0.1f, -10f,  10f),
                new FloatParameter("Offset",       () => displacementOffset, v => { DisplacementOffset = v; }, 0.05f, -5f,   5f),
            };
        }

        private void EnsureBuffers()
        {
            if(currentGridWidth == gridWidth && currentGridHeight == gridHeight)
                return;

            ReleaseBuffers();

            int count = gridWidth * gridHeight;

            // float4x4 = 16 floats = 64 bytes
            transformBuffer = new ComputeBuffer(count, 64);

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = pinMesh.GetIndexCount(0);
            args[1] = (uint)count;
            args[2] = pinMesh.GetIndexStart(0);
            args[3] = pinMesh.GetBaseVertex(0);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            currentGridWidth  = gridWidth;
            currentGridHeight = gridHeight;
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            if(compute == null || pinMaterial == null || pinMesh == null)
            {
                Debug.LogError("PinWallPass: missing compute shader, material, or mesh.");
                Graphics.Blit(src, dst);
                return;
            }

            EnsureBuffers();

            compute.SetTexture(kernel, "_DepthTex",         src);
            compute.SetBuffer (kernel, "_Transforms",       transformBuffer);
            compute.SetInt    ("_GridWidth",                gridWidth);
            compute.SetInt    ("_GridHeight",               gridHeight);
            compute.SetFloat  ("_PinScale",                 pinScale);
            compute.SetFloat  ("_DisplacementScale",        displacementScale);
            compute.SetFloat  ("_DisplacementOffset",       displacementOffset);
            compute.SetFloat  ("_WorldWidth",               WORLD_WIDTH);
            compute.SetFloat  ("_WorldHeight",              WORLD_HEIGHT);

            int groupsX = Mathf.CeilToInt(gridWidth  / 8.0f);
            int groupsY = Mathf.CeilToInt(gridHeight / 8.0f);
            compute.Dispatch(kernel, groupsX, groupsY, 1);

            pinMaterial.SetBuffer("_Transforms", transformBuffer);

            Bounds bounds = new Bounds(Vector3.zero, new Vector3(WORLD_WIDTH + 4f, WORLD_HEIGHT + 4f, 20f));
            Graphics.DrawMeshInstancedIndirect(pinMesh, 0, pinMaterial, bounds, argsBuffer);

            // Pass src through so pipeline continues cleanly
            Graphics.Blit(src, dst);
        }

        // Icosphere with subdivisions=1 gives 80 triangles — much cheaper than Unity's sphere (2880 tris)
        private Mesh CreateIcosphere(int subdivisions)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;

            var verts = new List<Vector3>
            {
                Norm(new Vector3(-1,  t,  0)), Norm(new Vector3( 1,  t,  0)),
                Norm(new Vector3(-1, -t,  0)), Norm(new Vector3( 1, -t,  0)),
                Norm(new Vector3( 0, -1,  t)), Norm(new Vector3( 0,  1,  t)),
                Norm(new Vector3( 0, -1, -t)), Norm(new Vector3( 0,  1, -t)),
                Norm(new Vector3( t,  0, -1)), Norm(new Vector3( t,  0,  1)),
                Norm(new Vector3(-t,  0, -1)), Norm(new Vector3(-t,  0,  1)),
            };

            var tris = new List<int>
            {
                0,11,5,  0,5,1,  0,1,7,  0,7,10,  0,10,11,
                1,5,9,   5,11,4, 11,10,2, 10,7,6,  7,1,8,
                3,9,4,   3,4,2,  3,2,6,  3,6,8,   3,8,9,
                4,9,5,   2,4,11, 6,2,10, 8,6,7,   9,8,1
            };

            for(int s = 0; s < subdivisions; s++)
            {
                var newTris = new List<int>();
                var cache   = new Dictionary<long, int>();

                for(int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i+1], c = tris[i+2];
                    int ab = MidPoint(verts, cache, a, b);
                    int bc = MidPoint(verts, cache, b, c);
                    int ca = MidPoint(verts, cache, c, a);

                    newTris.AddRange(new[] { a, ab, ca });
                    newTris.AddRange(new[] { b, bc, ab });
                    newTris.AddRange(new[] { c, ca, bc });
                    newTris.AddRange(new[] { ab, bc, ca });
                }
                tris = newTris;
            }

            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private int MidPoint(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
        {
            long key = ((long)Mathf.Min(a, b) << 32) + Mathf.Max(a, b);
            if(cache.TryGetValue(key, out int idx)) return idx;
            idx = verts.Count;
            verts.Add(Norm((verts[a] + verts[b]) * 0.5f));
            cache[key] = idx;
            return idx;
        }

        private Vector3 Norm(Vector3 v) => v.normalized;

        public override void LoadPrefs()
        {
            GridWidth         = PlayerPrefs.GetInt  (PrefKey("gridWidth"),          80);
            GridHeight        = PlayerPrefs.GetInt  (PrefKey("gridHeight"),         47);
            PinScale          = PlayerPrefs.GetFloat(PrefKey("pinScale"),           0.1f);
            DisplacementScale = PlayerPrefs.GetFloat(PrefKey("displacementScale"),  2.0f);
            DisplacementOffset= PlayerPrefs.GetFloat(PrefKey("displacementOffset"), 0.0f);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetInt  (PrefKey("gridWidth"),          gridWidth);
            PlayerPrefs.SetInt  (PrefKey("gridHeight"),         gridHeight);
            PlayerPrefs.SetFloat(PrefKey("pinScale"),           pinScale);
            PlayerPrefs.SetFloat(PrefKey("displacementScale"),  displacementScale);
            PlayerPrefs.SetFloat(PrefKey("displacementOffset"), displacementOffset);
        }

        private void ReleaseBuffers()
        {
            if(transformBuffer != null) { transformBuffer.Release(); transformBuffer = null; }
            if(argsBuffer      != null) { argsBuffer.Release();      argsBuffer      = null; }
            currentGridWidth  = -1;
            currentGridHeight = -1;
        }

        public void Dispose()
        {
            ReleaseBuffers();
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}
