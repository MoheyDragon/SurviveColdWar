using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Turns the 2D world map sprites into hex board data and wires up the runtime art references.
public static class ColdWarBaker
{
    const string Root = "Assets/ColdWar3D";
    const string MainScene = "Assets/Scenes/Main.unity";
    const int Columns = 60;

    // Map positions as fractions of the map bounds, with (0, 0) at the top left.
    static readonly Vector2 Washington = new Vector2(0.25f, 0.41f);
    static readonly Vector2 Moscow = new Vector2(0.63f, 0.305f);
    static readonly Vector2 BlueSilos = new Vector2(0.187f, 0.36f);
    static readonly Vector2 RedSilos = new Vector2(0.755f, 0.27f);

    // Tiles lean toward whichever side's nearest stronghold is closer, which draws a believable Iron Curtain.
    static readonly Vector2[] BlueStrongholds =
    {
        Washington,
        new Vector2(0.38f, 0.10f),  // Greenland
        new Vector2(0.54f, 0.335f), // London
        new Vector2(0.28f, 0.72f),  // Rio de Janeiro
        new Vector2(0.61f, 0.72f),  // Johannesburg
        new Vector2(0.87f, 0.44f),  // Tokyo
        new Vector2(0.90f, 0.77f),  // Sydney
    };

    static readonly Vector2[] RedStrongholds =
    {
        Moscow,
        new Vector2(0.817f, 0.41f), // Beijing
    };

    struct MapLayer
    {
        public Color32[] pixels;
        public int width;
        public int height;
        public Rect uvRect;
        public Rect screenRect;
    }

    [MenuItem("Cold War/Rebuild 3D Board")]
    public static string Rebuild()
    {
        Directory.CreateDirectory(Root + "/Generated");
        Directory.CreateDirectory(Root + "/Resources");
        AssetDatabase.Refresh();

        BoardData data = BakeBoard();
        ConvertToUrp("Assets/Missiles_Pack");
        ConvertToUrp("Assets/VoodooPlay");
        BoardArt art = BuildArt(data);

        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(art);
        AssetDatabase.SaveAssets();
        return $"land={data.land.Length} sea={data.sea.Length} size={data.boardSize} factories={art.factories.Length} " +
               $"missile={art.missile != null} drone={art.drone != null}";
    }

    static BoardData BakeBoard()
    {
        List<MapLayer> layers = ReadMapLayers(out Rect bounds);
        float aspect = bounds.height / bounds.width;

        const float radius = 0.5f;
        float colStep = Mathf.Sqrt(3f) * radius;
        float rowStep = 1.5f * radius;
        float width = Columns * colStep;
        float height = width * aspect;
        int rows = Mathf.CeilToInt(height / rowStep);

        var candidates = new List<BoardTile>();
        var sea = new List<Vector2>();
        const int seaMargin = 7;

        for (int row = -seaMargin; row < rows + seaMargin; row++)
        {
            for (int col = -seaMargin; col < Columns + seaMargin; col++)
            {
                float x = col * colStep + ((row & 1) != 0 ? colStep * 0.5f : 0f);
                float y = row * rowStep;
                Vector2 normalized = new Vector2(x / width, y / height);
                var position = new Vector2((normalized.x - 0.5f) * width, (0.5f - normalized.y) * height);

                if (CoverageAt(layers, bounds, normalized, radius / width) < 0.42f)
                {
                    sea.Add(position);
                    continue;
                }

                candidates.Add(new BoardTile
                {
                    position = position,
                    redThreshold = RedLean(normalized),
                    elevation = 0.18f + 0.62f * Mathf.PerlinNoise(x * 0.16f + 3.1f, y * 0.16f + 7.7f),
                });
            }
        }

        // Rank tiles so the value of Com equals the share of land that is Red.
        candidates.Sort((a, b) => b.redThreshold.CompareTo(a.redThreshold));
        for (int i = 0; i < candidates.Count; i++)
        {
            BoardTile tile = candidates[i];
            tile.redThreshold = (i + 0.5f) / candidates.Count;
            candidates[i] = tile;
        }

        string path = Root + "/Generated/BoardData.asset";
        var data = AssetDatabase.LoadAssetAtPath<BoardData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BoardData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.hexRadius = radius;
        data.boardSize = new Vector2(width, height);
        data.land = candidates.ToArray();
        data.sea = sea.ToArray();
        data.blueCapital = ToBoard(Washington, width, height);
        data.redCapital = ToBoard(Moscow, width, height);
        data.blueSilos = ToBoard(BlueSilos, width, height);
        data.redSilos = ToBoard(RedSilos, width, height);
        return data;
    }

    static Vector2 ToBoard(Vector2 normalized, float width, float height)
    {
        return new Vector2((normalized.x - 0.5f) * width, (0.5f - normalized.y) * height);
    }

    static float RedLean(Vector2 normalized)
    {
        float toBlue = NearestDistance(normalized, BlueStrongholds);
        float toRed = NearestDistance(normalized, RedStrongholds);
        return toBlue / (toBlue + toRed + 0.0001f);
    }

    static float NearestDistance(Vector2 point, Vector2[] sites)
    {
        float best = float.MaxValue;
        foreach (Vector2 site in sites)
            best = Mathf.Min(best, Vector2.Distance(point, site));
        return best;
    }

