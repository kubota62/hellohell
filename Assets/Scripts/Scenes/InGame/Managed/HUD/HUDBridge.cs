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

    void Awake()
    {
        Instance = this;
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
        bool nextAutoAttackEnabled)
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
            autoAttackEnabled);
    }
}
