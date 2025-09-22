using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

public static class SoftbodyTweakUtility
{
    public static void ApplyProfileTo(SoftbodyRuntime runtime, SoftbodyPhysicsProfile profile)
    {
        if (runtime == null || profile == null || runtime.SpriteSkin == null) return;

        var prevSelection = Selection.objects;
        Selection.objects = System.Array.Empty<Object>();

        var bones = runtime.SpriteSkin.boneTransforms;
        if (bones == null || bones.Length == 0) { Selection.objects = prevSelection; return; }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        for (int i = 0; i < bones.Length; i++)
        {
            if (runtime.HasCentralBone && i == 0) continue;

            var go = bones[i].gameObject;
            var rb = go.GetComponent<Rigidbody2D>();
            var collider = go.GetComponent<CircleCollider2D>();
            
            if (rb)
            {
                Undo.RecordObject(rb, "Apply Softbody Physics");
                rb.mass = profile.RigidbodyMass;
                rb.linearDamping = profile.RigidbodyLinearDamping;
                rb.angularDamping = profile.RigidbodyAngularDramping;
                rb.freezeRotation = profile.RigidbodyFreezeRotation;
                rb.interpolation = profile.RigidbodyInterpolation;
                rb.collisionDetectionMode = profile.CollisionDetection;
            }

            if (collider)
            {
                Undo.RecordObject(collider, "Apply Softbody Physics");
                collider.offset = profile.UseColliderOffset ? 
                    new Vector2(collider.radius / 2, 0) : 
                    Vector2.zero;
                collider.radius = profile.ColliderSize;
            }

            var springs = go.GetComponents<SpringJoint2D>();
            foreach (var sj in springs)
            {
                Undo.RecordObject(sj, "Apply Softbody Physics");
                sj.dampingRatio = profile.SpringJointDampingRatio;
                sj.frequency = profile.SpringJointFrequency;
                sj.enableCollision = profile.SpringEnableCollision;
            }
        }

        Undo.CollapseUndoOperations(group);
    }
}