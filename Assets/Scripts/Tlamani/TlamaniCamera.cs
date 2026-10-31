using UnityEngine;
namespace Noctal
{
    public sealed class TlamaniCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.12f;
        private Vector3 velocity;
        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = new Vector3(target.position.x, target.position.y + 1.5f, -10f);
            if (Vector3.Distance(transform.position, desired) > 15f) { transform.position = desired; velocity = Vector3.zero; }
            else transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
