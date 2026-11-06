using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Renders the land as one mesh and flips tiles along the front line as territory changes hands.
public class HexBoard : MonoBehaviour
{
    public static readonly Color BlueTop = new Color(0.14f, 0.36f, 0.80f);
    public static readonly Color RedTop = new Color(0.82f, 0.16f, 0.13f);
    static readonly Color BlueSide = new Color(0.07f, 0.16f, 0.38f);
    static readonly Color RedSide = new Color(0.38f, 0.07f, 0.07f);
    static readonly Color AshTop = new Color(0.20f, 0.18f, 0.17f);
    static readonly Color SeaColor = new Color(0.035f, 0.075f, 0.14f);

    const int VertsPerTile = 31;
    const float FlipSeconds = 0.55f;
    const float FrontWidth = 0.018f;

    BoardData data;
    Mesh mesh;
    Vector3[] vertices;
    Color[] colors;
    Vector3[] unitHex;

    bool[] isRed;
    float[] flip;
    float[] shadeJitter;
    float[] raise;
    float[] burn;
    readonly List<int> animating = new List<int>();
    float lastCom = -1f;
    bool dirty;

    public int TileCount => data.land.Length;
    public float Com { get; private set; }

    public void Build(BoardData boardData, Material material)
    {
        data = boardData;
        unitHex = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i + 30f);
            unitHex[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * data.hexRadius * 0.96f;
        }

