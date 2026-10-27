using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ShooterRange.Editor
{
    [InitializeOnLoad]
    public static class ArenaSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/DeathmatchArena.unity";
        static ArenaSceneBuilder() { EditorApplication.delayCall += FirstSetup; }
        static void FirstSetup()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath) || SceneManager.GetActiveScene().isDirty) return;
            Build();
        }
        [MenuItem("Shooter/Deathmatch arenasını aç")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) Build(); else EditorSceneManager.OpenScene(ScenePath);
        }
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Shooter/Materials/" + name + ".mat");

        static void Build()
        {
            if (!File.Exists(RangeSceneBuilder.ScenePath)) return;
            // Reuse the existing authored weapon, without changing the training scene.
            Scene source = SceneManager.GetSceneByPath(RangeSceneBuilder.ScenePath);
            if (!source.IsValid() || !source.isLoaded) source = EditorSceneManager.OpenScene(RangeSceneBuilder.ScenePath, OpenSceneMode.Additive);
            RangePlayer original = null;
            foreach (GameObject root in source.GetRootGameObjects())
                if (root.GetComponent<RangePlayer>() != null) original = root.GetComponent<RangePlayer>();
            if (original == null) { EditorSceneManager.CloseScene(source, true); return; }
            GameObject playerObject = Object.Instantiate(original.gameObject);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.MoveGameObjectToScene(playerObject, scene);
            EditorSceneManager.CloseScene(source, true);
            SceneManager.SetActiveScene(scene);
            playerObject.name = "Player / SEN";
            playerObject.transform.position = new Vector3(0, .04f, -12f);
            playerObject.transform.rotation = Quaternion.identity;
            RangePlayer player = playerObject.GetComponent<RangePlayer>();
            player.moveSpeed = 5.2f;
            RangeWeapon weapon = playerObject.GetComponent<RangeWeapon>();
            ArenaCombatant human = playerObject.AddComponent<ArenaCombatant>();
            human.isHuman = true; human.displayName = "SEN";
            human.motor = playerObject.GetComponent<CharacterController>();
            weapon.combatant = human;
            var map = new GameObject("ARENA / Procedural layout").AddComponent<ArenaMap>();
            map.floorMaterial = Mat("Floor"); map.wallMaterial = Mat("Wall"); map.darkMaterial = Mat("Graphite");
            map.accentMaterial = Mat("Signal orange"); map.lightMaterial = Mat("Strip light");
            map.Generate(16010);
            var game = new GameObject("DEATHMATCH / First to 10").AddComponent<ArenaGame>();
            game.player = player; game.weapon = weapon; game.map = map;
            game.bodyMaterial = Mat("Teal"); game.headMaterial = Mat("Target face"); game.darkMaterial = Mat("Graphite");
            game.tracerMaterial = Mat("Tracer"); game.impactMaterial = Mat("Hit flash");
            human.game = game;
            var hud = new GameObject("HUD / Scoreboard and headshots").AddComponent<ArenaHUD>();
            hud.game = game;
            var sunlight = new GameObject("Arena sun").AddComponent<Light>();
            sunlight.type = LightType.Directional; sunlight.intensity = 1.05f; sunlight.color = new Color(1,.92f,.8f);
            sunlight.shadows = LightShadows.Soft; sunlight.transform.rotation = Quaternion.Euler(58,-25,0);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.64f,.72f,.8f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.16f,.23f,.29f); RenderSettings.fogStartDistance = 30; RenderSettings.fogEndDistance = 65;
            player.viewCamera.backgroundColor = new Color(.19f,.29f,.37f);
            RenderSettings.sun = sunlight;
            EditorSceneManager.SaveScene(scene, ScenePath);
            // Close the former working scene only after the new arena is safely saved.
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene other = SceneManager.GetSceneAt(i);
                if (other != scene && !other.isDirty) EditorSceneManager.CloseScene(other, true);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene(RangeSceneBuilder.ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = game.gameObject;
            Debug.Log("[Arena] 6 kişilik deathmatch hazır. Play, ardından oyun alanına tıklayın. TAB skor, R şarjör, ESC duraklatır.");
        }
    }
}