    static float CoverageAt(List<MapLayer> layers, Rect bounds, Vector2 normalized, float sampleRadius)
    {
        const int samples = 7;
        int hits = 0;
        for (int i = 0; i < samples; i++)
        {
            Vector2 offset = Vector2.zero;
            if (i > 0)
            {
                float angle = (i - 1) * Mathf.PI / 3f;
                offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * sampleRadius * 0.7f;
            }

            Vector2 n = normalized + offset;
            var screen = new Vector2(bounds.xMin + n.x * bounds.width, bounds.yMax - n.y * bounds.height);
            if (IsLand(layers, screen))
                hits++;
        }

        return hits / (float)samples;
    }

    static bool IsLand(List<MapLayer> layers, Vector2 screen)
    {
        foreach (MapLayer layer in layers)
        {
            if (!layer.screenRect.Contains(screen))
                continue;

            Vector2 uv = Rect.PointToNormalized(layer.screenRect, screen);
            int px = Mathf.Clamp((int)(layer.uvRect.x + uv.x * layer.uvRect.width), 0, layer.width - 1);
            int py = Mathf.Clamp((int)(layer.uvRect.y + uv.y * layer.uvRect.height), 0, layer.height - 1);
            if (layer.pixels[py * layer.width + px].a > 96)
                return true;
        }

        return false;
    }

    // Reads the sprites and their placement from the Main scene without saving the scene.
    static List<MapLayer> ReadMapLayers(out Rect bounds)
    {
        var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Additive);
        var layers = new List<MapLayer>();
        bounds = default;

        try
        {
            Transform map = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                map = root.transform.Find("Map");
                if (map != null)
                    break;
            }

            if (map == null)
                throw new System.InvalidOperationException("Canvas/Map was not found in the Main scene.");

            bool hasBounds = false;
            var corners = new Vector3[4];
            foreach (string layerName in new[] { "Com", "Cap", "CapSouth" })
            {
                var image = map.Find(layerName).GetComponent<Image>();
                string spritePath = AssetDatabase.GetAssetPath(image.sprite.texture);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(spritePath));

                image.rectTransform.GetWorldCorners(corners);
                var screenRect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
                layers.Add(new MapLayer
                {
                    pixels = texture.GetPixels32(),
                    width = texture.width,
                    height = texture.height,
                    uvRect = new Rect(0, 0, texture.width, texture.height),
                    screenRect = screenRect,
                });
                Object.DestroyImmediate(texture);

                bounds = hasBounds
                    ? Rect.MinMaxRect(
                        Mathf.Min(bounds.xMin, screenRect.xMin), Mathf.Min(bounds.yMin, screenRect.yMin),
                        Mathf.Max(bounds.xMax, screenRect.xMax), Mathf.Max(bounds.yMax, screenRect.yMax))
                    : screenRect;
                hasBounds = true;
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        return layers;
    }

    static void ConvertToUrp(string folder)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material.shader == lit)
                continue;

            Texture albedo = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            material.shader = lit;
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
        }
    }

    static BoardArt BuildArt(BoardData data)
    {
        string path = Root + "/Resources/BoardArt.asset";
        var art = AssetDatabase.LoadAssetAtPath<BoardArt>(path);
        if (art == null)
        {
            art = ScriptableObject.CreateInstance<BoardArt>();
            AssetDatabase.CreateAsset(art, path);
        }

        art.data = data;
        art.boardMaterial = GetMaterial("M_Board", Shader.Find("ColdWar/BoardLit"), _ => { });
        art.particleAlpha = GetMaterial("M_ParticleAlpha", Shader.Find("Universal Render Pipeline/Particles/Unlit"), m => SetupParticle(m, false));
        art.particleAdditive = GetMaterial("M_ParticleAdditive", Shader.Find("Universal Render Pipeline/Particles/Unlit"), m => SetupParticle(m, true));

        const string buildings = "Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_";
        art.factories = LoadPrefabs(buildings, "Factory_01", "Factory_02", "Factory_03", "Factory_04", "Factory_05", "Factory_06", "Factory_07");
        art.blueCity = LoadPrefabs(buildings, "Skyscraper_01", "Skyscraper_02", "Office_01", "Office_02");
        art.redCity = LoadPrefabs(buildings, "Skyscraper_03", "Skyscraper_04", "Office_01", "Office_02");
        art.missile = LoadPrefab("Assets/Missiles_Pack", "Missil_01");
        art.drone = LoadPrefab("Assets/VoodooPlay", "Drone");
        return art;
    }

    static Material GetMaterial(string name, Shader shader, System.Action<Material> setup)
    {
        string path = Root + "/Generated/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        setup(material);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void SetupParticle(Material material, bool additive)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", additive ? 2f : 0f);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_SrcBlend", additive ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.SetOverrideTag("RenderType", "Transparent");
    }

    static GameObject[] LoadPrefabs(string prefix, params string[] names)
    {
        var prefabs = new List<GameObject>();
        foreach (string name in names)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefix + name + ".prefab");
            if (prefab != null)
                prefabs.Add(prefab);
            else
                Debug.LogWarning("[ColdWarBaker] Missing prefab: " + prefix + name);
        }

        return prefabs.ToArray();
    }

    static GameObject LoadPrefab(string folder, string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Prefab", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        Debug.LogWarning("[ColdWarBaker] Missing prefab: " + name);
        return null;
    }
}
