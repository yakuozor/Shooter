using System.Collections;
using UnityEngine;

namespace ShooterRange
{
    public sealed class RangeWeapon : MonoBehaviour
    {
        [Header("References")]
        public RangePlayer player;
        public Camera viewCamera;
        public Transform weaponModel;
        public Transform muzzle;
        public GameObject muzzleFlash;
        public Light flashLight;
        public Material tracerMaterial;
        public Material impactMaterial;
        [Header("Weapon tuning")]
        [Min(1)] public int magazineSize = 12;
        [Min(0)] public int startingReserve = 120;
        [Min(.05f)] public float fireInterval = .16f;
        [Min(.1f)] public float reloadDuration = 1.35f;
        public float range = 80f;
        public ArenaCombatant combatant;
        public int bodyDamage = 34;
        public int headDamage = 100;
        public float LastHeadshotTime { get; private set; } = -10f;
        public bool LastHitWasHeadshot { get; private set; }
        public int Ammo { get; private set; }
        public int Reserve { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadProgress => IsReloading ? Mathf.Clamp01(reloadElapsed / reloadDuration) : 0f;
        public int Hits { get; private set; }
        public int Knockdowns { get; private set; }
        public float LastHitTime { get; private set; } = -10f;
        public float LastShotTime { get; private set; } = -10f;
        public bool LastHitWasKnockdown { get; private set; }
        public string Notice { get; private set; } = "";
        public float NoticeUntil { get; private set; }
        Vector3 restPosition;
        Quaternion restRotation;
        float nextShot;
        float reloadElapsed;
        float recoil;
        float flashUntil;
        AudioSource audioSource;
        AudioClip shotSound, reloadSound, emptySound, hitSound, headshotSound;
        Coroutine reloadRoutine;
        int inputEnabledFrame;

        void Awake()
        {
            Ammo = magazineSize;
            Reserve = startingReserve;
            restPosition = weaponModel.localPosition;
            restRotation = weaponModel.localRotation;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            shotSound = MakeSound("Shot", .13f, 90f, .65f);
            reloadSound = MakeSound("Magazine", .09f, 420f, .3f);
            emptySound = MakeSound("Empty", .055f, 160f, .16f);
            hitSound = MakeSound("Hit", .08f, 1200f, .13f);
            headshotSound = MakeSound("Headshot", .22f, 1800f, .04f);
            muzzleFlash.SetActive(false);
            flashLight.enabled = false;
        }

        void Update()
        {
            if (combatant != null) weaponModel.gameObject.SetActive(combatant.IsAlive && !combatant.game.IsFinished);
            if (!player.IsControlling) inputEnabledFrame = Time.frameCount + 1;
            if (player.IsControlling && player.CanAct && Time.frameCount > inputEnabledFrame)
            {
                if (Input.GetKeyDown(KeyCode.R)) BeginReload();
                if (combatant == null && Input.GetKeyDown(KeyCode.T)) Refill();
                if (Input.GetMouseButton(0)) TryFire();
            }
            bool flash = Time.time < flashUntil;
            muzzleFlash.SetActive(flash);
            flashLight.enabled = flash;
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 7f);
            float dip = IsReloading ? Mathf.Sin(ReloadProgress * Mathf.PI) : 0f;
            float bob = player.IsControlling && (Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical")) > .1f)
                ? Mathf.Sin(Time.time * 11f) * .008f : 0f;
            weaponModel.localPosition = restPosition + new Vector3(0f, bob - dip * .32f, -recoil * .085f);
            weaponModel.localRotation = restRotation * Quaternion.Euler(-recoil * 7f + dip * 24f, 0f, dip * -22f);
        }

        public bool TryFire()
        {
            if (combatant != null && (!combatant.IsAlive || combatant.game == null || !combatant.game.IsSimulationRunning)) return false;
            if (IsReloading || Time.time < nextShot) return false;
            nextShot = Time.time + fireInterval;
            if (Ammo <= 0)
            {
                audioSource.PlayOneShot(emptySound);
                ShowNotice(Reserve > 0 ? "ŞARJÖR BOŞ  /  R İLE DOLDUR" : "MERMİ BİTTİ  /  T İLE İKMAL", 1.2f);
                return false;
            }
            Ammo--;
            LastShotTime = Time.time;
            recoil = 1f;
            flashUntil = Time.time + .045f;
            audioSource.pitch = Random.Range(.95f, 1.05f);
            audioSource.PlayOneShot(shotSound, .65f);
            const int mask = ~((1 << 2) | (1 << 5));
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            Vector3 end = ray.GetPoint(range);
            RaycastHit hit;
            bool hasHit = combatant != null ? ArenaGame.Cast(ray.origin, ray.direction, range, combatant, out hit) : Physics.Raycast(ray, out hit, range, mask, QueryTriggerInteraction.Ignore);
            if (hasHit) end = hit.point;
            // A nearby wall between the barrel and the aim point must stop the shot.
            Vector3 path = end - muzzle.position;
            RaycastHit obstruction;
            bool barrelBlocked = combatant != null ? ArenaGame.Cast(muzzle.position, path.normalized, path.magnitude - .02f, combatant, out obstruction) : Physics.Raycast(muzzle.position, path.normalized, out obstruction, path.magnitude - .02f, mask, QueryTriggerInteraction.Ignore);
            if (barrelBlocked)
            { hit = obstruction; hasHit = true; end = hit.point; }
            if (hasHit)
            {
                ArenaHitbox box = hit.collider.GetComponent<ArenaHitbox>();
                if (box != null && combatant != null && box.owner.TakeDamage(combatant, box.isHead ? headDamage : bodyDamage, box.isHead))
                {
                    Hits++;
                    LastHitTime = Time.time;
                    LastHitWasHeadshot = box.isHead;
                    LastHitWasKnockdown = !box.owner.IsAlive;
                    if (LastHitWasKnockdown) Knockdowns++;
                    if (box.isHead) LastHeadshotTime = Time.time;
                    audioSource.PlayOneShot(box.isHead ? headshotSound : hitSound, box.isHead ? 1.2f : 1f);
                    combatant.game.SpawnHitBurst(hit.point, box.isHead);
                }
                RangeTarget target = hit.collider.GetComponentInParent<RangeTarget>();
                if (target != null && !target.IsDown)
                {
                    Hits++;
                    LastHitTime = Time.time;
                    LastHitWasKnockdown = target.Hit();
                    if (LastHitWasKnockdown) Knockdowns++;
                    audioSource.PlayOneShot(hitSound);
                }
                SpawnImpact(end, hit.normal);
            }
            GameObject tracer = new GameObject("Shot tracer");
            LineRenderer line = tracer.AddComponent<LineRenderer>();
            line.sharedMaterial = tracerMaterial;
            line.positionCount = 2;
            line.SetPosition(0, muzzle.position);
            line.SetPosition(1, end);
            line.startWidth = .018f;
            line.endWidth = .004f;
            Destroy(tracer, .055f);
            return true;
        }

        public bool BeginReload()
        {
            if (combatant != null && (!combatant.IsAlive || combatant.game == null || !combatant.game.IsSimulationRunning)) return false;
            if (IsReloading || Ammo >= magazineSize) return false;
            if (Reserve <= 0) { ShowNotice("YEDEK MERMİ YOK  /  T İLE İKMAL", 1.5f); return false; }
            reloadRoutine = StartCoroutine(Reload());
            return true;
        }

        IEnumerator Reload()
        {
            IsReloading = true;
            reloadElapsed = 0;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(reloadSound);
            while (reloadElapsed < reloadDuration)
            {
                yield return null;
                if (combatant == null || combatant.game.IsSimulationRunning) reloadElapsed += Time.deltaTime;
            }
            int transfer = Mathf.Min(magazineSize - Ammo, Reserve);
            Ammo += transfer;
            Reserve -= transfer;
            IsReloading = false;
            reloadRoutine = null;
            audioSource.PlayOneShot(reloadSound);
        }

        public void Refill()
        {
            CancelReload();
            Ammo = magazineSize;
            Reserve = startingReserve;
            ShowNotice("MÜHİMMAT YENİLENDİ", 1.5f);
        }

        public void CancelReload()
        {
            if (reloadRoutine != null) StopCoroutine(reloadRoutine);
            reloadRoutine = null;
            IsReloading = false;
        }

        void ShowNotice(string message, float duration) { Notice = message; NoticeUntil = Time.time + duration; }

        void SpawnImpact(Vector3 position, Vector3 normal)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.name = "Impact";
            spark.layer = 2;
            Destroy(spark.GetComponent<Collider>());
            spark.transform.position = position + normal * .025f;
            spark.transform.rotation = Quaternion.LookRotation(normal);
            spark.transform.localScale = new Vector3(.07f, .07f, .025f);
            spark.GetComponent<Renderer>().sharedMaterial = impactMaterial;
            Destroy(spark, .22f);
        }

        public static AudioClip MakeSound(string clipName, float duration, float frequency, float noise)
        {
            const int sampleRate = 22050;
            float[] samples = new float[(int)(sampleRate * duration)];
            var random = new System.Random(42);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Pow(1f - i / (float)samples.Length, 3f) * Mathf.Min(1f, t * 1000f);
                samples[i] = (Mathf.Sin(t * frequency * Mathf.PI * 2f) * (1f - noise) + ((float)random.NextDouble() * 2f - 1f) * noise) * envelope * .65f;
            }
            AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnDisable()
        {
            if (reloadRoutine != null) StopCoroutine(reloadRoutine);
            IsReloading = false;
            reloadRoutine = null;
            if (muzzleFlash != null) muzzleFlash.SetActive(false);
            if (flashLight != null) flashLight.enabled = false;
        }
        void OnDestroy() { Destroy(shotSound); Destroy(reloadSound); Destroy(emptySound); Destroy(hitSound); Destroy(headshotSound); }
    }
}