        BuildLand(material);
        BuildSea(material);
    }

    void BuildLand(Material material)
    {
        int count = data.land.Length;
        vertices = new Vector3[count * VertsPerTile];
        colors = new Color[vertices.Length];
        var normals = new Vector3[vertices.Length];
        var triangles = new int[count * (6 + 12) * 3];
        isRed = new bool[count];
        flip = new float[count];
        shadeJitter = new float[count];
        raise = new float[count];
        burn = new float[count];

        int t = 0;
        for (int i = 0; i < count; i++)
        {
            shadeJitter[i] = Random.Range(0.90f, 1.08f);
            int b = i * VertsPerTile;
            normals[b] = Vector3.up;
            for (int s = 0; s < 6; s++)
            {
                normals[b + 1 + s] = Vector3.up;
                triangles[t++] = b;
                triangles[t++] = b + 1 + (s + 1) % 6;
                triangles[t++] = b + 1 + s;

                Vector3 normal = (unitHex[s] + unitHex[(s + 1) % 6]).normalized;
                int q = b + 7 + s * 4;
                normals[q] = normals[q + 1] = normals[q + 2] = normals[q + 3] = normal;
                triangles[t++] = q;
                triangles[t++] = q + 1;
                triangles[t++] = q + 2;
                triangles[t++] = q;
                triangles[t++] = q + 2;
                triangles[t++] = q + 3;
            }

            WriteTile(i);
        }

        mesh = new Mesh { name = "HexBoardLand", indexFormat = IndexFormat.UInt32 };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.bounds = new Bounds(Vector3.up, new Vector3(data.boardSize.x + 4f, 6f, data.boardSize.y + 4f));

        CreateRenderer("Land", mesh, material, true);
    }

    void BuildSea(Material material)
    {
        int count = data.sea.Length;
        var seaVertices = new Vector3[count * 7];
        var seaNormals = new Vector3[seaVertices.Length];
        var seaColors = new Color[seaVertices.Length];
        var triangles = new int[count * 18];

        int t = 0;
        var random = new System.Random(7);
        for (int i = 0; i < count; i++)
        {
            Vector3 center = new Vector3(data.sea[i].x, -0.02f, data.sea[i].y);
            Color color = SeaColor * (0.92f + (float)random.NextDouble() * 0.22f);
            color.a = 1f;
            int b = i * 7;
            seaVertices[b] = center;
            seaNormals[b] = Vector3.up;
            seaColors[b] = color;
            for (int s = 0; s < 6; s++)
            {
                seaVertices[b + 1 + s] = center + unitHex[s];
                seaNormals[b + 1 + s] = Vector3.up;
                seaColors[b + 1 + s] = color;
                triangles[t++] = b;
                triangles[t++] = b + 1 + (s + 1) % 6;
                triangles[t++] = b + 1 + s;
            }
        }

        var seaMesh = new Mesh { name = "HexBoardSea", indexFormat = IndexFormat.UInt32 };
        seaMesh.vertices = seaVertices;
        seaMesh.normals = seaNormals;
        seaMesh.colors = seaColors;
        seaMesh.triangles = triangles;
        seaMesh.RecalculateBounds();

        CreateRenderer("Sea", seaMesh, material, false);
    }

    void CreateRenderer(string childName, Mesh childMesh, Material material, bool castShadows)
    {
        var child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        child.AddComponent<MeshFilter>().sharedMesh = childMesh;
        var meshRenderer = child.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
    }

    void WriteTile(int index)
    {
        BoardTile tile = data.land[index];
        int b = index * VertsPerTile;

        float progress = Mathf.Max(0f, flip[index]);
        float pop = Mathf.Sin(progress * Mathf.PI) * 0.7f;
        float height = tile.elevation + raise[index] + pop;
        var center = new Vector3(tile.position.x, 0f, tile.position.y);
        Vector3 top = center + Vector3.up * height;
        Vector3 bottom = center + Vector3.down * 0.4f;

        Color topColor = isRed[index] ? Color.Lerp(RedTop, BlueTop, progress) : Color.Lerp(BlueTop, RedTop, progress);
        Color sideColor = isRed[index] ? Color.Lerp(RedSide, BlueSide, progress) : Color.Lerp(BlueSide, RedSide, progress);
        topColor *= shadeJitter[index];
        topColor = Color.Lerp(topColor, AshTop, burn[index]);
        sideColor = Color.Lerp(sideColor, AshTop * 0.5f, burn[index]);

        // Vertex alpha drives the shader's glow, which marks the front line.
        float glow = raise[index] > 0.001f ? 0.86f : 1f;
        glow = Mathf.Min(glow, 1f - Mathf.Sin(progress * Mathf.PI) * 0.6f);
        topColor.a = burn[index] > 0.5f ? 1f : glow;
        sideColor.a = 1f;

        vertices[b] = top;
        colors[b] = topColor;
        for (int s = 0; s < 6; s++)
        {
            Vector3 corner = unitHex[s];
            Vector3 nextCorner = unitHex[(s + 1) % 6];
            vertices[b + 1 + s] = top + corner;
            colors[b + 1 + s] = topColor;

            int q = b + 7 + s * 4;
            vertices[q] = top + corner;
            vertices[q + 1] = top + nextCorner;
            vertices[q + 2] = bottom + nextCorner;
            vertices[q + 3] = bottom + corner;
            colors[q] = colors[q + 1] = colors[q + 2] = colors[q + 3] = sideColor;
        }
    }

    // Applies territory instantly, with no flip animation.
    public void SnapTo(float com)
    {
        Com = Mathf.Clamp01(com);
        lastCom = Com;
        for (int i = 0; i < data.land.Length; i++)
        {
            isRed[i] = data.land[i].redThreshold < Com;
            flip[i] = 0f;
            raise[i] = IsFront(i) ? 0.18f : 0f;
            WriteTile(i);
        }

        animating.Clear();
        dirty = true;
    }

    public void SetCom(float com)
    {
        Com = Mathf.Clamp01(com);
        if (Mathf.Approximately(Com, lastCom))
            return;

        float low = Mathf.Min(Com, lastCom) - FrontWidth * 2f;
        float high = Mathf.Max(Com, lastCom) + FrontWidth * 2f;
        lastCom = Com;

        for (int i = 0; i < data.land.Length; i++)
        {
            float threshold = data.land[i].redThreshold;
            if (threshold < low || threshold > high)
                continue;

            bool shouldBeRed = threshold < Com;
            if (shouldBeRed != isRed[i] && flip[i] <= 0f)
            {
                // Negative progress staggers the wave so the front ripples instead of snapping.
                flip[i] = -Random.Range(0f, 0.25f) / FlipSeconds;
                animating.Add(i);
            }

            float targetRaise = IsFront(i) ? 0.18f : 0f;
            if (!Mathf.Approximately(raise[i], targetRaise))
            {
                raise[i] = targetRaise;
                WriteTile(i);
                dirty = true;
            }
        }
    }

    bool IsFront(int index)
    {
        return Mathf.Abs(data.land[index].redThreshold - Com) < FrontWidth;
    }

    // Spreads scorched earth outward from a point, for the end of the world.
    public void Burn(Vector2 origin, float radius)
    {
        for (int i = 0; i < data.land.Length; i++)
        {
            float distance = Vector2.Distance(data.land[i].position, origin);
            float amount = Mathf.Clamp01((radius - distance) / 2f);
            if (amount <= burn[i])
                continue;

            burn[i] = amount;
            WriteTile(i);
            dirty = true;
        }
    }

    void Update()
    {
        float step = Time.unscaledDeltaTime / FlipSeconds;
        for (int n = animating.Count - 1; n >= 0; n--)
        {
            int i = animating[n];
            flip[i] += step;
            if (flip[i] >= 1f)
            {
                isRed[i] = data.land[i].redThreshold < Com;
                flip[i] = 0f;
                animating.RemoveAt(n);
                WriteTile(i);
            }
            else if (flip[i] > 0f)
            {
                WriteTile(i);
            }

            dirty = true;
        }

        if (!dirty)
            return;

        dirty = false;
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
    }

    public Vector3 TileTop(int index)
    {
        BoardTile tile = data.land[index];
        return new Vector3(tile.position.x, tile.elevation + raise[index], tile.position.y);
    }

    public float ThresholdOf(int index)
    {
        return data.land[index].redThreshold;
    }

    public int NearestTile(Vector2 point)
    {
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < data.land.Length; i++)
        {
            float distance = (data.land[i].position - point).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }
}
