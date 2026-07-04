using Unity.Entities;
using UnityEngine;

public class HUDBridge : MonoBehaviour
{
    public static HUDBridge Instance;

    public HUDUnitCount unitCount;

    int actorCount;
    int projectileCount;
    int level = 1;
    int experience;
    int experienceToNextLevel = 1;
    int score;

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

    public void SetPlayerProgress(int nextLevel, int nextExperience, int nextExperienceToNextLevel, int nextScore)
    {
        level = nextLevel;
        experience = nextExperience;
        experienceToNextLevel = nextExperienceToNextLevel;
        score = nextScore;
        Refresh();
    }

    void Refresh()
    {
        unitCount.SetCount(actorCount, projectileCount, level, experience, experienceToNextLevel, score);
    }
}
