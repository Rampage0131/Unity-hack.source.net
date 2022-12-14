using UnityEngine;
using UnityEngine.Networking;
using UnityStandardAssets.CrossPlatformInput;
using OmiyaGames;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerSetup))]
[RequireComponent(typeof(CharacterController))]
public class PlayerStatus : NetworkBehaviour
{
    public const int MaxHealth = 4;
    public const float InvincibilityDuration = 1f;

    public enum State
    {
        ForcedStill,
        Alive,
        Invincible,
        Dead,
        Victory
    }

    [SerializeField]
    GameObject healthIndicator;

    [Header("Reflection")]
    [SerializeField]
    float reflectDuration = 1f;
    [SerializeField]
    float cooldownDuration = 0.5f;
    [SerializeField]
    Collider[] reflectorColliders;

    [Header("Sound")]
    [SerializeField]
    SoundEffect reflectorSound;
    [SerializeField]
    SoundEffect hurtSound;

    [SyncVar(hook = "OnPlayerHealthSynced")]
    int health = MaxHealth;
    [SyncVar(hook = "OnPlayerStateSynced")]
    int currentState = (int)State.Alive;    // FIXME: change this to forcedstill at some point
    [SyncVar]
    double timeReflectorIsOn = -1;
    [SyncVar]
    double timeLastInvincible = -1;

    PlayerSetup playerSetup;
    bool lastReflectorState = false;
    int displayHealth = MaxHealth;
    readonly GameObject[] healthIndicators = new GameObject[MaxHealth];

    #region Properties
    public int Health
    {
        get
        {
            return displayHealth;
        }
        set
        {
            int setValueTo = Mathf.Clamp(value, 0, MaxHealth);
            if (health != setValueTo)
            {
                if (setValueTo < health)
                {
                    hurtSound.Play();
                    if (setValueTo > 0)
                    {
                        CmdSetHealthInvincibility(setValueTo, Network.time);
                    }
                    else
                    {
                        CmdDie();
                    }
                }
                else
                {
                    CmdSetHealth(setValueTo);
                }
            }
        }
    }

    public State CurrentState
    {
        get
        {
            if((timeLastInvincible > 0) && (Network.time < (timeLastInvincible + InvincibilityDuration)))
            {
                return State.Invincible;
            }
            else
            {
                return (State)currentState;
            }
        }
        private set
        {
            int setValueTo = (int)value;
            if (currentState != setValueTo)
            {
                CmdSetState(setValueTo);
            }
        }
    }

    public bool IsReflectEnabled
    {
        get
        {
            bool returnFlag = false;
            if(timeReflectorIsOn > 0)
            {
                returnFlag = ReflectorCheck(timeReflectorIsOn);
            }
            return returnFlag;
        }
    }

    public bool ReflectorCheck(double time)
    {
        return (Network.time < (time + reflectDuration));
    }
    #endregion

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // Grab components
        playerSetup = GetComponent<PlayerSetup>();

        // Setup HUD
        SetupHud();

        // Setup shields
        playerSetup.NameChanged += PlayerSetup_NameChanged;
        PlayerSetup_NameChanged(playerSetup, name);

        // Reset variables
        Health = MaxHealth;
        CurrentState = State.Alive;
    }

    private void PlayerSetup_NameChanged(PlayerSetup arg1, string arg2)
    {
        // Set all the reflectors to have the same name!
        foreach(Collider collider in reflectorColliders)
        {
            collider.name = arg2;
        }
    }

    void Update()
    {
        UpdateInvincibleState();
        UpdateReflection();
        //UpdateWin();
    }

    #region Commands
    [Command]
    void CmdSetHealth(int newHealth)
    {
        health = newHealth;
    }

    [Command]
    void CmdSetHealthState(int newHealth, State newState)
    {
        health = newHealth;
        currentState = (int)newState;
    }

    [Command]
    void CmdDie()
    {
        foreach (KeyValuePair<string, PlayerSetup> setup in PlayerSetup.AllIdentifiedPlayers)
        {
            if (setup.Key == name)
            {
                health = 0;
                currentState = (int)State.Dead;
            }
            else
            {
                setup.Value.Status.currentState = (int)State.Victory;
            }
        }
    }

    [Command]
    void CmdSetHealthInvincibility(int newHealth, double time)
    {
        health = newHealth;
        timeLastInvincible = time;
    }

    [Command]
    void CmdSetState(int newState)
    {
        currentState = newState;
    }

    [Command]
    void CmdSetReflect(double time)
    {
        timeReflectorIsOn = time;
    }
    #endregion

    #region Helper Methods
    [Client]
    private void OnPlayerHealthSynced(int latestHealth)
    {
        displayHealth = latestHealth;
        if (isLocalPlayer == true)
        {
            for (int i = 0; i < MaxHealth; ++i)
            {
                healthIndicators[i].SetActive(i < latestHealth);
            }
        }
    }

    [Client]
    private void OnPlayerStateSynced(int latestState)
    {
        if (isLocalPlayer == true)
        {
            if (latestState == (int)State.Dead)
            {
                // Indicate death
                //Debug.Log("PlayerStatus: Death detected");
                Singleton.Get<MenuManager>().Hide<PauseMenu>();
                Singleton.Get<MenuManager>().Show<LoseMenu>();

                //Debug.Log("Menu shown");
                playerSetup.CmdSetLosingPlayer();
            }
            else if (latestState == (int)State.Victory)
            {
                // Indicate death
                //Debug.Log("PlayerStatus: Victory detected");
                Singleton.Get<MenuManager>().Hide<PauseMenu>();
                Singleton.Get<MenuManager>().Show<WinMenu>();

                //Debug.Log("Menu shown");
            }
        }
    }

    [Client]
    private void UpdateInvincibleState()
    {
        // TODO: take out this conditional if it doesn't work
        if (isLocalPlayer == true)
        {
            playerSetup.hudAnimations.SetBool(PlayerSetup.InvincibleBool, (CurrentState == State.Invincible));
        }
    }

    private void UpdateReflection()
    {
        // TODO: take out this conditional if it doesn't work
        if (isLocalPlayer == true)
        {
            // Turn on or off the reflector
            if((lastReflectorState == false) && (IsReflectEnabled == true))
            {
                reflectorSound.Play();
            }
            playerSetup.hudAnimations.SetBool(PlayerSetup.ReflectBool, IsReflectEnabled);
            lastReflectorState = IsReflectEnabled;
        }

        // Check if we are allowed to bring up the reflector
        if (isLocalPlayer == true)
        {
            bool reflect = CrossPlatformInputManager.GetButtonDown("Reflect");
            playerSetup.PressControls(PlayerSetup.ActiveControls.Reflect, reflect);
            if ((CurrentState != State.Dead) && (IsReflectEnabled == false) && (Network.time > (timeReflectorIsOn + cooldownDuration + reflectDuration)))
            {
                // Check if the player pressed reflection
                if ((reflect == true) && (playerSetup.IsControlActive(PlayerSetup.ActiveControls.Reflect) == true))
                {
                    CmdSetReflect(Network.time);
                }
            }
        }
    }

    private void SetupHud()
    {
        healthIndicators[0] = healthIndicator;
        GameObject newIndicator = null;
        for (int i = 1; i < MaxHealth; ++i)
        {
            newIndicator = Instantiate<GameObject>(healthIndicator);
            newIndicator.transform.SetParent(healthIndicator.transform.parent, false);
            newIndicator.transform.SetAsLastSibling();
            newIndicator.transform.localScale = Vector3.one;
            healthIndicators[i] = newIndicator;
        }
    }
    #endregion
}
