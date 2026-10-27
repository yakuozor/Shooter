using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShooterRange
{
    public sealed class ArenaGame : MonoBehaviour
    {
        public ArenaMap map;
        public RangePlayer player;
        public RangeWeapon weapon;
        public Material bodyMaterial, headMaterial, darkMaterial, tracerMaterial, impactMaterial;
        [Min(1)] public int killLimit = 10;
        public float respawnDelay = 3f;
        public bool VerificationMode { get; set; }
        public List<ArenaCombatant> Combatants { get; } = new List<ArenaCombatant>();
        public ArenaCombatant Human { get; private set; }
        public ArenaCombatant Winner { get; private set; }
        public bool HasStarted { get; private set; }
        public bool IsFinished => Winner != null;
        public float MatchTime { get; private set; }
        public bool IsSimulationRunning => HasStarted && !IsFinished && (VerificationMode || player.IsControlling);
        public readonly List<KillMessage> Feed = new List<KillMessage>();
        public struct KillMessage { public string text; public float expires; public bool headshot; }
        Transform botRoot;
        AudioClip botShot;
        readonly List<Material> ownedMaterials = new List<Material>();
        readonly Color[] botColors = { new Color(.9f,.29f,.14f), new Color(.6f,.4f,.85f), new Color(.9f,.65f,.13f), new Color(.23f,.64f,.83f), new Color(.48f,.7f,.3f) };

        void Start()
        {
            Human = player.GetComponent<ArenaCombatant>();
            Human.game = this;
            Human.isHuman = true;
            Human.displayName = "SEN";
            Human.CreateHitboxes(bodyMaterial, headMaterial, darkMaterial);
            weapon.combatant = Human;
            botShot = RangeWeapon.MakeSound("Bot shot", .09f, 130f, .65f);
            NewMatch();
        }

        public void BeginMatch() { if (!IsFinished) HasStarted = true; }

        void Update()
        {
            if (!HasStarted && player.IsControlling) BeginMatch();
            if (IsFinished && Input.GetKeyDown(KeyCode.Return)) { NewMatch(); return; }
            if (!IsSimulationRunning) return;
            MatchTime += Time.deltaTime;
            foreach (var fighter in Combatants)
            {
                if (!fighter.IsAlive && MatchTime >= fighter.RespawnAt)
                {
                    Vector3 spawn = SelectSpawn(fighter);
                    fighter.Respawn(spawn, FacingCenter(spawn));
                }
            }
            Feed.RemoveAll(message => message.expires < MatchTime);
        }

        public void NewMatch()
        {
            if (Human == null) return;
            player.ReleaseCursor();
            weapon.CancelReload();
            HasStarted = false;
            Winner = null;
            MatchTime = 0;
            Feed.Clear();
            if (botRoot != null) { botRoot.gameObject.SetActive(false); Destroy(botRoot.gameObject); }
            Combatants.Clear();
            foreach (Material material in ownedMaterials) Destroy(material);
            ownedMaterials.Clear();
            int seed = Guid.NewGuid().GetHashCode() & int.MaxValue;
            if (seed == map.Seed) seed ^= 12345;
            map.Generate(seed);
            Combatants.Add(Human);
            Human.ResetScore();
            Vector3 humanSpawn = map.SpawnPoints[UnityEngine.Random.Range(0, map.SpawnPoints.Count)];
            Human.Respawn(humanSpawn, FacingCenter(humanSpawn));
            botRoot = new GameObject("Five deathmatch bots").transform;
            for (int i = 0; i < 5; i++)
            {
                var obj = new GameObject("Bot " + (i + 1));
                obj.layer = 2;
                obj.transform.SetParent(botRoot, false);
                CharacterController motor = obj.AddComponent<CharacterController>();
                motor.height = 1.8f; motor.center = Vector3.up * .9f; motor.radius = .32f; motor.stepOffset = .15f;
                ArenaCombatant bot = obj.AddComponent<ArenaCombatant>();
                bot.game = this;
                bot.displayName = "BOT " + (i + 1).ToString("00");
                Material color = new Material(bodyMaterial) { color = botColors[i] };
                ownedMaterials.Add(color);
                bot.CreateHitboxes(color, headMaterial, darkMaterial);
                obj.AddComponent<ArenaBot>();
                Combatants.Add(bot);
                Vector3 spawn = SelectSpawn(bot);
                bot.Respawn(spawn, FacingCenter(spawn));
            }
            Physics.SyncTransforms();
        }

        public void RecordKill(ArenaCombatant attacker, ArenaCombatant victim, bool headshot)
        {
            if (IsFinished || attacker == victim) return;
            attacker.AddKill();
            Feed.Insert(0, new KillMessage { text = attacker.displayName + "  >  " + victim.displayName + (headshot ? "  [HS]" : ""), expires = MatchTime + 5f, headshot = headshot });
            if (Feed.Count > 4) Feed.RemoveAt(Feed.Count - 1);
            if (victim.isHuman) weapon.CancelReload();
            if (attacker.Kills < killLimit) return;
            Winner = attacker;
            weapon.CancelReload();
            player.ReleaseCursor();
        }

        public Vector3 SelectSpawn(ArenaCombatant spawning)
        {
            float bestScore = float.NegativeInfinity;
            Vector3 best = map.SpawnPoints[0];
            foreach (Vector3 spawn in map.SpawnPoints)
            {
                float nearest = 1000f;
                float exposure = 0;
                foreach (var other in Combatants)
                {
                    if (other == spawning || !other.IsAlive) continue;
                    float distance = Vector3.Distance(spawn, other.transform.position);
                    nearest = Mathf.Min(nearest, distance);
                    if (!Physics.Linecast(spawn + Vector3.up * 1.5f, other.transform.position + Vector3.up * 1.5f, 1, QueryTriggerInteraction.Ignore)) exposure += 2f;
                }
                float score = nearest - exposure + UnityEngine.Random.value * .75f;
                if (score > bestScore) { bestScore = score; best = spawn; }
            }
            return best;
        }

        static Quaternion FacingCenter(Vector3 point)
        {
            Vector3 delta = -point; delta.y = 0;
            return delta.sqrMagnitude > .01f ? Quaternion.LookRotation(delta) : Quaternion.identity;
        }

        // Ignore the shooter's own triggers, while cover and the first other fighter block the ray.
        public static bool Cast(Vector3 origin, Vector3 direction, float distance, ArenaCombatant shooter, out RaycastHit nearest)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, ~((1 << 2) | (1 << 5)), QueryTriggerInteraction.Collide);
            nearest = default;
            float best = float.MaxValue;
            foreach (var hit in hits)
            {
                ArenaHitbox box = hit.collider.GetComponent<ArenaHitbox>();
                if (box != null && (box.owner == shooter || !box.owner.IsAlive)) continue;
                if (hit.distance < best) { best = hit.distance; nearest = hit; }
            }
            return best < float.MaxValue;
        }

        public void DrawTracer(Vector3 start, Vector3 end, float duration)
        {
            GameObject obj = new GameObject("Bot tracer");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.sharedMaterial = tracerMaterial; line.positionCount = 2;
            line.SetPosition(0, start); line.SetPosition(1, end);
            line.startWidth = .022f; line.endWidth = .008f;
            Destroy(obj, duration);
        }

        public void PlayBotShot(Vector3 position)
        {
            var obj = new GameObject("Bot shot sound");
            obj.transform.position = position;
            AudioSource audio = obj.AddComponent<AudioSource>();
            audio.clip = botShot; audio.spatialBlend = 1; audio.minDistance = 3; audio.maxDistance = 35; audio.volume = .3f;
            audio.Play(); Destroy(obj, .2f);
        }

        public void SpawnHitBurst(Vector3 point, bool headshot)
        {
            for (int i = 0; i < (headshot ? 12 : 3); i++)
            {
                var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.name = headshot ? "Headshot burst" : "Hit spark";
                spark.layer = 2;
                spark.GetComponent<Collider>().enabled = false;
                Destroy(spark.GetComponent<Collider>());
                spark.transform.position = point;
                spark.transform.localScale = Vector3.one * (headshot ? .075f : .035f);
                spark.GetComponent<Renderer>().sharedMaterial = impactMaterial;
                ArenaSpark effect = spark.AddComponent<ArenaSpark>();
                effect.velocity = UnityEngine.Random.onUnitSphere * (headshot ? 4f : 2f) + Vector3.up;
                Destroy(spark, headshot ? .55f : .25f);
            }
        }

        void OnDestroy()
        {
            foreach (Material material in ownedMaterials) Destroy(material);
            Destroy(botShot);
        }
    }
}
