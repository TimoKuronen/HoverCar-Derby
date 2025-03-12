using UnityEngine;

public class CarPartConditionEffects : MonoBehaviour
{
    [SerializeField] private CarPartState partCondition;
    [SerializeField] private GameObject objectVisuals;
    [SerializeField] private AudioCue audioCue;

    public CarPartState PartCondition => partCondition;

    private ParticleSystem effectParticles;

    private void Start()
    {
        effectParticles = GetComponent<ParticleSystem>();
    }

    public void Toggle(bool value)
    {
        objectVisuals.SetActive(value);

        if (value)
        {
            if (effectParticles != null)
                effectParticles.Play();
        }
        else
        {
            if (effectParticles != null)
                effectParticles.Stop();
        }
    }
}
