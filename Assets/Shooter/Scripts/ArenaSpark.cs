using UnityEngine;

namespace ShooterRange
{
    public sealed class ArenaSpark : MonoBehaviour
    {
        public Vector3 velocity;
        void Update()
        {
            velocity += Vector3.down * (9f * Time.deltaTime);
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(new Vector3(90, 170, 45) * Time.deltaTime);
        }
    }
}
