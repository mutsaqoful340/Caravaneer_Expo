using UnityEngine;

public enum RepairMaterialFlightPath
{
    Straight,
    Curved
}

public class RepairMaterialVisual : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private RepairMaterialFlightPath flightPath = RepairMaterialFlightPath.Straight;
    [SerializeField] private float flySpeed = 8f;
    [SerializeField] private float arcHeight = 1.5f;
    [SerializeField] private AnimationCurve arcCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f));
    [SerializeField] private float arrivalDistance = 0.05f;

    [Header("Rotation")]
    [SerializeField] private bool rotateTowardsMovement;
    [SerializeField] private float rotationSpeed = 720f;

    private Transform target;
    private Vector3 startPosition;
    private float flightDuration;
    private float flightTime;

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
        startPosition = transform.position;
        flightTime = 0f;
        flightDuration = Vector3.Distance(startPosition, target.position)
            / Mathf.Max(0.01f, flySpeed);
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        if (flightPath == RepairMaterialFlightPath.Straight)
        {
            MoveStraight();
        }
        else
        {
            MoveAlongCurve();
        }

        if (rotateTowardsMovement)
        {
            RotateTowardsMovement();
        }
    }

    private void MoveStraight()
    {
        Vector3 previousPosition = transform.position;
        transform.position = Vector3.MoveTowards(
            previousPosition,
            target.position,
            flySpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) <= arrivalDistance)
        {
            transform.position = target.position;
        }
    }

    private void MoveAlongCurve()
    {
        flightTime += Time.deltaTime;
        float progress = Mathf.Clamp01(flightTime / Mathf.Max(0.01f, flightDuration));
        Vector3 previousPosition = transform.position;
        Vector3 directPosition = Vector3.Lerp(startPosition, target.position, progress);
        float arcOffset = arcCurve.Evaluate(progress) * arcHeight;
        transform.position = directPosition + Vector3.up * arcOffset;

        if (Vector3.Distance(transform.position, target.position) <= arrivalDistance
            || progress >= 1f)
        {
            transform.position = target.position;
        }
    }

    private void RotateTowardsMovement()
    {
        Vector3 movement = transform.position - startPosition;
        if (movement.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(movement.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }
}