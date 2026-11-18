using System.Collections.Generic;
using UnityEngine;

// Small low-poly mesh builder for props the asset packs don't cover. Colors go in vertex colors for BoardLit.
public class ProcMesh
{
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Vector3> normals = new List<Vector3>();
    readonly List<Color> colors = new List<Color>();
    readonly List<int> triangles = new List<int>();

    public static Color Glow(Color color, float amount)
    {
        color.a = 1f - amount;
        return color;
    }

    public ProcMesh Box(Vector3 center, Vector3 size, Color color)
    {
        Vector3 h = size * 0.5f;
        Quad(center + new Vector3(-h.x, h.y, -h.z), center + new Vector3(-h.x, h.y, h.z), center + new Vector3(h.x, h.y, h.z), center + new Vector3(h.x, h.y, -h.z), color);
        Quad(center + new Vector3(-h.x, -h.y, h.z), center + new Vector3(-h.x, -h.y, -h.z), center + new Vector3(h.x, -h.y, -h.z), center + new Vector3(h.x, -h.y, h.z), color);
        Quad(center + new Vector3(-h.x, -h.y, -h.z), center + new Vector3(-h.x, h.y, -h.z), center + new Vector3(h.x, h.y, -h.z), center + new Vector3(h.x, -h.y, -h.z), color);
        Quad(center + new Vector3(h.x, -h.y, h.z), center + new Vector3(h.x, h.y, h.z), center + new Vector3(-h.x, h.y, h.z), center + new Vector3(-h.x, -h.y, h.z), color);
        Quad(center + new Vector3(-h.x, -h.y, h.z), center + new Vector3(-h.x, h.y, h.z), center + new Vector3(-h.x, h.y, -h.z), center + new Vector3(-h.x, -h.y, -h.z), color);
        Quad(center + new Vector3(h.x, -h.y, -h.z), center + new Vector3(h.x, h.y, -h.z), center + new Vector3(h.x, h.y, h.z), center + new Vector3(h.x, -h.y, h.z), color);
        return this;
    }

    // A vertical frustum: equal radii make a cylinder, a zero top radius makes a cone.
    public ProcMesh Frustum(Vector3 baseCenter, float baseRadius, float topRadius, float height, int sides, Color color, bool capTop = true)
    {
        for (int i = 0; i < sides; i++)
        {
            float a0 = Mathf.PI * 2f * i / sides;
            float a1 = Mathf.PI * 2f * (i + 1) / sides;
            Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
            Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
            Vector3 up = Vector3.up * height;
            Quad(baseCenter + d0 * baseRadius, baseCenter + up + d0 * topRadius, baseCenter + up + d1 * topRadius, baseCenter + d1 * baseRadius, color);
            if (capTop && topRadius > 0f)
                Tri(baseCenter + up, baseCenter + up + d1 * topRadius, baseCenter + up + d0 * topRadius, color);
        }

        return this;
    }

