using System.Collections.Generic;
using UnityEngine;

namespace ShooterRange
{
    [RequireComponent(typeof(ArenaCombatant))]
    public sealed class ArenaBot : MonoBehaviour
    {
        public float moveSpeed = 2.6f;
        public float shotInterval = .65f;
        public int ShotsFired { get; private set; }
        public int DamageHits { get; private set; }
        ArenaCombatant self, target;
        List<Vector3> path = new List<Vector3>();
        int pathIndex;
        float nextDecision, nextShot, stuckTime;
        int magazine = 8;
        Vector3 lastPosition;

        void Awake() { self = GetComponent<ArenaCombatant>(); }
        public void ResetBrain()
        {
            path.Clear(); pathIndex = 0; target = null;
            nextDecision = 0; nextShot = Time.time + Random.Range(.6f, 1.4f);
            magazine = 8; stuckTime = 0; lastPosition = transform.position;
        }

        void Update()
        {
            if (self.game == null || !self.game.IsSimulationRunning || !self.IsAlive) return;
            if (Time.time >= nextDecision)
            {
                nextDecision = Time.time + Random.Range(.45f, .8f);
                target = PickTarget();
                Vector3 destination = target != null ? target.transform.position : self.game.map.SpawnPoints[Random.Range(0, self.game.map.SpawnPoints.Count)];
                path = self.game.map.FindPath(transform.position, destination);
                pathIndex = 0;
            }
            bool visible = target != null && target.IsAlive && !target.IsProtected && CanSee(target);
            if (visible)
            {
                Vector3 delta = target.transform.position - transform.position; delta.y = 0;
                if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 8f);
                if (Time.time >= nextShot)
                {
                    if (magazine <= 0) { magazine = 8; nextShot = Time.time + 1.8f; }
                    else Fire();
                }
            }
            // Close for a shot, then stop to aim. The simple bots do not shoot through cover.
            bool shouldMove = !visible || Vector3.Distance(transform.position, target.transform.position) > 12f;
            Vector3 velocity = Vector3.zero;
            if (shouldMove && pathIndex < path.Count)
            {
                Vector3 delta = path[pathIndex] - transform.position; delta.y = 0;
                if (delta.magnitude < .22f) pathIndex++;
                else
                {
                    velocity = delta.normalized * moveSpeed;
                    if (!visible) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 7f);
                }
            }
            self.motor.Move((velocity + Vector3.down * 3f) * Time.deltaTime);
            if (shouldMove && velocity.sqrMagnitude > .1f && Vector3.Distance(lastPosition, transform.position) < .003f) stuckTime += Time.deltaTime;
            else stuckTime = 0;
            if (stuckTime > .6f)
            {
                // Step sideways inside a walkable cell when another actor blocks the route.
                Vector3 side = transform.right * (GetInstanceID() % 2 == 0 ? 1f : -1f);
                self.motor.Move(side * moveSpeed * Time.deltaTime);
                nextDecision = 0;
            }
            lastPosition = transform.position;
        }

        ArenaCombatant PickTarget()
        {
            ArenaCombatant best = null;
            float score = float.MaxValue;
            foreach (var candidate in self.game.Combatants)
            {
                if (candidate == self || !candidate.IsAlive || candidate.IsProtected) continue;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (CanSee(candidate)) distance *= .3f;
                if (distance < score) { score = distance; best = candidate; }
            }
            return best;
        }

        public bool CanSee(ArenaCombatant other)
        {
            Vector3 origin = transform.position + Vector3.up * 1.42f;
            Vector3 delta = other.transform.position + Vector3.up * 1.03f - origin;
            return ArenaGame.Cast(origin, delta.normalized, delta.magnitude + .2f, self, out RaycastHit hit) && hit.collider.GetComponent<ArenaHitbox>()?.owner == other;
        }

        void Fire()
        {
            nextShot = Time.time + shotInterval * Random.Range(.9f, 1.4f);
            magazine--; ShotsFired++;
            Vector3 origin = transform.position + Vector3.up * 1.27f + transform.forward * .55f + transform.right * .25f;
            Vector3 aim = target.transform.position + Vector3.up * 1.05f;
            // Modest accuracy and low damage give the human time to react.
            aim += new Vector3(Random.Range(-.8f, .8f), Random.Range(-.4f, .4f), Random.Range(-.25f, .25f));
            Vector3 direction = (aim - origin).normalized;
            Vector3 end = origin + direction * 40f;
            if (ArenaGame.Cast(origin, direction, 40f, self, out RaycastHit hit))
            {
                end = hit.point;
                ArenaHitbox box = hit.collider.GetComponent<ArenaHitbox>();
                if (box != null && box.owner.TakeDamage(self, box.isHead ? 18 : 12, box.isHead)) DamageHits++;
            }
            self.game.DrawTracer(origin, end, .045f);
            self.game.PlayBotShot(origin);
        }
    }
}
