using UnityEngine;

public class CarCollisionHandler : MonoBehaviour
{
    private CarDamageManager damageManager;

    private void Start()
    {
        damageManager = GetComponent<CarDamageManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        return; // Temporarily disable collision damage

        float impactForce = collision.relativeVelocity.magnitude;
        //Debug.Log(impactForce);

        if (impactForce < 5)
        {
            return;
        }

        Vector3 hitDirection = collision.contacts[0].normal;

        float forwardDot = Vector3.Dot(hitDirection, transform.forward);
        float rightDot = Vector3.Dot(hitDirection, transform.right);

        if (forwardDot > 0.8f)
        {
            int otherLayer = collision.gameObject.layer;

            if (otherLayer == LayerMask.NameToLayer("Car"))
            {
                CarDamageManager otherCar = collision.collider.GetComponent<CarDamageManager>();
                if (otherCar != null)
                {
                    //otherCar.DealDamageByCar
                }
            }

            damageManager.ApplyDamageToPart(CarPartType.FrontBumper, impactForce * 2f);
        }
        else if (rightDot > 0.5f)
        {
            damageManager.ApplyDamageToPart(CarPartType.SidePanel_Right, impactForce * 1.2f);
        }
        else if (rightDot < -0.5f)
        {
            damageManager.ApplyDamageToPart(CarPartType.SidePanel_Left, impactForce * 1.2f);
        }
        else
        {
            damageManager.ApplyDamageToPart(CarPartType.RearBumper, impactForce * 0.8f);
        }
    }
}