using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ShooterRange.Editor
{
    [InitializeOnLoad]
    public static class RangeSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/ShooterRange.unity";
        const string MaterialFolder = "Assets/Shooter/Materials";
        static Material concrete, wall, dark, steel, orange, cream, teal, glow, whiteGlow, hit, tracer;

        static RangeSceneBuilder() { EditorApplication.delayCall += FirstSetup; }
        static void FirstSetup()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath)) return;
            // Never replace or close an unsaved user scene.
            if (SceneManager.GetActiveScene().isDirty) return;
            Build();
        }

        [MenuItem("Shooter/Atış poligonunu aç")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) Build();
            else EditorSceneManager.OpenScene(ScenePath);
        }

        // Explicit menu command is useful when the scene has been removed.
        [MenuItem("Shooter/Yeni poligon sahnesi oluştur")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(ScenePath)) { Open(); return; }
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
            concrete = Mat("Floor", new Color(.19f, .25f, .28f), .08f);
            wall = Mat("Wall", new Color(.27f, .35f, .38f), .05f);
            dark = Mat("Graphite", new Color(.055f, .085f, .105f), .25f);
            steel = Mat("Steel", new Color(.32f, .42f, .45f), .5f);
            orange = Mat("Signal orange", new Color(1f, .29f, .075f), .12f);
            cream = Mat("Target face", new Color(.88f, .88f, .77f), .05f);
            teal = Mat("Teal", new Color(.08f, .55f, .55f), .2f);
            glow = Mat("Amber light", new Color(1f, .45f, .08f), 0f, true);
            whiteGlow = Mat("Strip light", new Color(.53f, .94f, 1f), 0f, true);
            hit = Mat("Hit flash", new Color(1f, .78f, .21f), 0f, true);
            tracer = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, .76f, .3f) };
            AssetDatabase.CreateAsset(tracer, MaterialFolder + "/Tracer.mat");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.58f, .67f, .73f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.14f, .21f, .25f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 65f;
            var sunObject = new GameObject("Key light");
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .91f, .8f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = sun;

            Transform architecture = new GameObject("POLIGON / Architecture").transform;
            Cube("Floor", new Vector3(0, -.25f, 13), new Vector3(22, .5f, 38), concrete, architecture);
            Cube("Backstop", new Vector3(0, 3, 31), new Vector3(22, 6, .6f), dark, architecture);
            Cube("Left wall", new Vector3(-11, 2.5f, 13), new Vector3(.5f, 5, 38), wall, architecture);
            Cube("Right wall", new Vector3(11, 2.5f, 13), new Vector3(.5f, 5, 38), wall, architecture);
            Cube("Entry wall", new Vector3(0, 2.5f, -6), new Vector3(22, 5, .5f), wall, architecture);
            Cube("Roof", new Vector3(0, 6.7f, 13), new Vector3(22, .3f, 38), dark, architecture);
            // Low foreground rail keeps the player on the firing deck, with clear sight lines.
            Cube("Firing deck barrier", new Vector3(0, .4f, 6.1f), new Vector3(21.5f, .8f, .28f), dark, architecture);
            Cube("Safety edge", new Vector3(0, .82f, 6.1f), new Vector3(21.5f, .045f, .32f), orange, architecture);
            for (int z = -4; z <= 30; z += 4)
            {
                Cube("Floor seam", new Vector3(0, .004f, z), new Vector3(21.5f, .008f, .025f), dark, architecture, false);
                Cube("Left rib", new Vector3(-10.65f, 3, z), new Vector3(.25f, 6, .28f), dark, architecture);
                Cube("Right rib", new Vector3(10.65f, 3, z), new Vector3(.25f, 6, .28f), dark, architecture);
                Cube("Ceiling beam", new Vector3(0, 6.3f, z), new Vector3(21.5f, .3f, .24f), steel, architecture);
                Cube("Ceiling strip L", new Vector3(-5, 6.1f, z), new Vector3(4, .045f, .24f), whiteGlow, architecture, false);
                Cube("Ceiling strip R", new Vector3(5, 6.1f, z), new Vector3(4, .045f, .24f), whiteGlow, architecture, false);
            }
            for (int x = -8; x <= 8; x += 4)
            {
                Cube("Lane line", new Vector3(x, .012f, 17), new Vector3(.045f, .02f, 27), steel, architecture, false);
                Cube("Firing mark", new Vector3(x, .018f, 4.5f), new Vector3(1.4f, .025f, .09f), orange, architecture, false);
                Cube("Backstop panel", new Vector3(x, 2.1f, 30.62f), new Vector3(3.7f, 3.7f, .12f), wall, architecture);
                Cube("Backstop accent", new Vector3(x, 4.06f, 30.51f), new Vector3(3.7f, .065f, .045f), glow, architecture, false);
            }
            for (int i = 0; i < 3; i++)
            {
                float z = 10f + i * 8f;
                Sign("Distance L", (9 + i * 8) + " M", new Vector3(-10.33f, 2.65f, z), .11f, Quaternion.Euler(0, -90, 0), architecture, cream);
                Sign("Distance R", (9 + i * 8) + " M", new Vector3(10.33f, 2.65f, z), .11f, Quaternion.Euler(0, 90, 0), architecture, cream);
            }
            Sign("Range title", "P O L I G O N   /   0 1", new Vector3(0, 5.15f, 30.58f), .27f, Quaternion.identity, architecture, cream);
            Sign("Backstop subtitle", "TRAINING   •   CUBIC RANGE", new Vector3(0, 4.55f, 30.56f), .095f, Quaternion.identity, architecture, cream);
            for (int i = 0; i < 13; i++)
            {
                GameObject stripe = Cube("Safety stripe", new Vector3(-9f + i * 1.5f, .022f, 5.6f), new Vector3(.55f, .018f, .3f), orange, architecture, false);
                stripe.transform.rotation = Quaternion.Euler(0, -30, 0);
            }
            Transform props = new GameObject("POLIGON / Props").transform;
            Crate(new Vector3(-8, .7f, 2), props);
            Crate(new Vector3(-8, 2.05f, 2), props);
            Crate(new Vector3(8, .7f, -.5f), props);
            Cube("Supply bench", new Vector3(7.5f, 1, -3), new Vector3(3, .22f, 1.3f), steel, props);
            Cube("Bench foot L", new Vector3(6.4f, .5f, -3), new Vector3(.18f, 1, .9f), dark, props);
            Cube("Bench foot R", new Vector3(8.6f, .5f, -3), new Vector3(.18f, 1, .9f), dark, props);
            Cube("Ammo box", new Vector3(7.5f, 1.4f, -3), new Vector3(.85f, .6f, .65f), orange, props);

            Transform targets = new GameObject("POLIGON / Reactive targets").transform;
            Target("01", new Vector3(0f, 0f, 13f), targets);
            Target("02", new Vector3(-4.1f, 0f, 18f), targets);
            Target("03", new Vector3(4.1f, 0f, 18f), targets);
            Target("04", new Vector3(-7.7f, 0f, 25f), targets);
            Target("05", new Vector3(7.7f, 0f, 25f), targets);

            GameObject playerObject = new GameObject("Player");
            playerObject.layer = 2;
            playerObject.transform.position = new Vector3(0, .06f, 1f);
            CharacterController controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .3f;
            controller.center = new Vector3(0, .9f, 0);
            controller.stepOffset = .25f;
            RangePlayer player = playerObject.AddComponent<RangePlayer>();
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.65f, 0);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 65f;
            camera.nearClipPlane = .04f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;
            cameraObject.AddComponent<AudioListener>();
            player.viewCamera = camera;
            Transform model = new GameObject("P12 / Cubic weapon").transform;
            model.SetParent(cameraObject.transform, false);
            model.localPosition = new Vector3(.32f, -.29f, .52f);
            Cube("Receiver", new Vector3(0, 0, 0), new Vector3(.15f, .15f, .47f), dark, model, false);
            Cube("Slide", new Vector3(0, .105f, .015f), new Vector3(.155f, .085f, .51f), steel, model, false);
            Cube("Barrel", new Vector3(0, .025f, .3f), new Vector3(.09f, .09f, .19f), dark, model, false);
            Cube("Muzzle cap", new Vector3(0, .025f, .4f), new Vector3(.115f, .115f, .035f), steel, model, false);
            Cube("Muzzle opening", new Vector3(0, .025f, .42f), new Vector3(.057f, .057f, .009f), dark, model, false);
            var grip = Cube("Grip", new Vector3(0, -.16f, -.12f), new Vector3(.12f, .27f, .16f), dark, model, false);
            grip.transform.localRotation = Quaternion.Euler(-12, 0, 0);
            Cube("Magazine base", new Vector3(0, -.295f, -.09f), new Vector3(.155f, .035f, .18f), orange, model, false);
            Cube("Side panel", new Vector3(.079f, .005f, .02f), new Vector3(.014f, .065f, .23f), orange, model, false);
            Cube("Rear sight L", new Vector3(-.054f, .17f, -.18f), new Vector3(.025f, .05f, .04f), dark, model, false);
            Cube("Rear sight R", new Vector3(.054f, .17f, -.18f), new Vector3(.025f, .05f, .04f), dark, model, false);
            Cube("Front sight", new Vector3(0, .16f, .23f), new Vector3(.025f, .04f, .035f), teal, model, false);
            Cube("Hand", new Vector3(.02f, -.14f, -.21f), new Vector3(.18f, .15f, .2f), wall, model, false);
            Cube("Sleeve", new Vector3(.04f, -.23f, -.37f), new Vector3(.22f, .19f, .27f), teal, model, false);
            for (int i = 0; i < 5; i++) Cube("Slide serration", new Vector3(.08f, .11f, -.17f + i * .032f), new Vector3(.005f, .07f, .012f), dark, model, false);
            Transform muzzle = new GameObject("Muzzle / shot origin").transform;
            muzzle.SetParent(model, false);
            muzzle.localPosition = new Vector3(0, .025f, .46f);
            GameObject flash = Cube("Muzzle flash", Vector3.zero, new Vector3(.095f, .095f, .2f), hit, muzzle, false);
            flash.transform.localRotation = Quaternion.Euler(0, 0, 45);
            Light flashLight = muzzle.gameObject.AddComponent<Light>();
            flashLight.color = new Color(1f, .64f, .25f);
            flashLight.range = 3f;
            flashLight.intensity = 2f;
            flashLight.enabled = false;
            flash.SetActive(false);
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
            RangeWeapon weapon = playerObject.AddComponent<RangeWeapon>();
            weapon.player = player;
            weapon.viewCamera = camera;
            weapon.weaponModel = model;
            weapon.muzzle = muzzle;
            weapon.muzzleFlash = flash;
            weapon.flashLight = flashLight;
            weapon.tracerMaterial = tracer;
            weapon.impactMaterial = hit;
            RangeHUD hud = new GameObject("HUD / Ammo and crosshair").AddComponent<RangeHUD>();
            hud.weapon = weapon;
            hud.player = player;
            ImproveAppearance();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = playerObject;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 1.5f, 13), Quaternion.Euler(18, 0, 0), 20f);
            Debug.Log("[Shooter] Poligon hazır. Play'e basın ve oyun alanına tıklayın. Sol tık: ateş, R: şarjör, WASD: hareket, T: ikmal, Esc: imleç.");
        }

        [MenuItem("Shooter/Poligon görünümünü düzenle")]
        public static void ImproveAppearance()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != ScenePath && SceneManager.GetActiveScene().name != "Untitled") return;
            Transform architecture = GameObject.Find("POLIGON / Architecture")?.transform;
            if (architecture == null) return;
            // Let the key light reach the room; the visible ceiling stays in place.
            architecture.Find("Roof").GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            if (GameObject.Find("POLIGON / Fill lights") == null)
            {
                Transform root = new GameObject("POLIGON / Fill lights").transform;
                for (int i = 0; i < 4; i++)
                {
                    GameObject obj = new GameObject("Ceiling fill " + (i + 1));
                    obj.transform.SetParent(root, false);
                    obj.transform.position = new Vector3(0, 4.8f, 1 + i * 8);
                    Light light = obj.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(.7f, .86f, 1f);
                    light.range = 14f;
                    light.intensity = 1.5f;
                    light.shadows = LightShadows.None;
                    light.renderMode = LightRenderMode.ForcePixel;
                }
            }
            Transform title = architecture.Find("Range title");
            title.localPosition = new Vector3(0, 5.35f, 30.58f);
            title.GetComponent<TextMesh>().characterSize = .22f;
            Transform subtitle = architecture.Find("Backstop subtitle");
            subtitle.localPosition = new Vector3(0, 4.45f, 30.56f);
            subtitle.GetComponent<TextMesh>().characterSize = .07f;
            int leftIndex = 0, rightIndex = 0;
            foreach (Transform child in architecture)
            {
                if (child.name == "Distance L" || child.name == "Distance R")
                {
                    bool left = child.name == "Distance L";
                    int index = left ? leftIndex++ : rightIndex++;
                    child.localRotation = Quaternion.Euler(0, left ? -90f : 90f, 0);
                    TextMesh mesh = child.GetComponent<TextMesh>();
                    mesh.characterSize = .11f;
                    mesh.text = (9 + index * 8) + " M";
                }
            }
            foreach (Renderer renderer in GameObject.Find("P12 / Cubic weapon").GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (SceneManager.GetActiveScene().path == ScenePath) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        static Material Mat(string name, Color color, float metallic, bool emission = false)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", .25f);
            if (emission) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.4f); }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool collider = true)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }

        static void Crate(Vector3 position, Transform parent)
        {
            Transform root = new GameObject("Equipment crate").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            Cube("Case", Vector3.zero, new Vector3(1.5f, 1.3f, 1.4f), dark, root);
            Cube("Band A", new Vector3(-.5f, 0, 0), new Vector3(.09f, 1.34f, 1.44f), steel, root, false);
            Cube("Band B", new Vector3(.5f, 0, 0), new Vector3(.09f, 1.34f, 1.44f), steel, root, false);
            Cube("Latch", new Vector3(0, .15f, -.71f), new Vector3(.25f, .18f, .03f), orange, root, false);
        }

        static void Target(string number, Vector3 position, Transform parent)
        {
            Transform root = new GameObject("Target " + number + " / 3 hits").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            Cube("Base", new Vector3(0, .12f, 0), new Vector3(1.35f, .24f, .9f), dark, root);
            Cube("Post", new Vector3(0, .73f, .07f), new Vector3(.12f, 1.3f, .14f), steel, root);
            Transform plate = new GameObject("Hinged target plate").transform;
            plate.SetParent(root, false);
            plate.localPosition = new Vector3(0, 1.05f, 0);
            Cube("Armor", new Vector3(0, .65f, 0), new Vector3(1.24f, 1.65f, .18f), dark, plate);
            Renderer face = Cube("Face", new Vector3(0, .65f, -.11f), new Vector3(1.1f, 1.5f, .055f), cream, plate).GetComponent<Renderer>();
            Cube("Outer square", new Vector3(0, .71f, -.145f), new Vector3(.76f, .85f, .02f), orange, plate);
            Cube("Inner square", new Vector3(0, .71f, -.16f), new Vector3(.49f, .57f, .02f), dark, plate);
            Cube("Bullseye", new Vector3(0, .71f, -.175f), new Vector3(.2f, .23f, .02f), orange, plate);
            Cube("Head", new Vector3(0, 1.67f, 0), new Vector3(.58f, .4f, .18f), orange, plate);
            Sign("Target number", number, new Vector3(0, .14f, -.15f), .065f, Quaternion.identity, plate, dark);
            RangeTarget target = root.gameObject.AddComponent<RangeTarget>();
            target.plate = plate;
            target.face = face;
            target.normalMaterial = cream;
            target.hitMaterial = hit;
        }

        static void Sign(string name, string text, Vector3 position, float size, Quaternion rotation, Transform parent, Material material)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
            TextMesh mesh = obj.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 80;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = material.color;
            obj.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
        }
    }
}
