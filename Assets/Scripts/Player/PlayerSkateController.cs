using System.Collections;
using Controller;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Skate desbloqueável na cidade. Só ativa locomoção real com animator de skate configurado.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSkateLocomotion))]
public class PlayerSkateController : MonoBehaviour
{
    CharacterMover _mover;
    PlayerSkateLocomotion _skateLocomotion;
    GameObject _visualRoot;
    bool _riding;

    public bool IsRiding => _riding;

    void Awake()
    {
        _mover = GetComponent<CharacterMover>();
        _skateLocomotion = GetComponent<PlayerSkateLocomotion>();
    }

    void OnEnable()
    {
        PlayerUnlocks.Changed += RefreshFromUnlocks;
    }

    void OnDisable()
    {
        PlayerUnlocks.Changed -= RefreshFromUnlocks;
        if (_riding)
            SetRiding(false, silent: true);
    }

    void Update()
    {
        if (!PlayerUnlocks.HasSkate || _mover == null)
            return;

        if (PlayerNeeds.Instance != null && PlayerNeeds.Instance.IsFaintLocked)
        {
            if (_riding)
                SetRiding(false, silent: true);
            return;
        }

        if (!AllowsSkateInActiveScene())
        {
            if (_riding)
                SetRiding(false, silent: true);
            return;
        }

        var settings = RecomecoGameplaySettings.Instance;
        var key = settings != null ? settings.skateToggleKey : KeyCode.B;
        if (Input.GetKeyDown(key))
            ToggleRiding();
    }

    void OnDestroy()
    {
        if (_visualRoot != null)
            Destroy(_visualRoot);
    }

    public static bool IsSkateLocomotionReady()
    {
        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && !settings.requireSkateAnimatorForRiding)
            return true;

        return PlayerAnimatorSetup.HasSkateAnimatorConfigured();
    }

    public void RefreshFromUnlocks()
    {
        EnsureVisualExists();
        if (_visualRoot != null)
            _visualRoot.SetActive(_riding && PlayerUnlocks.HasSkate);

        if (!PlayerUnlocks.HasSkate && _riding)
            SetRiding(false, silent: true);
        else if (_riding)
            ApplySkateLocomotion();
    }

    public void ToggleRiding()
    {
        if (!PlayerUnlocks.HasSkate)
            return;

        SetRiding(!_riding);
    }

    public void SetRiding(bool riding, bool silent = false)
    {
        if (_riding == riding)
            return;

        if (riding && !IsSkateLocomotionReady())
        {
            if (!silent)
            {
                InteractionUI.ShowMessage(
                    "Skate no inventário, mas faltam animações. " +
                    "Recomeco → Player → Skate — importar animações (Mixamo).");
            }

            return;
        }

        _riding = riding;
        EnsureVisualExists();
        if (_visualRoot != null)
        {
            if (_riding)
                PlayerSkateVisualFactory.ApplyFeetOffset(_visualRoot.transform, transform, RecomecoGameplaySettings.Instance);

            _visualRoot.SetActive(_riding);
        }

        if (_riding)
            ApplySkateLocomotion();
        else
            RestoreBaseLocomotion();

        if (_riding)
        {
            var snap = GetComponent<CharacterGroundSnap>();
            if (snap != null)
                snap.SnapNow();
        }

        if (_riding && _skateLocomotion != null)
            _skateLocomotion.PrepareMount();

        if (_riding && _visualRoot != null)
            StartCoroutine(RealignSkateVisualNextFrame());

        FootstepAudio.SetSuppressedForPlayer(gameObject, _riding);

        var skateAudio = GetComponent<PlayerSkateAudio>();
        if (skateAudio == null)
            skateAudio = gameObject.AddComponent<PlayerSkateAudio>();
        skateAudio.ResetState();
    }

    void ApplySkateLocomotion()
    {
        var settings = RecomecoGameplaySettings.Instance;
        if (settings == null || _mover == null)
            return;

        if (IsSkateLocomotionReady())
        {
            PlayerAnimatorSetup.ApplySkateAnimator(gameObject);
            _mover.SetSuppressLocomotionAnimation(true);
            if (_skateLocomotion != null)
                _skateLocomotion.enabled = true;
        }

        var factor = settings.GetScaleFactor(transform);
        _mover.SetLocomotionSpeeds(
            settings.skateWalkSpeed * factor,
            settings.skateRunSpeed * factor,
            settings.rotateSpeed);
    }

    void RestoreBaseLocomotion()
    {
        if (_skateLocomotion != null)
            _skateLocomotion.enabled = false;

        if (_mover != null)
            _mover.SetSuppressLocomotionAnimation(false);

        PlayerAnimatorSetup.RestoreFootAnimator(gameObject);

        var settings = RecomecoGameplaySettings.Instance;
        if (settings == null || _mover == null)
            return;

        settings.ApplyToMover(_mover, transform);
    }

    void EnsureVisualExists()
    {
        if (_visualRoot != null)
            return;

        var settings = RecomecoGameplaySettings.Instance;
        var prefab = settings != null ? settings.skateVisualPrefab : null;

        if (prefab != null)
        {
            _visualRoot = Instantiate(prefab, transform);
            _visualRoot.name = "Skate_Visual";
            PlayerSkateVisualFactory.StripColliders(_visualRoot);
            PlayerSkateVisualFactory.FixUrpMaterials(_visualRoot);
        }
        else
            _visualRoot = PlayerSkateVisualFactory.CreateUnderPlayer(transform, settings);

        if (_visualRoot == null)
            return;

        PlayerSkateVisualFactory.ApplyFeetOffset(_visualRoot.transform, transform, settings);
        _visualRoot.SetActive(false);
    }

    IEnumerator RealignSkateVisualNextFrame()
    {
        yield return null;
        if (!_riding || _visualRoot == null)
            yield break;

        PlayerSkateVisualFactory.ApplyFeetOffset(
            _visualRoot.transform,
            transform,
            RecomecoGameplaySettings.Instance);
    }

    static bool AllowsSkateInActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || RecomecoSceneNames.IsMenuScene(scene))
            return false;

        var name = scene.name;
        if (name == RecomecoSceneNames.InteriorCasaElegante)
            return false;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && settings.skateOnlyInCity)
            return name == RecomecoSceneNames.Cidade;

        return name == RecomecoSceneNames.Cidade || name == RecomecoSceneNames.FerroVelho;
    }
}