using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity 功能表：Tools / Pokemon Capture / Build Scene
/// 一鍵建立所有場景配置、材質、Prefab、光源、攝影機及可執行動畫。
/// </summary>
public static class BuildCaptureScene
{
    private const string ScenePath = "Assets/Scenes/CaptureDemo.unity";
    private const string MatFolder = "Assets/Materials";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string ModelFolder = "Assets/Models";
    private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

    [MenuItem("Tools/Pokemon Capture/1 - Build and Open Scene")]
    public static void BuildAndOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Pokemon Capture", "請先停止 Play 模式。", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        MakeDirectory(MatFolder);
        MakeDirectory(PrefabFolder);
        MakeDirectory("Assets/Scenes");
        MakeDirectory("Assets/Generated");

        string[] paths = {
            ModelFolder + "/Trainer.obj", ModelFolder + "/TrainerThrowArm.obj",
            ModelFolder + "/ElectricBuddy.obj", ModelFolder + "/PokeBall.obj"
        };
        foreach (string path in paths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                EditorUtility.DisplayDialog("Missing model", "找不到模型：" + path + "\n請確認整個 Assets/Models 都已複製。", "OK");
                return;
            }
        }

        Mats.Clear();
        Material grass = Mat("Meadow", "#74B96B");
        Material grassLight = Mat("Meadow Light", "#A5D77B");
        Material grassDark = Mat("Grass Blade", "#438E52");
        Material hill = Mat("Distant Hill", "#629A8C");
        Material hillLight = Mat("Distant Hill Light", "#7FBCA2");
        Material bark = Mat("Trunk", "#865A46");
        Material leaf = Mat("Leaf", "#398E69");
        Material leafBright = Mat("Leaf Bright", "#53A779");
        Material stone = Mat("Stone", "#E4E6CE");
        Material cloud = Mat("Cloud", "#F7FCFE");
        Material pathMat = Mat("Dirt Path", "#D3BE86");

        // OBJ 自帶 MTL 顏色；另外建立可換材質。
        string[] colorNames = {
            "skin:#F5BE94", "hair:#593E30", "cap_red:#EA535A", "cap_white:#F8F5DF",
            "jacket:#3C80BD", "pants:#334363", "shoes:#282E3A", "backpack:#C15D56",
            "dark:#30303A", "yellow:#F9D84E", "cheek:#F17C77", "ear:#4A3933",
            "white:#F6F4EA", "red:#E95858", "black:#262932", "silver:#D0D7D9"
        };
        foreach (string pair in colorNames)
        {
            string[] spl = pair.Split(':');
            Mat(spl[0], spl[1]);
        }
        // 將 .obj 的內部材質直接對應到專案內的 Standard 材質。
        // 即使各 Unity 版本對 .mtl 的匯入方式不同，也盡量保留角色原色。
        foreach (string path in paths) RemapModelMaterials(path);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.76f, 0.86f, 0.93f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.75f, 0.88f, 0.87f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 25f;
        RenderSettings.fogEndDistance = 65f;

        // 相機：從左到右一覽訓練家與寶可夢。
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        cameraGo.AddComponent<AudioListener>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.66f, 0.85f, 0.95f);
        camera.fieldOfView = 53f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 150f;
        cameraGo.transform.position = new Vector3(0f, 4.8f, -17.5f);
        cameraGo.transform.LookAt(new Vector3(0f, 1.5f, 2f));
        camera.allowHDR = true;

        var sun = new GameObject("Directional Light - Afternoon Sun");
        Light sunlight = sun.AddComponent<Light>();
        sunlight.type = LightType.Directional;
        sunlight.intensity = 1.25f;
        sunlight.color = new Color(1f, 0.95f, 0.81f);
        sunlight.shadows = LightShadows.Soft;
        sunlight.shadowStrength = 0.62f;
        sun.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

        GameObject environment = new GameObject("ENVIRONMENT - grass, trees and mountains");
        CreatePrimitive("Meadow floor", PrimitiveType.Cube, environment.transform,
            new Vector3(0f, -0.30f, 5f), new Vector3(48f, 0.5f, 49f), grass);
        CreatePrimitive("Warm dirt path", PrimitiveType.Cylinder, environment.transform,
            new Vector3(-0.3f, 0.005f, 1.5f), new Vector3(5.3f, 0.012f, 1.65f), pathMat)
            .transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        CreatePrimitive("Trainer circle", PrimitiveType.Cylinder, environment.transform,
            new Vector3(-4.6f, 0.014f, 0f), new Vector3(1.75f, 0.02f, 1.05f), grassLight);
        CreatePrimitive("Pokemon circle", PrimitiveType.Cylinder, environment.transform,
            new Vector3(3.7f, 0.018f, 0f), new Vector3(1.75f, 0.02f, 1.05f), grassLight);

        CreateMountainMesh("Far hills", environment.transform, hillLight, 14f, 2.3f, 4.4f);
        CreateMountainMesh("Tall distant mountains", environment.transform, hill, 29f, 3.7f, 7.1f);
        CreateTrees(environment.transform, bark, leaf, leafBright);
        CreateGrass(environment.transform, grassDark, grassLight);
        CreateRocks(environment.transform, stone);
        CreateClouds(environment.transform, cloud);

        GameObject cast = new GameObject("CHARACTERS - model prefabs");
        GameObject trainer = AddModelPrefab("Trainer", "Trainer.obj", cast.transform,
            new Vector3(-4.8f, 0.02f, 0f), 1f);
        // The trainer faces -Z. In Unity, anatomical RIGHT is local -X.
        // Imported OBJ X has been pre-corrected by Source/make_models.py.
        // Right shoulder must be at -0.59 (not +0.59).
        // Rotating its pivot now moves the sleeve AND the hand without deforming
        // the trainer's head/body or depending on OBJ importer group settings.
        GameObject armPivotObject = new GameObject("RIGHT SHOULDER PIVOT - Animated Throw Arm");
        Transform armPivot = armPivotObject.transform;
        armPivot.SetParent(trainer.transform, false);
        armPivot.localPosition = new Vector3(-0.59f, 1.88f, -0.04f);
        armPivot.localRotation = Quaternion.identity;

        GameObject armSource = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/TrainerThrowArm.obj");
        GameObject armModel = PrefabUtility.InstantiatePrefab(armSource) as GameObject;
        if (armModel == null) throw new Exception("Unable to load throwing arm OBJ");
        armModel.name = "Throwing Right Arm and Hand";
        armModel.transform.SetParent(armPivot, false);
        armModel.transform.localPosition = Vector3.zero;
        armModel.transform.localRotation = Quaternion.identity;
        armModel.transform.localScale = Vector3.one;
        ConfigureModelRenderers(armModel);

        // A marker fixed to the palm. The ball follows it while the arm winds up.
        GameObject throwOrigin = new GameObject("THROW POINT - In Right Hand");
        throwOrigin.transform.SetParent(armPivot, false);
        // Right palm sits at approximately local (-0.10, -0.92, -0.09).
        // A small -Z offset puts the ball in front of the visible hand.
        throwOrigin.transform.localPosition = new Vector3(-0.10f, -0.92f, -0.31f);
        throwOrigin.transform.localRotation = Quaternion.identity;

        // Re-save the Trainer prefab with the new animated shoulder hierarchy.
        PrefabUtility.SaveAsPrefabAsset(trainer, PrefabFolder + "/Trainer.prefab");

        GameObject pokemon = AddModelPrefab("Pokemon - Electric Buddy", "ElectricBuddy.obj", cast.transform,
            new Vector3(3.7f, 0.04f, 0f), 1f);
        GameObject ball = AddModelPrefab("PokeBall", "PokeBall.obj", cast.transform,
            new Vector3(-3.7f, 1.65f, -0.65f), 0.80f);
        ball.SetActive(false);

        var controllerObject = new GameObject("GAME CONTROLLER - Press Space / Click");
        var controller = controllerObject.AddComponent<CaptureDemoController>();
        controller.trainer = trainer;
        controller.pokemon = pokemon;
        controller.pokeBall = ball;
        controller.throwPoint = throwOrigin.transform;
        controller.throwArmPivot = armPivot;
        controller.armRaiseDuration = 0.45f;
        controller.releaseAngle = 115f; // +angle magnitude; controller rotates negative Z to raise RIGHT hand
        controller.releaseHoldDuration = 0.06f;
        controller.armReturnDuration = 0.60f;
        controller.heldBallScale = 0.72f;
        controller.throwDuration = 1.20f;
        controller.arcHeight = 3.0f;

        EditorSceneManager.MarkSceneDirty(scene);
        bool success = EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (success)
        {
            Debug.Log("[Pokemon Capture] Scene built at " + ScenePath + ". Press Play, then Space / Left Click. R to reset.");
            EditorUtility.DisplayDialog("場景已建立！",
                "完成！\n\n1. 按上方 ▶ Play\n2. Space / 左鍵：右手伸直舉起後丟球\n3. R：重新開始\n\n可到 Controller Inspector 調整右臂舉起速度與角度。", "開始");
        }
        else Debug.LogError("[Pokemon Capture] Unable to save Scene. Check console.");
    }

    [MenuItem("Tools/Pokemon Capture/2 - Open Existing Scene")]
    private static void OpenScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog("Pokemon Capture", "尚未建立場景，請先點選 1 - Build and Open Scene。", "OK");
            return;
        }
        EditorSceneManager.OpenScene(ScenePath);
    }

    private static void RemapModelMaterials(string path)
    {
        AssetImporter importer = AssetImporter.GetAtPath(path);
        if (importer == null) return;
        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        bool changed = false;
        foreach (UnityEngine.Object sub in subAssets)
        {
            Material original = sub as Material;
            if (original == null) continue;
            string key = original.name.ToLowerInvariant();
            if (!Mats.TryGetValue(key, out Material material)) continue;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(original), material);
            changed = true;
        }
        if (changed)
        {
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }

    private static void MakeDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        string leaf = path.Substring(path.LastIndexOf('/') + 1);
        if (!AssetDatabase.IsValidFolder(parent)) MakeDirectory(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static Material Mat(string name, string hex)
    {
        Material existing;
        if (Mats.TryGetValue(name, out existing)) return existing;
        string path = MatFolder + "/" + name + ".mat";
        existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        ColorUtility.TryParseHtmlString(hex, out Color color);
        if (existing == null)
        {
            // 本示範以 Unity「3D (Built-In)」建立；URP/Lit 也可指定顏色。
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            existing = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(existing, path);
        }
        if (existing.HasProperty("_Color")) existing.SetColor("_Color", color);
        if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
        if (existing.HasProperty("_Glossiness")) existing.SetFloat("_Glossiness", 0.26f);
        Mats[name] = existing;
        return existing;
    }

    private static GameObject AddModelPrefab(string name, string assetName, Transform parent,
                                             Vector3 position, float scale)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + assetName);
        GameObject outer = new GameObject(name);
        outer.transform.position = position;
        outer.transform.SetParent(parent, true);
        GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (model == null) throw new Exception("Failed to load 3D model: " + assetName);
        model.transform.SetParent(outer.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        outer.transform.localScale = Vector3.one * scale;
        ConfigureModelRenderers(model);
        string prefabPath = PrefabFolder + "/" + assetName.Replace(".obj", "") + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(outer, prefabPath);
        return outer;
    }

    // Shared material assignment for the body model and the separate throwing arm.
    private static void ConfigureModelRenderers(GameObject model)
    {
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            Material[] original = renderer.sharedMaterials;
            Material[] mapped = new Material[original.Length];
            for (int i = 0; i < original.Length; i++)
            {
                string key = original[i] != null ? original[i].name.ToLowerInvariant() : "";
                key = key.Replace(" (instance)", "").Replace("_mat", "");
                if (Mats.TryGetValue(key, out Material material)) mapped[i] = material;
                else mapped[i] = original[i];
            }
            renderer.sharedMaterials = mapped;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent,
                                              Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        var collider = go.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        return go;
    }

    private static void CreateTrees(Transform parent, Material bark, Material leaf, Material leafBright)
    {
        GameObject template = new GameObject("Tree Template");
        CreatePrimitive("wood trunk", PrimitiveType.Cylinder, template.transform,
            new Vector3(0f, 0.92f, 0f), new Vector3(0.23f, 0.94f, 0.23f), bark);
        CreatePrimitive("big leaves", PrimitiveType.Sphere, template.transform,
            new Vector3(0f, 2.1f, 0f), new Vector3(1.55f, 1.28f, 1.50f), leaf);
        CreatePrimitive("upper leaves", PrimitiveType.Sphere, template.transform,
            new Vector3(0.27f, 2.63f, 0f), new Vector3(1.12f, 1.20f, 1.18f), leafBright);
        PrefabUtility.SaveAsPrefabAsset(template, PrefabFolder + "/Tree.prefab");
        var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Tree.prefab");
        UnityEngine.Object.DestroyImmediate(template);

        // 排在左右兩側，避開中間的拋球動線。
        Vector3[] positions = {
            new Vector3(-11f,0f,3f),new Vector3(-9f,0f,7f),new Vector3(-13f,0f,-3f),
            new Vector3(-16f,0f,12f),new Vector3(-7f,0f,11f),new Vector3(-2.5f,0f,11f),
            new Vector3(9f,0f,4f),new Vector3(12f,0f,-2f),new Vector3(15f,0f,8f),
            new Vector3(8.5f,0f,11f),new Vector3(4f,0f,13f),new Vector3(17f,0f,14f),
            new Vector3(-20f,0f,5f),new Vector3(20f,0f,1f),new Vector3(-19f,0f,15f)
        };
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject go = PrefabUtility.InstantiatePrefab(treePrefab) as GameObject;
            go.name = "Tree " + (i + 1).ToString("00");
            go.transform.SetParent(parent);
            go.transform.position = positions[i];
            float s = 0.80f + (i % 5) * 0.17f;
            go.transform.localScale = Vector3.one * s;
        }
    }

    private static void CreateGrass(Transform parent, Material blade, Material light)
    {
        var random = new System.Random(4217);
        GameObject tufts = new GameObject("Grass clusters");
        tufts.transform.SetParent(parent, false);
        for (int i = 0; i < 95; i++)
        {
            float x = (float)(random.NextDouble() * 31.0 - 15.5);
            float z = (float)(random.NextDouble() * 17.5 - 7.5);
            if ((x > -6f && x < 6f) && (z > -2.0f && z < 3.6f)) continue;
            GameObject cluster = new GameObject("Tuft " + i.ToString("00"));
            cluster.transform.SetParent(tufts.transform, false);
            cluster.transform.localPosition = new Vector3(x, 0.01f, z);
            for (int k = 0; k < 3; k++)
            {
                GameObject bladeGo = CreatePrimitive("grass blade", PrimitiveType.Capsule, cluster.transform,
                    new Vector3((k - 1) * 0.12f, 0.23f, 0f),
                    new Vector3(0.09f, 0.27f + k * 0.04f, 0.08f), k == 1 ? light : blade);
                bladeGo.transform.localRotation = Quaternion.Euler(0f, 0f, (k - 1) * -22f);
            }
        }
    }

    private static void CreateRocks(Transform parent, Material rock)
    {
        Vector3[] locations = {
            new Vector3(-9,0.10f,-2),new Vector3(-11,0.06f,1),new Vector3(8,0.07f,-4),
            new Vector3(10,0.08f,8),new Vector3(3,0.09f,9),new Vector3(-2,0.08f,9)
        };
        for (int i = 0; i < locations.Length; i++)
        {
            CreatePrimitive("Smooth rock " + i, PrimitiveType.Sphere, parent,
                locations[i], new Vector3(0.65f, 0.26f, 0.43f), rock);
        }
    }

    private static void CreateClouds(Transform parent, Material cloud)
    {
        for (int i = 0; i < 4; i++)
        {
            var puff = new GameObject("Cloud " + (i + 1));
            puff.transform.SetParent(parent, false);
            float x = -18f + i * 12f;
            float y = 12f + (i % 2) * 2f;
            puff.transform.localPosition = new Vector3(x, y, 31f);
            CreatePrimitive("center", PrimitiveType.Sphere, puff.transform,
                Vector3.zero, new Vector3(4.5f, 1.1f, 1.5f), cloud);
            CreatePrimitive("left", PrimitiveType.Sphere, puff.transform,
                new Vector3(-1.3f, 0.43f, 0f), new Vector3(2.9f, 1.45f, 1.55f), cloud);
            CreatePrimitive("right", PrimitiveType.Sphere, puff.transform,
                new Vector3(1.25f, 0.35f, 0f), new Vector3(2.7f, 1.36f, 1.55f), cloud);
        }
    }

    private static void CreateMountainMesh(string name, Transform parent, Material material,
                                           float centerZ, float amp1, float amp2)
    {
        const int xCount = 82;
        const int zCount = 30;
        Vector3[] vertices = new Vector3[(xCount + 1) * (zCount + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        for (int z = 0; z <= zCount; z++)
        {
            for (int x = 0; x <= xCount; x++)
            {
                float px = Mathf.Lerp(-27f, 27f, x / (float)xCount);
                float pz = Mathf.Lerp(centerZ - 5f, centerZ + 9f, z / (float)zCount);
                float wave = Mathf.Pow(Mathf.Sin(px * 0.26f + 1.3f) * 0.5f + 0.5f, 1.3f);
                float wave2 = Mathf.Pow(Mathf.Sin(px * 0.52f - 0.7f) * 0.5f + 0.5f, 1.5f);
                float noise = Mathf.PerlinNoise(px * 0.11f + 10f, pz * 0.09f + 12f);
                float height = 0.55f + wave * amp1 + wave2 * amp2 * 0.44f + noise * 1.35f;
                // 越遠較高；自然的前低後高山地地形。
                float depth = Mathf.InverseLerp(centerZ - 5f, centerZ + 9f, pz);
                float y = height * (0.46f + 0.54f * depth) - 0.08f;
                int idx = z * (xCount + 1) + x;
                vertices[idx] = new Vector3(px, y, pz);
                uv[idx] = new Vector2(x / (float)xCount, z / (float)zCount);
            }
        }
        int[] triangles = new int[xCount * zCount * 6];
        int count = 0;
        for (int z = 0; z < zCount; z++)
        {
            for (int x = 0; x < xCount; x++)
            {
                int a = z * (xCount + 1) + x;
                int b = a + 1;
                int c = a + xCount + 1;
                int d = c + 1;
                triangles[count++] = a; triangles[count++] = c; triangles[count++] = b;
                triangles[count++] = b; triangles[count++] = c; triangles[count++] = d;
            }
        }
        Mesh mesh = new Mesh { name = name };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        string assetPath = "Assets/Generated/" + name.Replace(" ", "") + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (existing != null) AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(mesh, assetPath);
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
