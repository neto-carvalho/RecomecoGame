using System;
using Controller;
using UnityEngine;

public class PlayerNeeds : MonoBehaviour
{
    public const float MaxHunger = 100f;
    public const float MaxHealth = 100f;
    public const float MaxReputation = 100f;
    public const float MaxProtection = 100f;
    public const float MaxIllness = 100f;

    public static event Action Changed;
    public static PlayerNeeds Instance { get; private set; }

    static bool s_pendingPrecariousStart;

    [SerializeField] float hunger = MaxHunger;
    [SerializeField] float health = MaxHealth;
    [SerializeField] float reputation = MaxReputation;
    [SerializeField] float protection = MaxProtection;
    [SerializeField] float illness = 0f;

    bool _faintLocked;
    CharacterMover _mover;

    public float Hunger => hunger;
    public float Health => health;
    public float Reputation => reputation;
    public float Protection => protection;
    public float Illness => illness;
    public bool IsFaintLocked => _faintLocked;
    public bool IsSick => illness >= 25f;

    public static void RequestPrecariousStartForNewGame()
    {
        s_pendingPrecariousStart = true;
    }

    void Awake()
    {
        Instance = this;
        _mover = GetComponent<CharacterMover>();

        if (s_pendingPrecariousStart)
        {
            s_pendingPrecariousStart = false;
            ApplyPrecariousNewGameStart(RecomecoGameplaySettings.Instance);
        }
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
        drainPerSecond *= GetHungerDrainMultiplier(settings);
        if (_mover != null && _mover.IsRun)
            drainPerSecond *= settings.hungerDrainRunMultiplier;

        hunger = Mathf.Max(0f, hunger - drainPerSecond * Time.deltaTime);

        TickExposureIllness(settings);

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

    public void SetShelterForDebug(float protectionValue, float illnessValue)
    {
        protection = Mathf.Clamp(protectionValue, 0f, MaxProtection);
        illness = Mathf.Clamp(illnessValue, 0f, MaxIllness);
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

    public void ApplyPrecariousNewGameStart(RecomecoGameplaySettings settings)
    {
        hunger = MaxHunger * 0.72f;
        health = MaxHealth * 0.88f;
        reputation = MaxReputation * 0.85f;
        protection = settings != null ? settings.newGameProtection : 42f;
        illness = 0f;
        protection = Mathf.Clamp(protection, 0f, MaxProtection);
        _faintLocked = false;
        NotifyChanged();
    }

    public void ApplyPrecariousSleep(RecomecoGameplaySettings settings)
    {
        if (settings == null)
            settings = RecomecoGameplaySettings.Instance;

        var hungerGain = settings != null ? settings.precariousSleepHungerRestore : 18f;
        var healthGain = settings != null ? settings.precariousSleepHealthRestore : 8f;
        var protLoss = settings != null ? settings.precariousSleepProtectionLoss : 12f;
        var illGain = settings != null ? settings.precariousSleepIllnessGain : 22f;

        hunger = Mathf.Clamp(hunger + hungerGain, 0f, MaxHunger);
        health = Mathf.Clamp(health + healthGain, 0f, MaxHealth);
        protection = Mathf.Clamp(protection - protLoss, 0f, MaxProtection);
        illness = Mathf.Clamp(illness + illGain, 0f, MaxIllness);
        NotifyChanged();
    }

    public void ApplySafeSleep(RecomecoGameplaySettings settings)
    {
        if (settings == null)
            settings = RecomecoGameplaySettings.Instance;

        var hungerGain = settings != null ? settings.safeSleepHungerRestore : 35f;
        var healthGain = settings != null ? settings.safeSleepHealthRestore : 28f;
        var protGain = settings != null ? settings.safeSleepProtectionGain : 35f;
        var illReduce = settings != null ? settings.safeSleepIllnessReduce : 40f;

        hunger = Mathf.Clamp(hunger + hungerGain, 0f, MaxHunger);
        health = Mathf.Clamp(health + healthGain, 0f, MaxHealth);
        protection = Mathf.Clamp(protection + protGain, 0f, MaxProtection);
        illness = Mathf.Clamp(illness - illReduce, 0f, MaxIllness);
        NotifyChanged();
    }

    float GetHungerDrainMultiplier(RecomecoGameplaySettings settings)
    {
        var mult = 1f;
        var protFactor = 1f - protection / MaxProtection;
        mult += protFactor * 0.55f;
        mult += (illness / MaxIllness) * 0.45f;
        return mult;
    }

    void TickExposureIllness(RecomecoGameplaySettings settings)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != RecomecoSceneNames.Cidade)
            return;

        if (PlayerHousingState.Owns(PlayerHousingState.CasaElegante))
            return;

        if (protection >= settings.exposedProtectionThreshold)
            return;

        var gainPerSecond = settings.illnessGainPerMinuteWhenExposed / 60f;
        if (gainPerSecond <= 0f)
            return;

        illness = Mathf.Clamp(illness + gainPerSecond * Time.deltaTime, 0f, MaxIllness);
    }

    public PlayerNeedsSnapshot ExportSnapshot()
    {
        return new PlayerNeedsSnapshot
        {
            hunger = hunger,
            health = health,
            reputation = reputation,
            protection = protection,
            illness = illness,
        };
    }

    public void ImportSnapshot(PlayerNeedsSnapshot snapshot)
    {
        hunger = Mathf.Clamp(snapshot.hunger, 0f, MaxHunger);
        health = Mathf.Clamp(snapshot.health, 0f, MaxHealth);
        reputation = Mathf.Clamp(snapshot.reputation, 0f, MaxReputation);
        protection = Mathf.Clamp(snapshot.protection, 0f, MaxProtection);
        illness = Mathf.Clamp(snapshot.illness, 0f, MaxIllness);

        if (protection <= 0.01f && illness <= 0.01f && health > 1f)
            protection = MaxProtection * 0.7f;

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
        needs.protection = MaxProtection;
        needs.illness = 0f;
        needs._faintLocked = false;
        PlayerNeeds.NotifyChanged();
    }

    public static void NotifyChanged() => Changed?.Invoke();
}
