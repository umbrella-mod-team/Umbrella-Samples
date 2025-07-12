using System.Linq;
using UnityEngine;
using WIGU;

namespace WIGUx.Modules.screenSocketModule
{
    /// <summary>
    /// Snaps this UGC onto another UGC at matching "screensocket" colliders,
    /// then locks them together with a FixedJoint.
    /// Place this on the side-screen prefab. Ensure both prefabs have a
    /// BoxCollider named "screensocket" at the exact snap position, and
    /// each has a Rigidbody. The main UGC prefab need not have this script.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class screenSocketController : MonoBehaviour
    {
        [Tooltip("Name of the BoxCollider on this object to use as the source socket.")]
        public string sourceSocketName = "screensocket";
        [Tooltip("Name of the BoxCollider on the target object to match.")]
        public string targetSocketName = "screensocket";

        [Tooltip("Force required to break the joint (use Mathf.Infinity to never break).")]
        public float breakForce = Mathf.Infinity;
        [Tooltip("Torque required to break the joint (use Mathf.Infinity to never break).")]
        public float breakTorque = Mathf.Infinity;

        private BoxCollider sourceSocket;
        private FixedJoint joint;

        void Start()
        {
            // Cache the source socket collider for snapping
            sourceSocket = GetComponentsInChildren<BoxCollider>()
                .FirstOrDefault(c => c.name == sourceSocketName);
            if (sourceSocket == null)
            {
                Debug.LogError($"[{name}] No BoxCollider named '{sourceSocketName}' found.");
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            // Already attached? Skip.
            if (joint != null) return;

            // Find the matching socket on the collided object
            var targetSocket = collision.gameObject
                .GetComponentsInChildren<BoxCollider>()
                .FirstOrDefault(c => c.name == targetSocketName);

            if (sourceSocket == null || targetSocket == null) return;

            // Compute world-space centers
            Vector3 worldCenterA = sourceSocket.transform.TransformPoint(sourceSocket.center);
            Vector3 worldCenterB = targetSocket.transform.TransformPoint(targetSocket.center);

            // 1) Snap position: move this object so its socket center matches the target
            Vector3 delta = worldCenterB - worldCenterA;
            transform.position += delta;

            // 2) Snap rotation: align this socket's orientation to the target socket
            Quaternion rotA = sourceSocket.transform.rotation;
            Quaternion rotB = targetSocket.transform.rotation;
            Quaternion adjustment = rotB * Quaternion.Inverse(rotA);
            transform.rotation = adjustment * transform.rotation;

            // 3) Lock together: create a FixedJoint at the socket points
            Rigidbody otherRb = collision.rigidbody;
            if (otherRb != null)
            {
                joint = gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = otherRb;
                joint.breakForce = breakForce;
                joint.breakTorque = breakTorque;

                // Set joint anchors to the local centers of each collider
                joint.anchor = sourceSocket.center;
                joint.connectedAnchor = transform.InverseTransformPoint(worldCenterB);

                Debug.Log($"[{name}] Snapped and locked to '{collision.gameObject.name}'.");
            }
            else
            {
                Debug.LogWarning($"[{name}] Cannot lock: target '{collision.gameObject.name}' has no Rigidbody.");
            }
        }

        void OnCollisionExit(Collision collision)
        {
            // Detach when separated
            if (joint != null && collision.rigidbody == joint.connectedBody)
            {
                Destroy(joint);
                joint = null;
                Debug.Log($"[{name}] Detached from '{collision.gameObject.name}'.");
            }
        }
    }
}