    public ProcMesh Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
        int start = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        for (int i = 0; i < 4; i++)
        {
            normals.Add(normal);
            colors.Add(color);
        }

        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        return this;
    }

    public ProcMesh Tri(Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
        int start = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c);
        for (int i = 0; i < 3; i++)
        {
            normals.Add(normal);
            colors.Add(color);
        }

        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        return this;
    }

    public Mesh ToMesh(string name)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    public static GameObject Spawn(string name, Mesh mesh, Material material, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    public static Mesh Tank(Color team)
    {
        Color track = new Color(0.10f, 0.10f, 0.11f);
        Color dark = team * 0.7f;
        dark.a = 1f;
        var m = new ProcMesh();
        m.Box(new Vector3(-0.17f, 0.06f, 0f), new Vector3(0.11f, 0.12f, 0.56f), track);
        m.Box(new Vector3(0.17f, 0.06f, 0f), new Vector3(0.11f, 0.12f, 0.56f), track);
        m.Box(new Vector3(0f, 0.14f, 0f), new Vector3(0.30f, 0.12f, 0.50f), team);
        m.Frustum(new Vector3(0f, 0.20f, -0.03f), 0.13f, 0.10f, 0.11f, 7, dark);
        m.Box(new Vector3(0f, 0.25f, 0.21f), new Vector3(0.035f, 0.035f, 0.34f), track);
        return m.ToMesh("Tank");
    }

    public static Mesh Rocket(Color team)
    {
        Color body = new Color(0.86f, 0.87f, 0.88f);
        var m = new ProcMesh();
        m.Frustum(new Vector3(0f, 0f, 0f), 0.09f, 0.09f, 0.72f, 8, body);
        m.Frustum(new Vector3(0f, 0.72f, 0f), 0.09f, 0f, 0.26f, 8, team);
        m.Box(new Vector3(0.11f, 0.08f, 0f), new Vector3(0.09f, 0.16f, 0.02f), team);
        m.Box(new Vector3(-0.11f, 0.08f, 0f), new Vector3(0.09f, 0.16f, 0.02f), team);
        m.Box(new Vector3(0f, 0.08f, 0.11f), new Vector3(0.02f, 0.16f, 0.09f), team);
        m.Box(new Vector3(0f, 0.08f, -0.11f), new Vector3(0.02f, 0.16f, 0.09f), team);
        return m.ToMesh("Rocket");
    }

    public static Mesh Satellite(Color team)
    {
        Color panel = new Color(0.10f, 0.22f, 0.45f);
        var m = new ProcMesh();
        m.Box(Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f), new Color(0.85f, 0.82f, 0.60f));
        m.Box(new Vector3(0.21f, 0f, 0f), new Vector3(0.28f, 0.015f, 0.12f), panel);
        m.Box(new Vector3(-0.21f, 0f, 0f), new Vector3(0.28f, 0.015f, 0.12f), panel);
        m.Box(new Vector3(0f, 0.09f, 0f), new Vector3(0.05f, 0.05f, 0.05f), Glow(team, 0.8f));
        return m.ToMesh("Satellite");
    }

    public static Mesh Silo()
    {
        Color concrete = new Color(0.32f, 0.33f, 0.34f);
        var m = new ProcMesh();
        m.Frustum(Vector3.zero, 0.36f, 0.30f, 0.10f, 8, concrete, false);
        m.Frustum(new Vector3(0f, 0.10f, 0f), 0.30f, 0.30f, 0.005f, 8, new Color(0.03f, 0.03f, 0.03f));
        return m.ToMesh("Silo");
    }

    public static Mesh Column(Color color, int sides = 8)
    {
        // One unit tall, with its pivot at the base, so localScale.y sets the height.
        return new ProcMesh().Frustum(Vector3.zero, 0.5f, 0.5f, 1f, sides, color).ToMesh("Column");
    }

    // One unit tall with its pivot at the base. A bright cap makes the top easy to read against the sea.
    public static Mesh Tower(Color team)
    {
        Color cap = Color.Lerp(team, Color.white, 0.55f);
        var m = new ProcMesh();
        m.Frustum(Vector3.zero, 0.5f, 0.5f, 0.94f, 6, team, false);
        m.Frustum(new Vector3(0f, 0.94f, 0f), 0.5f, 0.5f, 0.06f, 6, cap);
        return m.ToMesh("Tower");
    }

    public static Mesh Platform()
    {
        Color steel = new Color(0.16f, 0.18f, 0.22f);
        return new ProcMesh().Frustum(Vector3.zero, 0.58f, 0.52f, 0.14f, 6, steel).ToMesh("Platform");
    }

    // A flat hex token with one unit radius, its face pointing up.
    public static Mesh Token(Color rim, Color face)
    {
        var m = new ProcMesh();
        m.Frustum(new Vector3(0f, -0.2f, 0f), 1f, 1f, 0.28f, 6, rim, false);
        m.Frustum(new Vector3(0f, 0.08f, 0f), 1f, 0.88f, 0.1f, 6, face);
        return m.ToMesh("Token");
    }

    public static Mesh Glyph(string kind, Color color)
    {
        var m = new ProcMesh();
        Color c = Glow(color, 0.25f);
        const float y = 0.2f;
        switch (kind)
        {
            case "speed":
                m.Tri(new Vector3(-0.5f, y, 0.36f), new Vector3(0f, y, 0f), new Vector3(-0.5f, y, -0.36f), c);
                m.Tri(new Vector3(0f, y, 0.36f), new Vector3(0.5f, y, 0f), new Vector3(0f, y, -0.36f), c);
                break;
            case "pause":
                m.Box(new Vector3(-0.2f, y, 0f), new Vector3(0.2f, 0.02f, 0.72f), c);
                m.Box(new Vector3(0.2f, y, 0f), new Vector3(0.2f, 0.02f, 0.72f), c);
                break;
            case "exit":
                m.Box(new Vector3(-0.08f, y, 0f), new Vector3(0.5f, 0.02f, 0.76f), c);
                m.Tri(new Vector3(0.14f, y + 0.02f, 0.2f), new Vector3(0.56f, y + 0.02f, 0f), new Vector3(0.14f, y + 0.02f, -0.2f), Glow(new Color(1f, 0.85f, 0.3f), 0.4f));
                break;
            case "repeat":
                for (int i = 0; i < 12; i++)
                {
                    float a0 = i * Mathf.PI * 2f / 12f;
                    float a1 = (i + 0.8f) * Mathf.PI * 2f / 12f;
                    Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                    Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                    Vector3 up = Vector3.up * y;
                    m.Quad(up + d1 * 0.32f, up + d1 * 0.48f, up + d0 * 0.48f, up + d0 * 0.32f, c);
                }
                break;
            case "once":
                m.Frustum(new Vector3(0f, y - 0.01f, 0f), 0.3f, 0.3f, 0.02f, 10, c);
                break;
        }

        return m.ToMesh(kind);
    }

    // Points straight down the screen, with its tip at the origin and its length along +Y.
    public static Mesh Arrow(Color color)
    {
        Color c = Glow(color, 0.55f);
        var m = new ProcMesh();
        m.Frustum(Vector3.zero, 0f, 0.62f, 0.85f, 8, c);
        m.Frustum(new Vector3(0f, 0.85f, 0f), 0.26f, 0.26f, 1.1f, 8, c);
        return m.ToMesh("Arrow");
    }

    public static Mesh Coins()
    {
        // Alternating bands read as a stack of separate coins.
        Color gold = new Color(1f, 0.78f, 0.2f);
        Color shade = new Color(0.72f, 0.48f, 0.08f);
        var m = new ProcMesh();
        const int coins = 7;
        for (int i = 0; i < coins; i++)
        {
            float y = i / (float)coins;
            float wobble = (i % 2 == 0) ? 0.5f : 0.47f;
            m.Frustum(new Vector3(0f, y, 0f), wobble, wobble, 0.78f / coins, 12, gold, false);
            m.Frustum(new Vector3(0f, y + 0.78f / coins, 0f), wobble - 0.03f, wobble - 0.03f, 0.22f / coins, 12, shade, i == coins - 1);
        }

        m.Frustum(new Vector3(0f, 1f, 0f), 0.47f, 0.47f, 0.001f, 12, gold);
        return m.ToMesh("Coins");
    }

    public static Mesh Flag(Color team)
    {
        Color pole = new Color(0.8f, 0.8f, 0.8f);
        var m = new ProcMesh();
        m.Box(new Vector3(0f, 0.25f, 0f), new Vector3(0.02f, 0.5f, 0.02f), pole);
        m.Box(new Vector3(0.09f, 0.42f, 0f), new Vector3(0.17f, 0.11f, 0.012f), Glow(team, 0.3f));
        return m.ToMesh("Flag");
    }

    public static Mesh Ring(Color color, float radius, float width, int sides = 28)
    {
        var m = new ProcMesh();
        for (int i = 0; i < sides; i++)
        {
            float a0 = Mathf.PI * 2f * i / sides;
            float a1 = Mathf.PI * 2f * (i + 1) / sides;
            Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
            Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
            m.Quad(d0 * radius, d0 * (radius + width), d1 * (radius + width), d1 * radius, color);
        }

        return m.ToMesh("Ring");
    }
}
