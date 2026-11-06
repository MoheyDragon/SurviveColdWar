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

    public static Mesh Coins()
    {
        Color gold = Glow(new Color(0.98f, 0.73f, 0.16f), 0.12f);
        return new ProcMesh().Frustum(Vector3.zero, 0.5f, 0.5f, 1f, 12, gold).ToMesh("Coins");
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
