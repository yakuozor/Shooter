using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShooterRange
{
    public sealed class ArenaMap : MonoBehaviour
    {
        public const int GridSize = 15;
        public const float CellSize = 2f;
        public Material floorMaterial, wallMaterial, darkMaterial, accentMaterial, lightMaterial;
        public int Seed { get; private set; }
        public int CoverCount { get; private set; }
        public readonly List<Vector3> SpawnPoints = new List<Vector3>();
        public bool[,] Blocked { get; private set; }
        public Transform Geometry { get; private set; }
        readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public void Generate(int seed)
        {
            Seed = seed;
            CoverCount = 0;
            if (Geometry == null) Geometry = transform.Find("Generated geometry");
            if (Geometry != null)
            {
                Geometry.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(Geometry.gameObject); else DestroyImmediate(Geometry.gameObject);
            }
            Geometry = new GameObject("Generated geometry").transform;
            Geometry.SetParent(transform, false);
            var rng = new System.Random(seed);
            Blocked = new bool[GridSize, GridSize];
            SpawnPoints.Clear();
            int[] edge = { 1, 7, 13 };
            foreach (int x in edge) foreach (int z in edge)
                if (x != 7 || z != 7) SpawnPoints.Add(CellWorld(new Vector2Int(x, z)));
            Cube("Arena floor", new Vector3(0, -.25f, 0), new Vector3(31, .5f, 31), floorMaterial);
            Cube("North wall", new Vector3(0, 2, 15.5f), new Vector3(32, 4, 1), wallMaterial);
            Cube("South wall", new Vector3(0, 2, -15.5f), new Vector3(32, 4, 1), wallMaterial);
            Cube("West wall", new Vector3(-15.5f, 2, 0), new Vector3(1, 4, 30), wallMaterial);
            Cube("East wall", new Vector3(15.5f, 2, 0), new Vector3(1, 4, 30), wallMaterial);
            for (int i = -14; i <= 14; i += 2)
            {
                Cube("Floor joint", new Vector3(i, .003f, 0), new Vector3(.025f, .006f, 30), darkMaterial, false);
                Cube("Floor joint", new Vector3(0, .004f, i), new Vector3(30, .006f, .025f), darkMaterial, false);
            }
            // The central cross, perimeter and spawn clearances are always open.
            // Each proposed island is accepted only if every free grid cell remains connected.
            for (int attempt = 0; attempt < 120 && CoverCount < 25; attempt++)
            {
                int x = rng.Next(2, GridSize - 2), z = rng.Next(2, GridSize - 2);
                int length = rng.Next(1, 3);
                bool horizontal = rng.Next(2) == 0;
                var cells = new List<Vector2Int>();
                bool valid = true;
                for (int n = 0; n < length; n++)
                {
                    var c = new Vector2Int(x + (horizontal ? n : 0), z + (horizontal ? 0 : n));
                    if (!InBounds(c) || c.x <= 1 || c.y <= 1 || c.x >= GridSize - 2 || c.y >= GridSize - 2 || c.x == 7 || c.y == 7 || Blocked[c.x, c.y]) { valid = false; break; }
                    foreach (Vector3 spawn in SpawnPoints) if (Vector3.Distance(spawn, CellWorld(c)) < 3.5f) valid = false;
                    cells.Add(c);
                }
                if (!valid) continue;
                foreach (var c in cells) Blocked[c.x, c.y] = true;
                if (!AllWalkableConnected()) { foreach (var c in cells) Blocked[c.x, c.y] = false; continue; }
                float height = rng.Next(3) == 0 ? 1.25f : 2.5f;
                foreach (var c in cells)
                {
                    Vector3 p = CellWorld(c);
                    Cube("Cover", p + Vector3.up * (height * .5f), new Vector3(1.7f, height, 1.7f), rng.Next(2) == 0 ? darkMaterial : wallMaterial);
                    Cube("Cover edge", p + Vector3.up * (height + .025f), new Vector3(1.74f, .05f, 1.74f), accentMaterial, false);
                    CoverCount++;
                }
            }
            for (int i = 0; i < SpawnPoints.Count; i++)
            {
                Vector3 p = SpawnPoints[i];
                Cube("Spawn pad " + (i + 1), p + Vector3.up * .012f, new Vector3(1.5f, .024f, 1.5f), accentMaterial, false);
            }
            for (int i = -12; i <= 12; i += 6)
            {
                Cube("North wall rib", new Vector3(i, 2, 14.93f), new Vector3(.2f, 4, .2f), darkMaterial);
                Cube("South wall rib", new Vector3(i, 2, -14.93f), new Vector3(.2f, 4, .2f), darkMaterial);
                Cube("North light", new Vector3(i, 3.55f, 14.8f), new Vector3(2, .12f, .08f), lightMaterial, false);
                Cube("South light", new Vector3(i, 3.55f, -14.8f), new Vector3(2, .12f, .08f), lightMaterial, false);
            }
        }

        public Vector3 CellWorld(Vector2Int cell) => new Vector3((cell.x - 7) * CellSize, .04f, (cell.y - 7) * CellSize);
        public Vector2Int WorldCell(Vector3 position) => new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(position.x / CellSize) + 7, 0, GridSize - 1), Mathf.Clamp(Mathf.RoundToInt(position.z / CellSize) + 7, 0, GridSize - 1));
        bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < GridSize && c.y < GridSize;
        public bool IsWalkable(Vector2Int c) => InBounds(c) && !Blocked[c.x, c.y];

        public bool AllWalkableConnected()
        {
            if (Blocked == null) return false;
            var visited = new bool[GridSize, GridSize];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(Vector2Int.zero); visited[0, 0] = true;
            int count = 0, free = 0;
            while (queue.Count > 0)
            {
                var c = queue.Dequeue(); count++;
                foreach (var d in directions)
                {
                    var n = c + d;
                    if (IsWalkable(n) && !visited[n.x, n.y]) { visited[n.x, n.y] = true; queue.Enqueue(n); }
                }
            }
            foreach (bool b in Blocked) if (!b) free++;
            return count == free;
        }

        public List<Vector3> FindPath(Vector3 start, Vector3 goal)
        {
            Vector2Int from = WorldCell(start), to = WorldCell(goal);
            var result = new List<Vector3>();
            if (!IsWalkable(from) || !IsWalkable(to)) return result;
            var seen = new bool[GridSize, GridSize];
            var previous = new Vector2Int[GridSize, GridSize];
            var queue = new Queue<Vector2Int>();
            seen[from.x, from.y] = true; queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c == to) break;
                foreach (var d in directions)
                {
                    var n = c + d;
                    if (IsWalkable(n) && !seen[n.x, n.y]) { seen[n.x, n.y] = true; previous[n.x, n.y] = c; queue.Enqueue(n); }
                }
            }
            if (!seen[to.x, to.y]) return result;
            var at = to;
            while (at != from) { result.Add(CellWorld(at)); at = previous[at.x, at.y]; }
            result.Reverse();
            return result;
        }

        void Cube(string name, Vector3 position, Vector3 scale, Material material, bool collision = true)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(Geometry, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision)
            {
                obj.GetComponent<Collider>().enabled = false;
                if (Application.isPlaying) Destroy(obj.GetComponent<Collider>()); else DestroyImmediate(obj.GetComponent<Collider>());
            }
        }
    }
}
