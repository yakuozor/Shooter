using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShooterRange.Editor
{
    [InitializeOnLoad]
    public static class ArenaVerification
    {
        const string Request = "Library/ArenaVerify.request";
        const string Report = "Library/ArenaVerification.txt";
        static IEnumerator steps;
        static float resumeAt;
        static StringBuilder report;
        static ArenaGame game;

        static ArenaVerification()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += Changed;
        }
        [MenuItem("Shooter/Deathmatch mekaniklerini doğrula")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || steps != null) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ArenaSceneBuilder.ScenePath);
            report = new StringBuilder("Deathmatch arena verification\n");
            try
            {
                ArenaMap map = UnityEngine.Object.FindObjectOfType<ArenaMap>();
                int seed = map.Seed;
                var signatures = new HashSet<string>();
                for (int i = 1; i <= 32; i++)
                {
                    map.Generate(i * 9973);
                    Assert(map.AllWalkableConnected(), "Seed " + i + ": all walkable cells connected");
                    Assert(map.SpawnPoints.Count == 8 && map.CoverCount >= 10 && map.CoverCount <= 26, "Seed " + i + ": bounded cover and 8 clear spawn pads");
                    for (int c = 0; c < ArenaMap.GridSize; c++)
                        if (map.Blocked[7,c] || map.Blocked[c,7] || map.Blocked[0,c] || map.Blocked[14,c] || map.Blocked[c,0] || map.Blocked[c,14]) throw new Exception("Reserved navigation corridor blocked");
                    foreach (Vector3 spawn in map.SpawnPoints)
                    {
                        Assert(map.IsWalkable(map.WorldCell(spawn)), "Spawn is walkable");
                        if (spawn != map.SpawnPoints[0]) Assert(map.FindPath(map.SpawnPoints[0], spawn).Count > 0, "Spawn reachable from first spawn");
                    }
                    signatures.Add(Signature(map));
                }
                Assert(signatures.Count == 32, "32 seeds produce 32 distinct layouts");
                map.Generate(12345); string first = Signature(map);
                map.Generate(12345); Assert(first == Signature(map), "Same seed reproduces the same layout");
                map.Generate(seed == 0 ? 16010 : seed);
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                File.WriteAllText(Report, report.ToString());
                SessionState.SetBool("Arena.Verifying", true);
                EditorApplication.isPlaying = true;
            }
            catch (Exception ex) { File.WriteAllText(Report, report + "RESULT: FAIL\n" + ex); Debug.LogException(ex); }
        }

        static string Signature(ArenaMap map)
        {
            var text = new StringBuilder();
            foreach (bool b in map.Blocked) text.Append(b ? '1' : '0');
            return text.ToString();
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Arena.Verifying", false))
            {
                report = new StringBuilder(File.ReadAllText(Report));
                steps = Mechanics(); resumeAt = Time.time + .4f;
            }
            if (state == PlayModeStateChange.ExitingPlayMode) { steps = null; SessionState.SetBool("Arena.Verifying", false); }
        }
        static void Update()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(Request))
            { File.Delete(Request); Run(); return; }
            if (steps == null || !EditorApplication.isPlaying || Time.time < resumeAt) return;
            try
            {
                if (steps.MoveNext()) resumeAt = Time.time + (steps.Current is float wait ? wait : .02f);
                else Finish(null);
            }
            catch (Exception ex) { Finish(ex); }
        }
        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            report.AppendLine("PASS: " + message);
        }
        static void StopBots()
        {
            foreach (var actor in game.Combatants) if (!actor.isHuman) actor.GetComponent<ArenaBot>().enabled = false;
        }
        static void Duel(ArenaCombatant victim)
        {
            game.Human.Respawn(new Vector3(0,.04f,-4), Quaternion.identity);
            victim.Respawn(new Vector3(0,.04f,4), Quaternion.Euler(0,180,0));
            int n = 0;
            foreach (var actor in game.Combatants)
                if (actor != game.Human && actor != victim) actor.Respawn(new Vector3(12,.04f,-12 + n++ * 6), Quaternion.identity);
            Physics.SyncTransforms();
        }
        static IEnumerator Mechanics()
        {
            game = UnityEngine.Object.FindObjectOfType<ArenaGame>();
            Assert(game != null && game.Combatants.Count == 6, "Exactly six combatants exist");
            Assert(game.Combatants.FindAll(a => !a.isHuman).Count == 5, "Exactly five bots exist");
            Assert(game.killLimit == 10 && !game.HasStarted, "Match waits for player and uses 10-kill limit");
            Assert(!game.weapon.TryFire(), "Cannot shoot before match begins");
            game.VerificationMode = true;
            StopBots(); game.BeginMatch();
            var human = game.Human;
            var bot = game.Combatants[1];
            Duel(bot);
            Assert(!bot.TakeDamage(human,100,true), "Spawn protection rejects damage");
            yield return 1.4f;
            game.weapon.viewCamera.transform.LookAt(bot.transform.position + Vector3.up * .95f);
            yield return .1f;
            Assert(game.weapon.TryFire() && bot.Health == 66, "Real body raycast deals 34 damage");
            Assert(!game.weapon.TryFire(), "Fire cooldown is enforced");
            yield return game.weapon.fireInterval + .04f;
            Assert(game.weapon.TryFire() && bot.Health == 32, "Second body shot leaves 32 health");
            yield return game.weapon.fireInterval + .04f;
            Assert(game.weapon.TryFire() && !bot.IsAlive, "Three body shots kill a bot");
            Assert(human.Kills == 1 && bot.Deaths == 1, "Kill and death scores increase once");
            Assert(!bot.TakeDamage(human,100,true) && human.Kills == 1, "Dead fighter cannot award duplicate kill");
            Assert(!bot.GetComponent<CharacterController>().enabled, "Dead fighter stops colliding and moving");
            yield return game.respawnDelay + .2f;
            Assert(bot.IsAlive && bot.Health == 100 && bot.IsProtected, "Bot respawns with full health and protection");
            Duel(bot); yield return 1.4f;
            game.weapon.viewCamera.transform.LookAt(bot.transform.position + Vector3.up * 1.58f);
            yield return .1f;
            Assert(game.weapon.TryFire() && !bot.IsAlive, "Real head raycast kills in one shot");
            Assert(game.weapon.LastHitWasHeadshot && Time.time - game.weapon.LastHeadshotTime < .1f, "Headshot feedback event is published");
            Assert(human.Kills == 2 && bot.Deaths == 2 && game.Feed[0].headshot, "Headshot scores once and is marked in kill feed");
            ScreenCapture.CaptureScreenshot("Library/ArenaHeadshot.png");
            yield return .25f;
            Duel(bot); yield return 1.4f;
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Verification cover"; wall.transform.position = new Vector3(0,1.5f,0); wall.transform.localScale = new Vector3(2,3,.5f);
            Physics.SyncTransforms();
            game.weapon.viewCamera.transform.LookAt(bot.transform.position + Vector3.up * 1.58f);
            yield return .1f;
            Assert(game.weapon.TryFire() && bot.Health == 100, "Cover blocks player damage");
            Assert(!bot.GetComponent<ArenaBot>().CanSee(human), "Bot line of sight is blocked by cover");
            wall.GetComponent<Collider>().enabled = false; UnityEngine.Object.Destroy(wall);
            Physics.SyncTransforms();
            Assert(bot.GetComponent<ArenaBot>().CanSee(human), "Bot sees the player when cover is removed");
            var brain = bot.GetComponent<ArenaBot>();
            brain.ResetBrain(); brain.enabled = true;
            int shotsBefore = brain.ShotsFired;
            yield return 8f;
            brain.enabled = false;
            Assert(brain.ShotsFired > shotsBefore + 2, "Live bot AI acquires an enemy and fires repeatedly");
            Assert(brain.DamageHits > 0, "Bot raycast shots damage an opponent");
            Duel(bot); yield return 1.4f;
            int botKills = bot.Kills, humanDeaths = human.Deaths;
            game.weapon.viewCamera.transform.rotation = Quaternion.Euler(0,90,0);
            Assert(game.weapon.TryFire() && game.weapon.BeginReload(), "Reload-before-death setup");
            Assert(human.TakeDamage(bot,100,false), "Bot can eliminate the human");
            Assert(!game.weapon.IsReloading && !game.weapon.TryFire(), "Death cancels reload and prevents firing");
            Assert(bot.Kills == botKills + 1 && human.Deaths == humanDeaths + 1, "Human death updates both scoreboard rows");
            yield return game.respawnDelay + .2f;
            Assert(human.IsAlive && human.Health == 100 && human.IsProtected && game.weapon.Ammo == 12 && game.weapon.Reserve == 120, "Human respawns with health, ammo and protection");
            Duel(bot); yield return 1.4f;
            var other = game.Combatants[2];
            Assert(other.TakeDamage(bot,100,false) && bot.Kills == botKills + 2, "Bots also score kills against each other");
            game.weapon.Refill();
            game.weapon.viewCamera.transform.rotation = Quaternion.Euler(0,90,0);
            Assert(game.weapon.TryFire() && game.weapon.BeginReload(), "Paused-reload setup");
            yield return .2f;
            float progress = game.weapon.ReloadProgress;
            game.VerificationMode = false;
            Assert(!game.IsSimulationRunning && !bot.TakeDamage(human,100,false), "Released cursor pauses combat and damage");
            yield return .4f;
            Assert(Mathf.Abs(game.weapon.ReloadProgress - progress) < .02f && game.weapon.Ammo == 11, "Pause also freezes reload progress");
            game.VerificationMode = true;
            yield return game.weapon.reloadDuration + .1f;
            Assert(!game.weapon.IsReloading && game.weapon.Ammo == 12 && game.weapon.Reserve == 119, "Reload resumes and transfers exactly one round");
            while (human.Kills < 9) human.AddKill();
            Assert(!game.IsFinished && human.Kills == 9, "Nine kills do not end the match");
            Assert(bot.TakeDamage(human,100,true), "Tenth kill applies damage");
            Assert(game.IsFinished && game.Winner == human && human.Kills == 10, "Human wins exactly at 10 kills");
            Assert(!game.weapon.TryFire() && !human.TakeDamage(other,100,false), "Finished match prevents all further damage and firing");
            yield return .2f;
            ScreenCapture.CaptureScreenshot("Library/ArenaScoreboard.png");
            yield return .25f;
            int previousSeed = game.map.Seed;
            game.NewMatch(); StopBots();
            Assert(!game.HasStarted && !game.IsFinished && game.Combatants.Count == 6, "New match returns to six ready combatants");
            Assert(game.map.Seed != previousSeed && game.map.AllWalkableConnected(), "New match generates a different valid arena");
            foreach (var fighter in game.Combatants) Assert(fighter.Kills == 0 && fighter.Deaths == 0, "New match resets every scoreboard row");
            game.BeginMatch(); bot = game.Combatants[1];
            while (bot.Kills < 9) bot.AddKill();
            yield return 1.4f;
            Assert(game.Human.TakeDamage(bot,100,false) && game.Winner == bot && bot.Kills == 10, "Bot can win the match at 10 kills");
            Assert(UnityEngine.Object.FindObjectsOfType<AudioListener>().Length == 1, "Exactly one audio listener");
            Assert(UnityEngine.Object.FindObjectOfType<ArenaHUD>() != null, "Deathmatch HUD and scoreboard exist");
        }
        static void Finish(Exception error)
        {
            steps = null;
            report.AppendLine(error == null ? "RESULT: PASS" : "RESULT: FAIL\n" + error);
            File.WriteAllText(Report, report.ToString());
            if (error == null) Debug.Log("[Arena] Verification PASSED. " + Report); else Debug.LogError("[Arena] Verification FAILED: " + error);
            SessionState.SetBool("Arena.Verifying", false);
            EditorApplication.isPlaying = false;
        }
    }
}
