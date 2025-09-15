using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MathMethods
{
    public static Transform FindClosestTransform(Transform[] transforms, Transform target)
    {
        if (transforms == null || transforms.Length == 0 || target == null)
        {
            return null; // Return null if the array is empty or the target is null.
        }

        Transform closestTransform = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform t in transforms)
        {
            float distance = Vector3.Distance(t.position, target.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTransform = t;
            }
        }

        return closestTransform;
    }

    public static Transform FindClosestTransform(List<Transform> transforms, Transform target)
    {
        if (transforms == null || transforms.Count == 0 || target == null)
        {
            return null; // Return null if the array is empty or the target is null.
        }

        Transform closestTransform = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform t in transforms)
        {
            float distance = Vector3.Distance(t.position, target.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTransform = t;
            }
        }

        return closestTransform;
    }

    public static int GetClosestIndex(float[] array, float target)
    {
        if (array == null || array.Length == 0)
            throw new System.ArgumentException("Array cannot be null or empty");

        int closestIndex = 0;
        float smallestDifference = Mathf.Abs(array[0] - target);

        for (int i = 1; i < array.Length; i++)
        {
            float difference = Mathf.Abs(array[i] - target);
            if (difference < smallestDifference)
            {
                smallestDifference = difference;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public static bool IsObjectInFront(GameObject object1, GameObject object2)
    {
        // Calculate the vector from Object 1 to Object 2
        Vector3 vectorToObject2 = object2.transform.position - object1.transform.position;

        // Calculate the vector from Object 1 to Object 2 normalized
        Vector3 normalizedVector = vectorToObject2.normalized;

        // Get the forward vector of Object 1 (assuming it's a Unity GameObject with a transform)
        Vector3 forwardVector = object1.transform.forward;

        // Calculate the dot product between the forward vector of Object 1 and the normalized vector between Object 1 and Object 2
        float dotProduct = Vector3.Dot(forwardVector, normalizedVector);

        return dotProduct > 0;
    }

    public static bool IsLookingAtTarget(Transform original, Transform target, float requiredAngle)
    {
        Vector3 direction = (target.position - original.position).normalized;
        var lookAngle = Vector3.Angle(original.forward, direction);

        if (lookAngle <= requiredAngle / 2)
            return true;

        return false;
    }

    public static bool IsApproximately(float a, float b, float threshold)
    {
        if (threshold > 0f)
        {
            return Mathf.Abs(a - b) <= threshold;
        }
        else
        {
            return Mathf.Approximately(a, b);
        }
    }

    public static IEnumerator MoveToPosition(GameObject obj, Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = obj.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            obj.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the object reaches the exact target position
        obj.transform.position = targetPosition;
    }

    public static IEnumerator RotateToTarget(GameObject obj, Vector3? targetDirection = null, GameObject targetObject = null, float duration = 1f)
    {
        // Check which mode to use - if `targetObject` is provided, use it as target, else use `targetDirection`.
        if (targetObject == null && targetDirection == null)
        {
            Debug.LogError("You must provide either a targetDirection or a targetObject.");
            yield break;
        }

        // Calculate the initial rotation and target rotation
        Quaternion startRotation = obj.transform.rotation;
        Quaternion targetRotation;

        if (targetObject != null)
        {
            // Face the target object
            targetRotation = Quaternion.LookRotation(targetObject.transform.position - obj.transform.position);
        }
        else
        {
            // Face the given direction
            targetRotation = Quaternion.LookRotation(targetDirection.Value);
        }

        float elapsedTime = 0f;

        // Rotate over the duration
        while (elapsedTime < duration)
        {
            obj.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the object reaches the exact target rotation
        obj.transform.rotation = targetRotation;
    }

    public static string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);

        return $"{minutes}:{seconds:D2}";
    }

    public static T[] ShuffleArray<T>(T[] array)
    {
        System.Random random = new System.Random();
        return array.OrderBy(x => random.Next()).ToArray();
    }
}