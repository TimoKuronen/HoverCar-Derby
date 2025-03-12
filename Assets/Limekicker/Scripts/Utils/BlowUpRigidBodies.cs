using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlowUpRigidBodies : MonoBehaviour
{
    [SerializeField] private float explosionForce;
    [SerializeField] private float rotationalForce;
    [SerializeField] private float explosionRadius;
    [SerializeField] private Vector3 relativeExplosionLocation;
    [SerializeField] private Rigidbody[] bodies;
    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;

    public void Awake()
    {
        if(bodies==null)
            bodies = GetComponentsInChildren<Rigidbody>();
        originalPositions = new Vector3[bodies.Length];
        originalRotations = new Quaternion[bodies.Length];

        for (int i = 0; i < bodies.Length; i++)
        {
            originalPositions[i] = bodies[i].transform.localPosition;
            originalRotations[i] = bodies[i].transform.localRotation;
        }
    }

    private void OnEnable()
    {
        BlowUp();
    }

    private void OnDisable()
    {
        if (bodies.Length > 0)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i].gameObject.activeInHierarchy)
                {
                    bodies[i].isKinematic = true;
                    //colliders[i].transform.parent = transform;
                    bodies[i].transform.localPosition = originalPositions[i];
                    bodies[i].transform.localRotation = originalRotations[i];
                }
            }
        }
    }

    public void BlowUp()
    {
        foreach (Rigidbody rb in bodies)
        {
            //rb.transform.parent = null;
            rb.isKinematic = false;
            Vector3 relativeDir = transform.right * relativeExplosionLocation.x + transform.up * relativeExplosionLocation.y + transform.forward * relativeExplosionLocation.z;
            rb.AddExplosionForce(explosionForce, transform.position + relativeDir, explosionRadius);
            rb.AddTorque(rotationalForce, rotationalForce, rotationalForce, ForceMode.Impulse);
        }
    }

    private void Disable()
    {
        gameObject.SetActive(false);
    }
}
