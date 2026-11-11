using UnityEngine;

/// <summary>
/// Supplies a lightweight idle and run cycle for the Adventure Female rig.
/// The imported asset has a Humanoid Avatar but no animation clips/controller.
/// This component poses the existing bones and leaves the CharacterController
/// as the sole authority for the player's movement and collisions.
/// </summary>
[DisallowMultipleComponent]
public sealed class AventureraLocomotionPose : MonoBehaviour
{
    [SerializeField, Range(0f, 75f)] float idleArmDrop = 75f;
    [SerializeField, Range(0f, 1f)] float armSwing = 0.65f;
    [SerializeField, Range(0f, 1f)] float legSwing = 0.68f;
    [SerializeField, Range(0f, 1f)] float kneeBend = 0.5f;
    [SerializeField, Min(0.1f)] float runSpeedForFullCycle = 6.2f;

    Animator animator;
    SegmentPose leftUpperArm;
    SegmentPose rightUpperArm;
    SegmentPose leftLowerArm;
    SegmentPose rightLowerArm;
    SegmentPose leftUpperLeg;
    SegmentPose rightUpperLeg;
    SegmentPose leftLowerLeg;
    SegmentPose rightLowerLeg;
    Vector3 previousPosition;
    Quaternion avatarRootRotationAtCapture;
    float movementBlend;
    float phase;
    bool aiming;

    public void SetAiming(bool value) => aiming = value;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        previousPosition = transform.position;
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogError("La aventurera necesita un Animator con Avatar Humanoid para mover su esqueleto.", this);
            enabled = false;
            return;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        avatarRootRotationAtCapture = animator.transform.rotation;
        leftUpperArm = Capture(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm);
        rightUpperArm = Capture(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm);
        leftLowerArm = Capture(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
        rightLowerArm = Capture(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
        leftUpperLeg = Capture(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg);
        rightUpperLeg = Capture(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg);
        leftLowerLeg = Capture(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
        rightLowerLeg = Capture(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
    }

    void LateUpdate()
    {
        if (animator == null || Time.deltaTime <= 0f) return;

        var position = transform.position;
        var displacement = position - previousPosition;
        previousPosition = position;
        displacement.y = 0f;
        var measuredSpeed = displacement.magnitude / Time.deltaTime;
        var targetBlend = Mathf.Clamp01(measuredSpeed / runSpeedForFullCycle);
        movementBlend = Mathf.MoveTowards(movementBlend, targetBlend, Time.deltaTime * 7f);
        if (movementBlend > 0.01f)
            phase += Time.deltaTime * Mathf.Lerp(7.5f, 11.5f, movementBlend);

        var gait = Mathf.SmoothStep(0f, 1f, movementBlend);
        var step = Mathf.Sin(phase) * gait;
        var leftKnee = Mathf.Max(0f, step) * kneeBend;
        var rightKnee = Mathf.Max(0f, -step) * kneeBend;
        var up = transform.up;
        var forward = transform.forward;
        var right = transform.right;

        // Start from each arm's actual bind-pose direction, then aim it down
        // beside the body. The small forward/back offset creates an opposing run swing.
        var shoulderDrop = Mathf.Clamp01(idleArmDrop / 75f);
        var sideReach = Mathf.Lerp(1f, 0.22f, shoulderDrop);
        var downReach = Mathf.Lerp(0f, 0.96f, shoulderDrop);
        var leftArmDirection = (-right * sideReach - up * downReach - forward * step * armSwing).normalized;
        var rightArmDirection = (right * sideReach - up * downReach + forward * step * armSwing).normalized;
        var leftForearmDirection = (-up * 0.88f + forward * (0.34f - step * armSwing * 0.18f)).normalized;
        var rightForearmDirection = (-up * 0.88f + forward * (0.34f + step * armSwing * 0.18f)).normalized;

        if (aiming)
        {
            // Bring both hands forward while the player aims, without changing the
            // CharacterController or disturbing the run cycle in the legs.
            leftArmDirection = (forward * 0.70f - right * 0.18f + up * 0.10f).normalized;
            rightArmDirection = (forward * 0.94f + right * 0.04f + up * 0.20f).normalized;
            leftForearmDirection = (forward * 0.92f + up * 0.08f).normalized;
            rightForearmDirection = (forward * 0.96f + up * 0.10f).normalized;
        }
        PoseSegment(leftUpperArm, leftArmDirection);
        PoseSegment(rightUpperArm, rightArmDirection);
        PoseSegment(leftLowerArm, leftForearmDirection);
        PoseSegment(rightLowerArm, rightForearmDirection);

        var leftLegDirection = (-up + forward * step * legSwing).normalized;
        var rightLegDirection = (-up - forward * step * legSwing).normalized;
        PoseSegment(leftUpperLeg, leftLegDirection);
        PoseSegment(rightUpperLeg, rightLegDirection);
        PoseSegment(leftLowerLeg, (-up - forward * leftKnee * 0.65f).normalized);
        PoseSegment(rightLowerLeg, (-up - forward * rightKnee * 0.65f).normalized);
    }

    SegmentPose Capture(HumanBodyBones bone, HumanBodyBones childBone)
    {
        var boneTransform = animator.GetBoneTransform(bone);
        var childTransform = animator.GetBoneTransform(childBone);
        if (boneTransform == null || childTransform == null)
        {
            Debug.LogWarning("No se encontraron los huesos Humanoid " + bone + " / " + childBone + ".", this);
            return default;
        }

        var direction = (childTransform.position - boneTransform.position).normalized;
        return new SegmentPose(boneTransform, boneTransform.rotation, direction);
    }

    void PoseSegment(SegmentPose segment, Vector3 desiredDirection)
    {
        if (segment.Bone == null) return;

        var avatarRootDelta = animator.transform.rotation * Quaternion.Inverse(avatarRootRotationAtCapture);
        var restDirection = avatarRootDelta * segment.RestDirection;
        var restRotation = avatarRootDelta * segment.RestRotation;
        var correction = Quaternion.FromToRotation(restDirection, desiredDirection.normalized);
        segment.Bone.rotation = correction * restRotation;
    }

    readonly struct SegmentPose
    {
        public readonly Transform Bone;
        public readonly Quaternion RestRotation;
        public readonly Vector3 RestDirection;

        public SegmentPose(Transform bone, Quaternion restRotation, Vector3 restDirection)
        {
            Bone = bone;
            RestRotation = restRotation;
            RestDirection = restDirection;
        }
    }
}
