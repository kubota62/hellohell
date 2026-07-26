using Unity.Entities;
using UnityEngine;

public class HUDBridge : MonoBehaviour
{
    public static HUDBridge Instance;

    public HUDUnitCount unitCount;

    int actorCount;
    int projectileCount;
    int eliteCount;
    int level = 1;
    int experience;
    int experienceToNextLevel = 1;
    int score;
    int health = 1;
    int maxHealth = 1;
    float elapsedSeconds;
    int threatLevel = 1;
    bool isGameOver;
    bool autoAttackEnabled;
    PlayerSkillStats skillStats;
    string skillNotification;
    float skillNotificationRemaining;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (skillNotificationRemaining <= 0f)
        {
            return;
        }

        skillNotificationRemaining -= Time.deltaTime;
        if (skillNotificationRemaining <= 0f)
        {
            skillNotification = string.Empty;
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

    public void SetPlayerProgress(
        int nextLevel,
        int nextExperience,
        int nextExperienceToNextLevel,
        int nextScore,
        int nextHealth,
        int nextMaxHealth,
        float nextElapsedSeconds,
        int nextThreatLevel,
        bool nextIsGameOver,
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
        threatLevel = nextThreatLevel;
        isGameOver = nextIsGameOver;
        autoAttackEnabled = nextAutoAttackEnabled;
        skillStats = nextSkillStats;
        Refresh();
    }

    public void ShowSkillApplied(PlayerSkillKind kind, int newLevel)
    {
        skillNotification = $"LEVEL UP!  {GetSkillName(kind)} -> L{newLevel}";
        skillNotificationRemaining = 2.5f;
        Refresh();
    }

    void Refresh()
    {
        unitCount.SetCount(
            actorCount,
            projectileCount,
            eliteCount,
            level,
            experience,
            experienceToNextLevel,
            score,
            health,
            maxHealth,
            elapsedSeconds,
            threatLevel,
            isGameOver,
            autoAttackEnabled,
            skillStats,
            skillNotification);
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

            case PlayerSkillKind.Damage:
            default:
                return "DAMAGE";
        }
    }
}
