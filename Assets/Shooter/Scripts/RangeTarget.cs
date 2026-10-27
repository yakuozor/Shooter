using System.Collections;
using UnityEngine;

namespace ShooterRange
{
    public sealed class RangeTarget : MonoBehaviour
    {
        public int maxHealth = 3;
        public float resetDelay = 2.2f;
        public Transform plate;
        public Renderer face;
        public Material normalMaterial;
        public Material hitMaterial;
        public int Health { get; private set; }
        public bool IsDown { get; private set; }
        Collider[] colliders;
        float flashUntil;
        Quaternion restRotation;

        void Awake()
        {
            Health = maxHealth;
            restRotation = plate.localRotation;
            colliders = plate.GetComponentsInChildren<Collider>();
        }

        void Update()
        {
            Quaternion goal = IsDown ? restRotation * Quaternion.Euler(82f, 0f, 0f) : restRotation;
            plate.localRotation = Quaternion.Slerp(plate.localRotation, goal, Time.deltaTime * 14f);
            face.sharedMaterial = Time.time < flashUntil ? hitMaterial : normalMaterial;
        }

        // Returns true only for the hit that knocks the target down.
        public bool Hit()
        {
            if (IsDown) return false;
            Health--;
            flashUntil = Time.time + .13f;
            if (Health > 0) return false;
            IsDown = true;
            foreach (Collider c in colliders) c.enabled = false;
            StartCoroutine(Recover());
            return true;
        }

        IEnumerator Recover()
        {
            yield return new WaitForSeconds(resetDelay);
            Health = maxHealth;
            IsDown = false;
            foreach (Collider c in colliders) c.enabled = true;
        }
    }
}
