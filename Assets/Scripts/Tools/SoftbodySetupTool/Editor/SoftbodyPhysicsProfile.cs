using UnityEngine;

[CreateAssetMenu(menuName = "Softbody 2D/Physics Profile", fileName = "SoftbodyPhysicsProfile")]
public class SoftbodyPhysicsProfile : ScriptableObject
{
    [Header("Spring Joint (ring + cross)")]
    [Range(0f, 1f)] public float SpringJointDampingRatio = 0.8f;
    [Min(0f)] public float SpringJointFrequency = 6f;
    public bool SpringEnableCollision = false;

    [Header("Anti Compression Strut")] 
    public bool UseAntiCompressionStrut = false;
    
    [Header("Rigidbodies (all bones)")]
    [Min(0f)] public float RigidbodyMass = 1f;
    [Min(0f)] public float RigidbodyLinearDamping = 0f;
    [Min(0f)] public float RigidbodyAngularDramping = 7.5f;
    public bool RigidbodyFreezeRotation = true;
    public RigidbodyInterpolation2D RigidbodyInterpolation = RigidbodyInterpolation2D.Interpolate;
    public CollisionDetectionMode2D CollisionDetection = CollisionDetectionMode2D.Continuous;

    [Header("Collider (all bones)")]
    [Min(0.1f)] public float ColliderSize = 0.5f;
    public bool UseColliderOffset = true;

}