using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShooterRange.Editor
{
    // Uses the running scene and real raycasts/coroutines; no test framework dependency.
    [InitializeOnLoad]
    public static class RangeVerification
    {
        const string Request = "Library/ShooterVerify.request";
        const string Report = "Library/ShooterVerification.txt";
        static IEnumerator steps;
        static float resumeAt;
        static StringBuilder report;
        static RangePlayer player;
        static RangeWeapon weapon;
        static int originalReserve;

        static RangeVerification()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        [MenuItem("Shooter/Mekanikleri doğrula")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || steps != null) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(RangeSceneBuilder.ScenePath);
            SessionState.SetBool("Shooter.Verifying", true);
            EditorApplication.isPlaying = true;
        }

        static void PlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Shooter.Verifying", false))
            {
                report = new StringBuilder("Shooter mechanics verification\n");
                steps = CheckMechanics();
                resumeAt = Time.time + .3f;
            }
            if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool("Shooter.Verifying", false))
            {
                SessionState.SetBool("Shooter.Verifying", false);
                steps = null;
            }
        }

        static void Update()
        {
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request))
            {
                File.Delete(Request);
                Run();
                return;
            }
            if (steps == null || !EditorApplication.isPlaying || Time.time < resumeAt) return;
            try
            {
                if (steps.MoveNext()) resumeAt = Time.time + (steps.Current is float wait ? wait : .02f);
                else Finish(true, null);
            }
            catch (Exception ex) { Finish(false, ex.ToString()); }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            report.AppendLine("PASS: " + message);
        }

        static IEnumerator CheckMechanics()
        {
            weapon = UnityEngine.Object.FindObjectOfType<RangeWeapon>();
            player = UnityEngine.Object.FindObjectOfType<RangePlayer>();
            Assert(weapon != null && player != null, "Scene has player and weapon");
            player.enabled = false;
            originalReserve = weapon.startingReserve;
            RangeTarget target = Array.Find(UnityEngine.Object.FindObjectsOfType<RangeTarget>(), t => t.name.StartsWith("Target 01"));
            Assert(target != null, "Reactive target exists");
            Assert(weapon.Ammo == 12 && weapon.Reserve == 120, "Initial ammo is 12 / 120");
            Assert(!weapon.BeginReload(), "Full magazine cannot reload");
            // Move the test rig onto the selected lane, avoiding other targets.
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = new Vector3(target.transform.position.x, .06f, 1f);
            weapon.viewCamera.transform.LookAt(target.plate.position + Vector3.up * .65f);
            Physics.SyncTransforms();
            yield return .15f;
            Assert(weapon.TryFire(), "Loaded weapon fires");
            Assert(weapon.Ammo == 11 && weapon.Hits == 1 && target.Health == 2, "Raycast damages target and consumes one round (ammo=" + weapon.Ammo + ", hits=" + weapon.Hits + ", health=" + target.Health + ")");
            Assert(!weapon.TryFire() && weapon.Ammo == 11, "Fire interval prevents duplicate shots");
            Assert(weapon.BeginReload(), "Partial magazine can reload");
            Assert(!weapon.BeginReload(), "Reload cannot start twice");
            Assert(!weapon.TryFire(), "Cannot shoot while reloading");
            yield return weapon.reloadDuration + .12f;
            Assert(!weapon.IsReloading && weapon.Ammo == 12 && weapon.Reserve == 119, "Reload transfers only the missing round");
            Assert(weapon.TryFire(), "Fires after reload");
            yield return weapon.fireInterval + .03f;
            Assert(weapon.TryFire() && target.IsDown && weapon.Knockdowns == 1, "Three target hits knock it down");
            Assert(!target.plate.GetComponentInChildren<Collider>().enabled, "Downed target colliders are disabled");
            weapon.viewCamera.transform.rotation = Quaternion.Euler(0, 180, 0);
            while (weapon.Ammo > 0)
            {
                yield return weapon.fireInterval + .03f;
                Assert(weapon.TryFire(), "Remaining magazine round fires");
            }
            yield return weapon.fireInterval + .03f;
            Assert(!weapon.TryFire() && weapon.Ammo == 0, "Empty fire cannot produce negative ammo");
            Assert(weapon.BeginReload(), "Empty magazine reload starts");
            yield return weapon.reloadDuration + .12f;
            Assert(weapon.Ammo == 12 && weapon.Reserve == 107, "Empty reload transfers a full magazine");
            Assert(!target.IsDown && target.Health == 3, "Target recovers with full health");
            weapon.startingReserve = 2;
            weapon.Refill();
            for (int i = 0; i < 3; i++) { Assert(weapon.TryFire(), "Limited-reserve setup shot fires"); yield return weapon.fireInterval + .03f; }
            Assert(weapon.BeginReload(), "Limited reserve reload starts");
            yield return weapon.reloadDuration + .12f;
            Assert(weapon.Ammo == 11 && weapon.Reserve == 0, "Limited reserve transfers only available rounds");
            Assert(!weapon.BeginReload(), "Zero reserve cannot reload");
            while (weapon.Ammo > 0) { Assert(weapon.TryFire(), "Last available round fires"); yield return weapon.fireInterval + .03f; }
            Assert(!weapon.TryFire() && !weapon.BeginReload() && weapon.Ammo == 0 && weapon.Reserve == 0, "Fully exhausted ammunition stays at zero");
            weapon.startingReserve = originalReserve;
            weapon.Refill();
            yield return weapon.fireInterval + .03f;
            Assert(weapon.TryFire() && weapon.BeginReload(), "Refill-cancellation setup");
            weapon.Refill();
            yield return weapon.reloadDuration + .12f;
            Assert(weapon.Ammo == 12 && weapon.Reserve == 120 && !weapon.IsReloading, "Refill safely cancels a pending reload");
            Assert(UnityEngine.Object.FindObjectsOfType<AudioListener>().Length == 1, "Exactly one active audio listener");
            Assert(UnityEngine.Object.FindObjectOfType<RangeHUD>() != null, "Ammo HUD exists");
        }

        static void Finish(bool success, string error)
        {
            steps = null;
            report.AppendLine(success ? "RESULT: PASS" : "RESULT: FAIL\n" + error);
            File.WriteAllText(Report, report.ToString());
            if (success) Debug.Log("[Shooter] Mechanics verification PASSED. " + Report);
            else Debug.LogError("[Shooter] Mechanics verification FAILED: " + error);
            SessionState.SetBool("Shooter.Verifying", false);
            EditorApplication.isPlaying = false;
        }
    }
}
