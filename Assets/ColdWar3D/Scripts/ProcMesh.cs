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

    // Four marks of tank, one per hidden power level: light tank, medium tank, heavy twin-gun tank, missile tank.
    public static Mesh Tank(Color team, int mark = 0)
    {
        Color track = new Color(0.10f, 0.10f, 0.11f);
        Color dark = team * 0.7f;
        dark.a = 1f;
        Color steel = new Color(0.32f, 0.34f, 0.37f);
        var m = new ProcMesh();
        switch (mark)
        {
            case 0:
                m.Box(new Vector3(-0.17f, 0.06f, 0f), new Vector3(0.11f, 0.12f, 0.56f), track);
                m.Box(new Vector3(0.17f, 0.06f, 0f), new Vector3(0.11f, 0.12f, 0.56f), track);
                m.Box(new Vector3(0f, 0.14f, 0f), new Vector3(0.30f, 0.12f, 0.50f), team);
                m.Frustum(new Vector3(0f, 0.20f, -0.03f), 0.13f, 0.10f, 0.11f, 7, dark);
                m.Box(new Vector3(0f, 0.25f, 0.21f), new Vector3(0.035f, 0.035f, 0.34f), track);
                break;
            case 1:
                // Medium: longer hull with side skirts, a bigger turret and a long gun with a muzzle brake.
                m.Box(new Vector3(-0.19f, 0.07f, 0f), new Vector3(0.12f, 0.14f, 0.66f), track);
                m.Box(new Vector3(0.19f, 0.07f, 0f), new Vector3(0.12f, 0.14f, 0.66f), track);
                m.Box(new Vector3(-0.25f, 0.11f, 0f), new Vector3(0.02f, 0.08f, 0.6f), dark);
                m.Box(new Vector3(0.25f, 0.11f, 0f), new Vector3(0.02f, 0.08f, 0.6f), dark);
                m.Box(new Vector3(0f, 0.16f, 0f), new Vector3(0.34f, 0.12f, 0.6f), team);
                m.Box(new Vector3(0f, 0.25f, -0.04f), new Vector3(0.24f, 0.09f, 0.28f), dark);
                m.Box(new Vector3(0f, 0.26f, 0.27f), new Vector3(0.04f, 0.04f, 0.46f), track);
                m.Box(new Vector3(0f, 0.26f, 0.5f), new Vector3(0.07f, 0.06f, 0.06f), steel);
                m.Box(new Vector3(0.07f, 0.31f, -0.1f), new Vector3(0.06f, 0.04f, 0.06f), steel);
                break;
            case 2:
                // Heavy: wide tracks, a broad angled turret with two guns and a radio mast.
                m.Box(new Vector3(-0.22f, 0.08f, 0f), new Vector3(0.14f, 0.16f, 0.72f), track);
                m.Box(new Vector3(0.22f, 0.08f, 0f), new Vector3(0.14f, 0.16f, 0.72f), track);
                m.Box(new Vector3(0f, 0.18f, 0f), new Vector3(0.40f, 0.14f, 0.66f), team);
                m.Box(new Vector3(0f, 0.18f, 0.33f), new Vector3(0.40f, 0.04f, 0.04f), Glow(team, 0.6f));
                m.Frustum(new Vector3(0f, 0.25f, -0.05f), 0.19f, 0.14f, 0.12f, 6, dark);
                m.Box(new Vector3(-0.05f, 0.31f, 0.3f), new Vector3(0.035f, 0.035f, 0.5f), track);
                m.Box(new Vector3(0.05f, 0.31f, 0.3f), new Vector3(0.035f, 0.035f, 0.5f), track);
                m.Box(new Vector3(-0.1f, 0.45f, -0.18f), new Vector3(0.012f, 0.28f, 0.012f), steel);
                m.Box(new Vector3(-0.1f, 0.6f, -0.18f), new Vector3(0.03f, 0.03f, 0.03f), Glow(team, 0.9f));
                break;
            default:
                // Missile tank: heavy hull carrying a raised launcher with four glowing missile tips.
                m.Box(new Vector3(-0.22f, 0.08f, 0f), new Vector3(0.14f, 0.16f, 0.76f), track);
                m.Box(new Vector3(0.22f, 0.08f, 0f), new Vector3(0.14f, 0.16f, 0.76f), track);
                m.Box(new Vector3(0f, 0.18f, 0f), new Vector3(0.42f, 0.14f, 0.7f), team);
                m.Box(new Vector3(0f, 0.28f, 0.22f), new Vector3(0.3f, 0.08f, 0.2f), dark);
                m.Box(new Vector3(0f, 0.36f, -0.12f), new Vector3(0.34f, 0.18f, 0.44f), dark);
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.12f + 0.08f * i;
                    m.Box(new Vector3(x, 0.4f, -0.12f), new Vector3(0.05f, 0.05f, 0.46f), steel);
                    m.Box(new Vector3(x, 0.4f, 0.13f), new Vector3(0.04f, 0.04f, 0.04f), Glow(new Color(1f, 0.35f, 0.15f), 0.9f));
                }
                break;
        }

        return m.ToMesh("Tank");
    }

    // Four marks of warship along +z: patrol boat, destroyer, battleship, missile cruiser.
    public static Mesh Ship(Color team, int mark = 0)
    {
        Color hull = new Color(0.30f, 0.32f, 0.36f);
        Color deck = new Color(0.55f, 0.57f, 0.6f);
        Color dark = new Color(0.16f, 0.17f, 0.19f);
        Color white = new Color(0.86f, 0.87f, 0.9f);
        float scale = 0.8f + 0.12f * Mathf.Clamp(mark, 0, 3);
        float half = 0.5f * scale;
        var m = new ProcMesh();
        // Hull: a long body tapering in steps to the bow, with a team-colored stripe at the waterline.
        m.Box(new Vector3(0f, 0.05f, -0.08f * scale), new Vector3(0.24f, 0.1f, 0.8f * scale), hull);
        m.Box(new Vector3(0f, 0.05f, 0.4f * scale), new Vector3(0.16f, 0.1f, 0.18f * scale), hull);
        m.Box(new Vector3(0f, 0.05f, 0.53f * scale), new Vector3(0.08f, 0.1f, 0.1f * scale), hull);
        m.Box(new Vector3(0f, 0.012f, 0f), new Vector3(0.25f, 0.025f, 0.98f * scale), Glow(team, 0.45f));
        m.Box(new Vector3(0f, 0.105f, -0.02f * scale), new Vector3(0.22f, 0.012f, 0.92f * scale), deck);
        // Bridge and funnel amidships.
        m.Box(new Vector3(0f, 0.16f, -0.05f * scale), new Vector3(0.14f, 0.1f, 0.2f * scale), white);
        m.Box(new Vector3(0f, 0.23f, -0.02f * scale), new Vector3(0.1f, 0.05f, 0.1f * scale), white);
        m.Frustum(new Vector3(0f, 0.11f, -0.22f * scale), 0.045f, 0.035f, 0.17f, 8, dark);

        void Turret(float z, float facing)
        {
            m.Frustum(new Vector3(0f, 0.11f, z), 0.06f, 0.05f, 0.05f, 8, deck);
            m.Box(new Vector3(-0.02f, 0.14f, z + facing * 0.09f), new Vector3(0.018f, 0.018f, 0.16f), dark);
            m.Box(new Vector3(0.02f, 0.14f, z + facing * 0.09f), new Vector3(0.018f, 0.018f, 0.16f), dark);
        }

        Turret(half * 0.55f, 1f);
        if (mark >= 1)
            Turret(-half * 0.65f, -1f);
        if (mark >= 2)
        {
            Turret(half * 0.32f, 1f);
            m.Box(new Vector3(0f, 0.33f, -0.04f * scale), new Vector3(0.012f, 0.16f, 0.012f), dark);
            m.Box(new Vector3(0f, 0.4f, -0.04f * scale), new Vector3(0.1f, 0.012f, 0.012f), dark);
        }
        if (mark >= 3)
        {
            // Missile cells behind the bridge, with glowing tips.
            for (int i = 0; i < 4; i++)
            {
                m.Box(new Vector3(-0.045f + 0.03f * i, 0.135f, -0.36f * scale), new Vector3(0.022f, 0.05f, 0.06f), dark);
                m.Box(new Vector3(-0.045f + 0.03f * i, 0.165f, -0.36f * scale), new Vector3(0.02f, 0.012f, 0.02f), Glow(new Color(1f, 0.35f, 0.15f), 0.9f));
            }
        }

        return m.ToMesh("Ship");
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

    // Three marks of satellite: a probe with two solar wings, a larger one with double wings and a big dish,
    // and a station of stacked modules with four wings.
    public static Mesh Satellite(Color team, int mark = 0)
    {
        Color cell = new Color(0.09f, 0.2f, 0.48f);
        Color gold = new Color(0.86f, 0.7f, 0.3f);
        Color white = new Color(0.88f, 0.89f, 0.92f);
        Color strut = new Color(0.55f, 0.57f, 0.6f);
        var m = new ProcMesh();

        // A wing of solar cells along +x or -x (or +z/-z when alongZ), with thin gaps between the cells.
        void Wing(float dir, bool alongZ, int cells, float y, float width)
        {
            float start = 0.12f, size = 0.085f, gap = 0.012f;
            Vector3 axis = alongZ ? new Vector3(0f, 0f, dir) : new Vector3(dir, 0f, 0f);
            Vector3 across = alongZ ? new Vector3(width, 0.012f, size) : new Vector3(size, 0.012f, width);
            m.Box(axis * (start * 0.5f) + Vector3.up * y, alongZ ? new Vector3(0.015f, 0.015f, start) : new Vector3(start, 0.015f, 0.015f), strut);
            for (int i = 0; i < cells; i++)
                m.Box(axis * (start + size * 0.5f + i * (size + gap)) + Vector3.up * y, across, cell);
        }

        if (mark <= 0)
        {
            m.Frustum(new Vector3(0f, -0.08f, 0f), 0.065f, 0.065f, 0.16f, 10, gold);
            Wing(1f, false, 3, 0f, 0.11f);
            Wing(-1f, false, 3, 0f, 0.11f);
            m.Frustum(new Vector3(0f, 0.08f, 0f), 0.015f, 0.07f, 0.04f, 12, white);
            m.Box(new Vector3(0f, 0.15f, 0f), new Vector3(0.008f, 0.06f, 0.008f), strut);
            m.Box(new Vector3(0f, 0.185f, 0f), new Vector3(0.025f, 0.025f, 0.025f), Glow(team, 0.9f));
        }
        else if (mark == 1)
        {
            m.Frustum(new Vector3(0f, -0.11f, 0f), 0.08f, 0.08f, 0.22f, 12, gold);
            m.Frustum(new Vector3(0f, -0.13f, 0f), 0.06f, 0.08f, 0.02f, 12, white);
            foreach (float dir in new[] { -1f, 1f })
            {
                Wing(dir, false, 4, 0.035f, 0.1f);
                Wing(dir, false, 4, -0.035f, 0.1f);
            }
            m.Frustum(new Vector3(0f, 0.11f, 0f), 0.02f, 0.11f, 0.06f, 14, white);
            m.Box(new Vector3(0f, 0.2f, 0f), new Vector3(0.01f, 0.08f, 0.01f), strut);
            m.Box(new Vector3(0f, 0.245f, 0f), new Vector3(0.03f, 0.03f, 0.03f), Glow(team, 0.9f));
        }
        else
        {
            m.Frustum(new Vector3(0f, -0.2f, 0f), 0.07f, 0.07f, 0.14f, 12, white);
            m.Frustum(new Vector3(0f, -0.06f, 0f), 0.1f, 0.1f, 0.1f, 12, gold);
            m.Frustum(new Vector3(0f, 0.04f, 0f), 0.07f, 0.07f, 0.14f, 12, white);
            m.Frustum(new Vector3(0f, -0.02f, 0f), 0.13f, 0.13f, 0.02f, 16, Glow(team, 0.6f));
            foreach (float dir in new[] { -1f, 1f })
            {
                Wing(dir, false, 4, -0.01f, 0.12f);
                Wing(dir, true, 3, 0.1f, 0.1f);
            }
            m.Frustum(new Vector3(0f, 0.18f, 0f), 0.02f, 0.12f, 0.06f, 14, white);
            m.Box(new Vector3(0f, 0.27f, 0f), new Vector3(0.03f, 0.03f, 0.03f), Glow(team, 0.9f));
        }

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

    // A futuristic tower for the far end of the power scale: a tapering spire with glowing bands and a bright tip.
    public static Mesh Spire(Color team)
    {
        Color body = Color.Lerp(team, Color.white, 0.35f);
        Color glow = Glow(Color.Lerp(team, new Color(0.6f, 1f, 1f), 0.5f), 0.95f);
        var m = new ProcMesh();
        m.Frustum(Vector3.zero, 0.42f, 0.3f, 1.2f, 6, body, false);
        m.Frustum(new Vector3(0f, 1.2f, 0f), 0.34f, 0.34f, 0.08f, 6, glow, false);
        m.Frustum(new Vector3(0f, 1.28f, 0f), 0.3f, 0.16f, 1.3f, 6, body, false);
        m.Frustum(new Vector3(0f, 2.58f, 0f), 0.2f, 0.2f, 0.07f, 6, glow, false);
        m.Frustum(new Vector3(0f, 2.65f, 0f), 0.16f, 0.02f, 0.8f, 6, glow);
        return m.ToMesh("Spire");
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

    // The same hex token, lengthened sideways: its left and right points move outward by `stretch`, so the bevels and
    // proportions of the edges stay exactly as they are instead of being squashed.
    public static Mesh WideToken(Color rim, Color face, float stretch)
    {
        Mesh mesh = Token(rim, face);
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].x > 0.01f)
                vertices[i].x += stretch;
            else if (vertices[i].x < -0.01f)
                vertices[i].x -= stretch;
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.name = "WideToken";
        return mesh;
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

    // A long banner hanging from a gold rod, with a gold star and hem. It hangs down from the origin and faces +z.
    public static Mesh Banner(Color team)
    {
        Color gold = new Color(0.95f, 0.74f, 0.24f);
        var m = new ProcMesh();
        m.Box(new Vector3(0f, 0f, 0.03f), new Vector3(0.22f, 0.03f, 0.03f), gold);
        m.Box(new Vector3(0f, -0.47f, 0.02f), new Vector3(0.17f, 0.92f, 0.014f), Glow(team, 0.3f));
        m.Box(new Vector3(0f, -0.915f, 0.03f), new Vector3(0.17f, 0.03f, 0.006f), gold);
        var center = new Vector3(0f, -0.2f, 0.029f);
        for (int k = 0; k < 5; k++)
        {
            float outer = (90f + 72f * k) * Mathf.Deg2Rad;
            float before = outer - 36f * Mathf.Deg2Rad;
            float after = outer + 36f * Mathf.Deg2Rad;
            Vector3 tip = center + new Vector3(Mathf.Cos(outer), Mathf.Sin(outer), 0f) * 0.06f;
            Vector3 a = center + new Vector3(Mathf.Cos(before), Mathf.Sin(before), 0f) * 0.024f;
            Vector3 b = center + new Vector3(Mathf.Cos(after), Mathf.Sin(after), 0f) * 0.024f;
            m.Tri(center, a, tip, gold).Tri(center, tip, a, gold);
            m.Tri(center, tip, b, gold).Tri(center, b, tip, gold);
        }

        return m.ToMesh("Banner");
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
