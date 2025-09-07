using System.Collections;
using UnityEngine;

public abstract class CollisionCollectible : MonoBehaviour
{
    [SerializeField] private GameObject visuals;

    private Collider triggerCollider;
    private bool processed;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        processed = false;
    }

    private void OnEnable()
    {
        visuals.SetActive(true);
        triggerCollider.enabled = true;
    }

    private void OnCollisionEnter(Collision collidingCar)
    {
        if (processed || !collidingCar.gameObject.CompareTag("Vehicle"))
            return;

        float magnitude = collidingCar.relativeVelocity.magnitude;
        if (magnitude > 5)
        {
            ProcessItem(collidingCar);
        }
    }

    void ProcessItem(Collision collidingCar)
    {
        processed = true;
        triggerCollider.enabled = false;

        CollectItem(this, collidingCar.gameObject.GetComponent<CarManager>());
        StartCoroutine(PlayEffects());
    }

    private IEnumerator PlayEffects()
    {
        yield return new WaitForSeconds(2);

        //ReturnToPool();
    }

    protected abstract void CollectItem(CollisionCollectible collectible, CarManager carManager);
}
