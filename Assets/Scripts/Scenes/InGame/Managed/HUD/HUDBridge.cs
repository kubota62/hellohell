using Unity.Entities;
using UnityEngine;

public class HUDBridge : MonoBehaviour
{
    public static HUDBridge Instance;

    public HUDUnitCount unitCount;

    int actorCount;
    int projectileCount;
    int eliteCount;
    int championCount;
    int championCurrentHealth;
    int championMaxHealth;
    int level = 1;
    int experience;
    int experienceToNextLevel = 1;
    int score;
    int health = 1;
    int maxHealth = 1;
    float elapsedSeconds;
    float durationSeconds = 600f;
    int threatLevel = 1;
    bool isGameOver;
    bool isVictory;
    bool autoAttackEnabled;
    PlayerSkillStats skillStats;
    bool hasUpgradeChoice;
    PlayerUpgradeChoice upgradeChoice;
    string statusNotification;
    float notificationRemaining;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (notificationRemaining <= 0f)
        {
            return;
        }

        notificationRemaining -= Time.deltaTime;
        if (notificationRemaining <= 0f)
        {
            statusNotification = string.Empty;
            Refresh();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetActorCount(int count)
    {
        actorCount = count;
        Refresh();
    }

    public void SetProjectileCount(int count)
    {
        projectileCount = count;
        Refresh();
    }

    public void SetEliteCount(int count)
    {
        eliteCount = count;
        Refresh();
    }

    public void SetChampionState(
        int count,
        int currentHealth,
        int maximumHealth)
    {
        if (count > championCount)
        {
            statusNotification = "CHAMPION APPROACHES!";
            notificationRemaining = 4f;
        }

        championCount = count;
        championCurrentHealth = currentHealth;
        championMaxHealth = maximumHealth;
        Refresh();
    }

    public void SetPlayerProgress(
        int nextLevel,
        int nextExperience,
        int nextExperienceToNextLevel,
        int nextScore,
        int nextHealth,
        int nextMaxHealth,
        float nextElapsedSeconds,
        float nextDurationSeconds,
        int nextThreatLevel,
        bool nextIsGameOver,
        bool nextIsVictory,
        bool nextAutoAttackEnabled,
        PlayerSkillStats nextSkillStats)
    {
        level = nextLevel;
        experience = nextExperience;
        experienceToNextLevel = nextExperienceToNextLevel;
        score = nextScore;
        health = nextHealth;
        maxHealth = nextMaxHealth;
        elapsedSeconds = nextElapsedSeconds;
        durationSeconds = nextDurationSeconds;
        if (nextThreatLevel > threatLevel)
        {
            statusNotification = $"THREAT RISING!  LEVEL {nextThreatLevel}";
            notificationRemaining = 3f;
        }

        threatLevel = nextThreatLevel;
        isGameOver = nextIsGameOver;
        isVictory = nextIsVictory;
        autoAttackEnabled = nextAutoAttackEnabled;
        skillStats = nextSkillStats;
        Refresh();
    }

    public void ShowSkillApplied(PlayerSkillKind kind, int newLevel)
    {
        statusNotification = $"LEVEL UP!  {GetSkillName(kind)} -> L{newLevel}";
        notificationRemaining = 2.5f;
        Refresh();
    }

    public void ShowChampionDefeated(int experience, int scoreReward)
    {
        statusNotification =
            $"CHAMPION DEFEATED!  +{experience} XP  +{scoreReward} SCORE";
        notificationRemaining = 4f;
        Refresh();
    }

    public void ShowPlayerHealed(int amount)
    {
        statusNotification = $"RESTORATIVE SHARD  +{Mathf.Max(0, amount)} HP";
        notificationRemaining = 2.5f;
        Refresh();
    }

    public void ShowPlayerRevived(int restoredHealth, int chargesRemaining)
    {
        statusNotification =
            $"SECOND WIND!  {Mathf.Max(1, restoredHealth)} HP  " +
            $"{Mathf.Max(0, chargesRemaining)} CHARGES LEFT";
        notificationRemaining = 4f;
        Refresh();
    }

    public void ShowHordeSurge(int wave, int enemyCount, int currentThreat)
    {
        statusNotification =
            $"HORDE SURGE {Mathf.Max(1, wave)}!  " +
            $"{Mathf.Max(0, enemyCount)} ENEMIES  THREAT {Mathf.Max(1, currentThreat)}";
        notificationRemaining = 4f;
        Refresh();
    }

    public void SetUpgradeChoice(
        bool isActive,
        PlayerUpgradeChoice nextUpgradeChoice)
    {
        hasUpgradeChoice = isActive;
        upgradeChoice = nextUpgradeChoice;
        Refresh();
    }

    void Refresh()
    {
        unitCount.SetCount(
            actorCount,
            projectileCount,
            eliteCount,
            championCount,
            championCurrentHealth,
            championMaxHealth,
            level,
            experience,
            experienceToNextLevel,
            score,
            health,
            maxHealth,
            elapsedSeconds,
            durationSeconds,
            threatLevel,
            isGameOver,
            isVictory,
            autoAttackEnabled,
            skillStats,
            hasUpgradeChoice,
            upgradeChoice,
            statusNotification);
    }

    static string GetSkillName(PlayerSkillKind kind)
    {
        switch (kind)
        {
            case PlayerSkillKind.AttackSpeed:
                return "HASTE";

            case PlayerSkillKind.MoveSpeed:
                return "MOVE";

            case PlayerSkillKind.Area:
                return "AREA";

            case PlayerSkillKind.Regeneration:
                return "REGEN";

            case PlayerSkillKind.MaxHealth:
                return "FORTITUDE";

            case PlayerSkillKind.PickupRange:
                return "MAGNET";

            case PlayerSkillKind.MeleeArc:
                return "BLADE";

            case PlayerSkillKind.RapidBolt:
                return "BOLT";

            case PlayerSkillKind.PiercingLance:
                return "LANCE";

            case PlayerSkillKind.ExplosiveOrb:
                return "ORB";

            case PlayerSkillKind.CriticalChance:
                return "CRIT";

            case PlayerSkillKind.CriticalDamage:
                return "FEROCITY";

            case PlayerSkillKind.Armor:
                return "ARMOR";

            case PlayerSkillKind.Multistrike:
                return "MULTISTRIKE";

            case PlayerSkillKind.Executioner:
                return "EXECUTIONER";

            case PlayerSkillKind.SecondWind:
                return "SECOND WIND";

            case PlayerSkillKind.Wisdom:
                return "WISDOM";

            case PlayerSkillKind.Longshot:
                return "LONGSHOT";

            case PlayerSkillKind.BossHunter:
                return "BOSS HUNTER";

            case PlayerSkillKind.Penetration:
                return "PENETRATION";

            case PlayerSkillKind.Damage:
            default:
                return "DAMAGE";
        }
    }
}
