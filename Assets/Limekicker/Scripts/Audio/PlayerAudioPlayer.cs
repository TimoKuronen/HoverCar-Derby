using Unity.Netcode;
using UnityEngine;
using VContainer;

[RequireComponent(typeof(Rigidbody), typeof(PlayerController))]
public class PlayerAudioPlayer : NetworkBehaviour
{
    [Header("Collision Audio Cues")]
    [SerializeField] private AudioCue lightCollisionCue;
    [SerializeField] private AudioCue mediumCollisionCue;
    [SerializeField] private AudioCue heavyCollisionCue;

    [Header("Collectible Audio Cues")]
    [SerializeField] private AudioCue collectibleCollectedCue;

    [Header("Collision Settings")]
    [SerializeField] private float minCollisionVelocity = 3f;
    [SerializeField] private float mediumCollisionThreshold = 8f;
    [SerializeField] private float heavyCollisionThreshold = 15f;
    [SerializeField] private float collisionCooldown = 0.2f;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    private IAudioService audioService;
    private Rigidbody rb;
    private PlayerController playerController;
    private EventBinding<CollectibleCollectedEvent> collectibleEventBinding;
    private float lastCollisionSoundTime;

    [Inject]
    public void Construct(IAudioService audioService)
    {
        this.audioService = audioService;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            collectibleEventBinding = new EventBinding<CollectibleCollectedEvent>(OnCollectibleCollected);
            EventBus<CollectibleCollectedEvent>.Register(collectibleEventBinding);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (collectibleEventBinding != null)
        {
            EventBus<CollectibleCollectedEvent>.Unregister(collectibleEventBinding);
        }

        base.OnNetworkDespawn();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner)
            return;

        if (Time.time - lastCollisionSoundTime < collisionCooldown)
            return;

        float relativeVelocity = collision.relativeVelocity.magnitude;

        if (relativeVelocity < minCollisionVelocity)
            return;

        CollisionIntensity intensity = GetCollisionIntensity(relativeVelocity);
        PlayCollisionSound(intensity);
        
        lastCollisionSoundTime = Time.time;
    }

    private CollisionIntensity GetCollisionIntensity(float relativeVelocity)
    {
        if (relativeVelocity >= heavyCollisionThreshold)
            return CollisionIntensity.Heavy;
        
        if (relativeVelocity >= mediumCollisionThreshold)
            return CollisionIntensity.Medium;
        
        return CollisionIntensity.Light;
    }

    private void PlayCollisionSound(CollisionIntensity intensity)
    {
        AudioCue cueToPlay = GetCollisionCue(intensity);
        
        if (cueToPlay != null && audioSource != null && audioService != null)
        {
            audioService.Play(cueToPlay, audioSource);
        }
    }

    private AudioCue GetCollisionCue(CollisionIntensity intensity)
    {
        return intensity switch
        {
            CollisionIntensity.Light => lightCollisionCue,
            CollisionIntensity.Medium => mediumCollisionCue,
            CollisionIntensity.Heavy => heavyCollisionCue,
            _ => null
        };
    }

    private void OnCollectibleCollected(CollectibleCollectedEvent collectibleEvent)
    {
        if (collectibleEvent.PlayerNetworkObjectId != NetworkObjectId)
            return;

        if (collectibleCollectedCue != null && audioSource != null && audioService != null)
        {
            audioService.Play(collectibleCollectedCue, audioSource);
        }
    }

    public void PlayCollisionSoundManually(CollisionIntensity intensity)
    {
        PlayCollisionSound(intensity);
    }
}

public enum CollisionIntensity
{
    Light,
    Medium,
    Heavy
}
