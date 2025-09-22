using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

public static class SoftbodyToolBuilder
{
    public class SoftbodyConfig
    {
        public SpriteRenderer Sprite;
        public SoftbodyPhysicsProfile PhysicsProfile;
        public bool HasCentralBone = false;
    }

    private enum JointTarget
    {
        Left,
        Right,
        Cross
    }
    
    private static int BoneCount(Transform[] bones, in SoftbodyConfig config) => config.HasCentralBone ? bones.Length - 1 : bones.Length;
    private static int ToBoneIndex(int ringIndex, in SoftbodyConfig config) => config.HasCentralBone ? ringIndex + 1 : ringIndex;
    
    public static GameObject CreateSoftBody(SoftbodyConfig config)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        var prevSelection = Selection.objects;
        Selection.objects = System.Array.Empty<Object>();

        var root = config.Sprite.gameObject;
        Undo.RecordObject(root, "Create Soft Body 2D");

        if(!root.GetComponent<SpriteSkin>()) root.AddComponent<SpriteSkin>();
        if(!root.GetComponent<SoftbodyRuntime>()) root.AddComponent<SoftbodyRuntime>();
        
        var spriteSkin =  root.GetComponent<SpriteSkin>();
        var runtime = root.GetComponent<SoftbodyRuntime>();

        CreateBones(spriteSkin);

        var bones = spriteSkin.boneTransforms;
        if (bones == null || bones.Length == 0)
        {
            EditorApplication.delayCall += () => Selection.objects = prevSelection;
            Debug.LogWarning($"[{nameof(SoftbodyToolBuilder)}] Aborting: SpriteSkin has no bones assigned on {spriteSkin.name}");
            return null;
        }

        SetupBones(bones, config);
        SetupParent(root, bones[0].GetComponent<Rigidbody2D>());

        int boneCount = BoneCount(bones, config);

        for (int i = 0; i < bones.Length; i++)
        {
            if (config.HasCentralBone && i == 0)
                continue;

            var boneGO = bones[i].gameObject;

            var distanceJoint = boneGO.AddComponent<DistanceJoint2D>();
            var leftSpringJoint = boneGO.AddComponent<SpringJoint2D>();
            var rightSpringJoint = boneGO.AddComponent<SpringJoint2D>();

            int boneIndex = config.HasCentralBone ? i - 1 : i;

            SetupBoneRB(config, bones[i].gameObject);
            SetupBoneCollider(config, bones[i].gameObject);
            
            SetupBoneDistanceJoint(config, distanceJoint, bones, boneIndex, boneCount);
            
            SetupBoneSpringJoint(config, leftSpringJoint,  bones, boneIndex, boneCount, JointTarget.Left);
            SetupBoneSpringJoint(config, rightSpringJoint, bones, boneIndex, boneCount, JointTarget.Right);

            if (!config.PhysicsProfile.UseAntiCompressionStrut)
            {
                var crossSpringJoint = boneGO.AddComponent<SpringJoint2D>();
                SetupBoneSpringJoint(config, crossSpringJoint, bones, boneIndex, boneCount, JointTarget.Cross);
            }
            else SetupAntiCompressionStrut(config, boneGO, bones, boneIndex, boneCount);
            
            // TRY THIS FOR FIXIING THE ROTATION ISSUE
            SetupBoneRelativeJointRot(root.GetComponent<Rigidbody2D>(), boneGO);
        }

        Undo.CollapseUndoOperations(group);

        runtime.HasCentralBone = config.HasCentralBone;
        runtime.SpriteSkin = root.GetComponent<SpriteSkin>();
        
