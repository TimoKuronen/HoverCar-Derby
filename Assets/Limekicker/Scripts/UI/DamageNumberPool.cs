using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class DamageNumberPool : MonoBehaviour
{
    public enum ShowMode
    {
        AttackerOnly,   // Only show damage to the player who dealt it
        VictimOnly,     // Only show damage to the player who received it
        Both,           // Show to both attacker and victim
        AllPlayers      // Show to everyone
    }

    [Header("UI Prefab (TextMeshProUGUI)")]
    [SerializeField] private TextMeshProUGUI damageNumberPrefab;

    [Header("Canvas where numbers spawn")]
    [SerializeField] private Canvas targetCanvas;

    [Header("Settings")]
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float floatSpeed = 40f;
    [SerializeField] private float lifetime = 1.0f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 0.6f, 0);
    [SerializeField] private Color defaultColor = Color.red;
    [SerializeField] private ShowMode visibility = ShowMode.Both;

    private readonly Queue<TextMeshProUGUI> pool = new();
    private Camera cam;
    private static DamageNumberPool instance;

    public static DamageNumberPool Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DamageNumberPool>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        cam = Camera.main;
        if (targetCanvas == null)
        {
            Debug.LogError("DamageNumberPool: No Canvas assigned.");
            enabled = false;
            return;
        }

        if (damageNumberPrefab == null)
        {
            Debug.LogError("DamageNumberPool: No damageNumberPrefab assigned.");
            enabled = false;
            return;
        }

        for (int i = 0; i < poolSize; i++)
            CreateInstance();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private TextMeshProUGUI CreateInstance()
    {
        var inst = Instantiate(damageNumberPrefab, targetCanvas.transform);
        inst.gameObject.SetActive(false);
        pool.Enqueue(inst);
        return inst;
    }

    /// <summary>
    /// Shows a damage number if it should be visible to the local client based on ShowMode.
    /// </summary>
    /// <param name="worldPosition">World position where damage occurred</param>
    /// <param name="amount">Damage amount</param>
    /// <param name="attackerClientId">Client ID of the player who dealt damage (ulong.MaxValue for non-player sources)</param>
    /// <param name="victimClientId">Client ID of the player who received damage (ulong.MaxValue for non-player sources)</param>
    public void ShowDamageNumber(Vector3 worldPosition, float amount, ulong attackerClientId = ulong.MaxValue, ulong victimClientId = ulong.MaxValue)
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("DamageNumberPool: No camera found.");
                return;
            }
        }

        // Check if we should show this damage number based on visibility mode
        if (!ShouldShowDamage(attackerClientId, victimClientId))
            return;

        var text = pool.Count > 0 ? pool.Dequeue() : CreateInstance();
        text.text = Mathf.RoundToInt(amount).ToString();
        text.color = defaultColor;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition + worldOffset);
        
        // Check if position is behind camera
        if (screenPos.z < 0)
        {
            pool.Enqueue(text);
            return;
        }

        text.transform.position = screenPos;
        text.gameObject.SetActive(true);

        StartCoroutine(Animate(text, screenPos, worldPosition));
    }

    /// <summary>
    /// Determines if damage should be shown to the local client based on ShowMode.
    /// </summary>
    private bool ShouldShowDamage(ulong attackerClientId, ulong victimClientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            // If not networked, always show
            return visibility == ShowMode.AllPlayers || visibility == ShowMode.Both;
        }

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        bool isAttacker = attackerClientId != ulong.MaxValue && attackerClientId == localClientId;
        bool isVictim = victimClientId != ulong.MaxValue && victimClientId == localClientId;

        return visibility switch
        {
            ShowMode.AttackerOnly => isAttacker,
            ShowMode.VictimOnly => isVictim,
            ShowMode.Both => isAttacker || isVictim,
            ShowMode.AllPlayers => true,
            _ => false
        };
    }

    private IEnumerator Animate(TextMeshProUGUI text, Vector3 startScreenPos, Vector3 worldPosition)
    {
        float t = 0f;
        Color startColor = text.color;

        while (t < lifetime)
        {
            t += Time.deltaTime;
            float p = t / lifetime;

            // Update world position to screen position each frame (in case camera moves)
            if (cam != null)
            {
                Vector3 currentWorldPos = worldPosition + worldOffset + Vector3.up * floatSpeed * p;
                Vector3 currentScreenPos = cam.WorldToScreenPoint(currentWorldPos);
                
                // Stop animating if behind camera
                if (currentScreenPos.z < 0)
                {
                    break;
                }
                
                text.transform.position = currentScreenPos;
            }
            else
            {
                // Fallback to screen space animation if camera is lost
                text.transform.position = startScreenPos + Vector3.up * floatSpeed * p;
            }

            // Fade out
            Color c = startColor;
            c.a = 1f - p;
            text.color = c;

            yield return null;
        }

        text.gameObject.SetActive(false);
        pool.Enqueue(text);
    }
}
