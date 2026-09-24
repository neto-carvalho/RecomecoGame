using System;
using Controller;
using UnityEngine;

public class PlayerNeeds : MonoBehaviour
{
    public const float MaxHunger = 100f;
    public const float MaxHealth = 100f;
    public const float MaxReputation = 100f;

    public static event Action Changed;
    public static PlayerNeeds Instance { get; private set; }

    [SerializeField] float hunger = MaxHunger;
    [SerializeField] float health = MaxHealth;
    [SerializeField] float reputation = MaxReputation;

    bool _faintLocked;
    CharacterMover _mover;

    public float Hunger => hunger;
    public float Health => health;
    public float Reputation => reputation;
    public bool IsFaintLocked => _faintLocked;

    void Awake()
    {
        Instance = this;
        _mover = GetComponent<CharacterMover>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (_faintLocked || PlayerFaintSequence.IsPlaying ||
            RecomecoSceneNames.IsMenuScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()))
            return;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings == null)
            return;

        var drainPerSecond = settings.hungerDrainPerMinute / 60f;
        if (_mover != null && _mover.IsRun)
            drainPerSecond *= settings.hungerDrainRunMultiplier;

        hunger = Mathf.Max(0f, hunger - drainPerSecond * Time.deltaTime);

        if (hunger <= 0f)
        {
            health = Mathf.Max(0f, health - settings.healthLossPerSecondWhenStarving * Time.deltaTime);
        }
        else if (hunger >= settings.healthRegenMinHunger)
        {
            health = Mathf.Min(MaxHealth, health + settings.healthRegenPerSecond * Time.deltaTime);
        }

        if (health <= 0f)
            PlayerFaintHandler.TryFaint(gameObject);

        NotifyChanged();
    }

    public void SetFaintLocked(bool locked)
    {
        _faintLocked = locked;
    }

    public void ApplyHospitalRecovery(RecomecoGameplaySettings settings)
    {
        if (settings == null)
            settings = RecomecoGameplaySettings.Instance;

        health = settings != null ? settings.hospitalWakeHealth : 35f;
        hunger = settings != null ? settings.hospitalWakeHunger : 30f;
        health = Mathf.Clamp(health, 1f, MaxHealth);
        hunger = Mathf.Clamp(hunger, 0f, MaxHunger);
        _faintLocked = false;
        NotifyChanged();
    }

    public void AddHunger(float amount)
    {
        if (amount == 0f)
            return;

        hunger = Mathf.Clamp(hunger + amount, 0f, MaxHunger);
        NotifyChanged();
    }

    public void SetNeedsForDebug(float hungerValue, float healthValue, float reputationValue)
    {
        hunger = Mathf.Clamp(hungerValue, 0f, MaxHunger);
        health = Mathf.Clamp(healthValue, 0f, MaxHealth);
        reputation = Mathf.Clamp(reputationValue, 0f, MaxReputation);
        NotifyChanged();
    }

    public void AddHealth(float amount)
    {
        if (amount == 0f)
            return;

        health = Mathf.Clamp(health + amount, 0f, MaxHealth);
        NotifyChanged();
    }

    public void AddReputation(float amount)
    {
        if (amount == 0f)
            return;

        reputation = Mathf.Clamp(reputation + amount, 0f, MaxReputation);
        NotifyChanged();
    }

    public PlayerNeedsSnapshot ExportSnapshot()
    {
        return new PlayerNeedsSnapshot
        {
            hunger = hunger,
            health = health,
            reputation = reputation,
        };
    }

    public void ImportSnapshot(PlayerNeedsSnapshot snapshot)
    {
        hunger = Mathf.Clamp(snapshot.hunger, 0f, MaxHunger);
        health = Mathf.Clamp(snapshot.health, 0f, MaxHealth);
        reputation = Mathf.Clamp(snapshot.reputation, 0f, MaxReputation);

        if (health <= 0f)
            health = 1f;

        _faintLocked = false;
        NotifyChanged();
    }

    public static void ResetToDefaults(GameObject player)
    {
        var needs = player != null ? player.GetComponent<PlayerNeeds>() : null;
        if (needs == null)
            return;

        needs.hunger = MaxHunger;
        needs.health = MaxHealth;
        needs.reputation = MaxReputation;
        needs._faintLocked = false;
        PlayerNeeds.NotifyChanged();
    }

    public static void NotifyChanged() => Changed?.Invoke();
}
