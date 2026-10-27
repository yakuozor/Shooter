using UnityEngine;

namespace ShooterRange
{
    public sealed class ArenaCombatant : MonoBehaviour
    {
        public ArenaGame game;
        public string displayName;
        public bool isHuman;
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int Health { get; private set; } = 100;
        public bool IsAlive => Health > 0;
        public bool IsProtected => game != null && game.MatchTime < protectedUntil;
        public float RespawnAt { get; private set; }
        public float LastDamageTime { get; private set; } = -100f;
        public Transform visual;
        public Collider[] hitboxes;
        public CharacterController motor;
        float protectedUntil;
        float flinch;
        float deathKick;

        public void ResetScore() { Kills = 0; Deaths = 0; }
        public void AddKill() { Kills++; }

        public bool TakeDamage(ArenaCombatant attacker, int amount, bool headshot)
        {
            if (!IsAlive || IsProtected || game == null || !game.IsSimulationRunning || attacker == this || attacker == null || !attacker.IsAlive) return false;
            Health = Mathf.Max(0, Health - amount);
            LastDamageTime = Time.time;
            flinch = headshot ? 24f : 9f;
            if (Health > 0) return true;
            Deaths++;
            deathKick = headshot ? 115f : 82f;
            RespawnAt = game.MatchTime + game.respawnDelay;
            foreach (Collider box in hitboxes) box.enabled = false;
            if (motor != null) motor.enabled = false;
            game.RecordKill(attacker, this, headshot);
            return true;
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            if (motor != null) motor.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            Health = 100;
            flinch = 0;
            protectedUntil = game.MatchTime + 1.25f;
            if (visual != null) visual.localRotation = Quaternion.identity;
            foreach (Collider box in hitboxes) box.enabled = true;
            if (motor != null) motor.enabled = true;
            if (isHuman)
            {
                GetComponent<RangePlayer>().ResetView();
                GetComponent<RangeWeapon>().Refill();
            }
            GetComponent<ArenaBot>()?.ResetBrain();
        }

        void Update()
        {
            if (game == null || !game.IsSimulationRunning || visual == null) return;
            flinch = Mathf.MoveTowards(flinch, 0f, Time.deltaTime * 65f);
            Quaternion goal = IsAlive ? Quaternion.Euler(-flinch, 0, 0) : Quaternion.Euler(deathKick, 0, 22f);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, goal, Time.deltaTime * 15f);
        }

        public void CreateHitboxes(Material body, Material head, Material dark)
        {
            motor = GetComponent<CharacterController>();
            var root = new GameObject("Body / hit reaction").transform;
            root.SetParent(transform, false);
            visual = root;
            MakePart("Torso", new Vector3(0, .95f, 0), new Vector3(.6f, .8f, .36f), body, false, true);
            MakePart("Head / headshot", new Vector3(0, 1.58f, 0), new Vector3(.44f, .44f, .44f), head, true, true);
            if (isHuman) MakePart("Lower body", new Vector3(0, .275f, 0), new Vector3(.5f, .55f, .32f), dark, false, true);
            if (!isHuman)
            {
                MakePart("Leg L", new Vector3(-.17f, .3f, 0), new Vector3(.22f, .6f, .27f), dark, false, true);
                MakePart("Leg R", new Vector3(.17f, .3f, 0), new Vector3(.22f, .6f, .27f), dark, false, true);
                MakePart("Visor", new Vector3(0, 1.64f, .23f), new Vector3(.34f, .1f, .04f), dark, false, false);
                MakePart("Gun", new Vector3(.35f, 1.1f, .32f), new Vector3(.13f, .14f, .55f), dark, false, false);
                MakePart("Arm", new Vector3(.36f, .94f, .1f), new Vector3(.16f, .48f, .2f), body, false, false);
            }
            ArenaHitbox[] regions = root.GetComponentsInChildren<ArenaHitbox>();
            hitboxes = new Collider[regions.Length];
            for (int i = 0; i < regions.Length; i++) hitboxes[i] = regions[i].GetComponent<Collider>();

            void MakePart(string name, Vector3 p, Vector3 scale, Material mat, bool isHead, bool collision)
            {
                var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.name = name;
                part.transform.SetParent(root, false);
                part.transform.localPosition = p;
                part.transform.localScale = scale;
                part.GetComponent<Renderer>().sharedMaterial = mat;
                part.GetComponent<Renderer>().enabled = !isHuman;
                if (collision)
                {
                    part.layer = 8;
                    part.GetComponent<Collider>().isTrigger = true;
                    ArenaHitbox hitbox = part.AddComponent<ArenaHitbox>();
                    hitbox.owner = this;
                    hitbox.isHead = isHead;
                }
                else { part.GetComponent<Collider>().enabled = false; Destroy(part.GetComponent<Collider>()); }
            }
        }
    }
}