        return root;
    }

    private static void SetupParent(GameObject parent, Rigidbody2D jointTarget)
    {
        if(!parent.GetComponent<CircleCollider2D>()) parent.AddComponent<CircleCollider2D>();
        if(!parent.GetComponent<Rigidbody2D>()) parent.AddComponent<Rigidbody2D>();
        if(!parent.GetComponent<RelativeJoint2D>()) parent.AddComponent<RelativeJoint2D>();
        
        var collider = parent.GetComponent<CircleCollider2D>();
        collider.radius *= 0.9f;
        collider.excludeLayers = LayerMask.NameToLayer("SoftbodyBones");
        
        parent.GetComponent<Rigidbody2D>().freezeRotation = true;
            
        parent.GetComponent<RelativeJoint2D>().connectedBody = jointTarget;
    }

    private static void SetupBones(Transform[] bones, in SoftbodyConfig config)
    {
        for (int i = 0; i < bones.Length; i++)
        {
            if (!bones[i].TryGetComponent<CircleCollider2D>(out _))
                bones[i].gameObject.AddComponent<CircleCollider2D>();
            
            if (!bones[i].TryGetComponent<Rigidbody2D>(out _))
                bones[i].gameObject.AddComponent<Rigidbody2D>();
        }
    }

    private static void SetupBoneSpringJoint(SoftbodyConfig config, SpringJoint2D joint, Transform[] bones, int boneIndex, int boneCount, JointTarget target)
    {
        Rigidbody2D targetBody = null;

        switch (target)
        {
            case JointTarget.Left:
            {
                int targetRingIndex = (boneIndex - 2 + boneCount) % boneCount;
                targetBody = bones[ToBoneIndex(targetRingIndex, config)].GetComponent<Rigidbody2D>();
                break;
            }
            case JointTarget.Right:
            {
                int targetRingIndex = (boneIndex + 2) % boneCount;
                targetBody = bones[ToBoneIndex(targetRingIndex, config)].GetComponent<Rigidbody2D>();
                break;
            }
            case JointTarget.Cross:
            {
                if (config.HasCentralBone)
                {
                    targetBody = bones[0].GetComponent<Rigidbody2D>();
                }
                else
                {
                    int targetRingIndex = (boneIndex + boneCount / 2) % boneCount;
                    targetBody = bones[ToBoneIndex(targetRingIndex, config)].GetComponent<Rigidbody2D>();
                }
                break;
            }
        }

        joint.connectedBody = targetBody;
        
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector2.zero;
        joint.connectedAnchor = Vector2.zero;
        
        
        joint.dampingRatio = config.PhysicsProfile.SpringJointDampingRatio;
        joint.frequency = config.PhysicsProfile.SpringJointFrequency;
        joint.enableCollision = config.PhysicsProfile.SpringEnableCollision;
    }

    private static void SetupBoneDistanceJoint(SoftbodyConfig config, DistanceJoint2D distanceJoint, Transform[] bones, int boneIndex, int boneCount)
    {
        int nextBoneIndex = (boneIndex + 1) % boneCount;
        distanceJoint.connectedBody = bones[ToBoneIndex(nextBoneIndex, config)].GetComponent<Rigidbody2D>();
        
        distanceJoint.autoConfigureConnectedAnchor = false;
        distanceJoint.anchor = Vector2.zero;
        distanceJoint.connectedAnchor = Vector2.zero;
    }

    private static void SetupBoneRelativeJointRot(Rigidbody2D parent, GameObject bone)
    {
        var joint = bone.AddComponent<RelativeJoint2D>();
        joint.connectedBody = parent;

        joint.autoConfigureOffset = false;
        joint.linearOffset = Vector2.zero; 
        joint.maxForce = 0f;

        joint.angularOffset = 0f;
        joint.maxTorque = 1000f;
        joint.correctionScale = 1f;

    }

    private static void SetupBoneCollider(SoftbodyConfig config, GameObject boneGO)
    {
        var collider = boneGO.GetComponent<CircleCollider2D>() ?? boneGO.AddComponent<CircleCollider2D>();
        
        collider.radius = config.PhysicsProfile.ColliderSize;
        collider.offset = config.PhysicsProfile.UseColliderOffset ? 
            new Vector2(collider.radius / 2, 0) : 
            Vector2.zero;
    }
    
    private static void SetupBoneRB(SoftbodyConfig config, GameObject boneGO)
    {
        var rb = boneGO.GetComponent<Rigidbody2D>() ?? boneGO.AddComponent<Rigidbody2D>();
        
        rb.constraints = RigidbodyConstraints2D.FreezePositionX;
        rb.freezeRotation = config.PhysicsProfile.RigidbodyFreezeRotation;
        rb.angularDamping = config.PhysicsProfile.RigidbodyAngularDramping;
        rb.linearDamping = config.PhysicsProfile.RigidbodyLinearDamping;
        rb.interpolation = config.PhysicsProfile.RigidbodyInterpolation;
        rb.collisionDetectionMode = config.PhysicsProfile.CollisionDetection;
    }
    
    private static void SetupAntiCompressionStrut(SoftbodyConfig config, GameObject boneGO, Transform[] bones, int boneIndex, int boneCount)
    {
        Rigidbody2D targetBody;
        Vector3 targetPos;

        if (config.HasCentralBone)
        {
            targetBody = bones[0].GetComponent<Rigidbody2D>();
            targetPos  = bones[0].position;
        }
        else
        {
            int targetRingIndex = (boneIndex + boneCount / 2) % boneCount;
            int targetBoneIndex = ToBoneIndex(targetRingIndex, config);
            targetBody = bones[targetBoneIndex].GetComponent<Rigidbody2D>();
            targetPos  = bones[targetBoneIndex].position;
        }

        var rb = boneGO.GetComponent<Rigidbody2D>();
        var strut = boneGO.AddComponent<DistanceJoint2D>();
        strut.connectedBody = targetBody;

        strut.maxDistanceOnly = false;
        strut.enableCollision = true;

        float restDistance = Vector2.Distance(rb.position, targetPos);
        strut.autoConfigureDistance = false;
        strut.distance = restDistance;
    }
    
    private static void CreateBones(SpriteSkin spriteSkin)
    {
        var spriteRenderer = spriteSkin.GetComponent<SpriteRenderer>();
        
        if (!spriteRenderer || !spriteRenderer.sprite)
        {
            Debug.LogWarning($"[{nameof(SoftbodyToolBuilder)}] Missing SpriteRenderer/Sprite on {spriteSkin.name}");
            return;
        }

        var bones = spriteRenderer.sprite.GetBones();
        if (bones == null || bones.Length == 0)
        {
            Debug.LogWarning($"[{nameof(SoftbodyToolBuilder)}] Sprite has no SpriteBones: {spriteRenderer.sprite.name}");
            return;
        }

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();

        var root = new GameObject("BoneRoot").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Bones");
        
        root.SetParent(spriteSkin.transform, false);

        var newBones = new Transform[bones.Length];
        
        for (int i = 0; i < bones.Length; i++)
        {
            var bone = bones[i];
            
            var boneGO = new GameObject(string.IsNullOrEmpty(bone.name) ? $"Bone_{i}" : bone.name);
            Undo.RegisterCreatedObjectUndo(boneGO, "Create Bone");
            
            var boneTransform = boneGO.transform;

            if (bone.parentId < 0) boneTransform.SetParent(root, false);
            else boneTransform.SetParent(newBones[bone.parentId], false);

            boneTransform.localPosition = bone.position;
            boneTransform.localRotation = bone.rotation;
            boneTransform.localScale = Vector3.one;

            newBones[i] = boneTransform;
            
            boneGO.layer = LayerMask.NameToLayer("SoftbodyBones");
        }

        var serializedSpriteSkin = new SerializedObject(spriteSkin);
        serializedSpriteSkin.Update();

        var rootProperty = serializedSpriteSkin.FindProperty("m_RootBone");
        rootProperty.objectReferenceValue = root;

        var bonesProperty = serializedSpriteSkin.FindProperty("m_BoneTransforms");
        
        bonesProperty.arraySize = newBones.Length;
        
        for (int i = 0; i < newBones.Length; i++)
            bonesProperty.GetArrayElementAtIndex(i).objectReferenceValue = newBones[i];

        serializedSpriteSkin.ApplyModifiedProperties();
        EditorUtility.SetDirty(spriteSkin);

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[{nameof(SoftbodyToolBuilder)}] Created {newBones.Length} bones and assigned root for {spriteSkin.name}");
    }
}