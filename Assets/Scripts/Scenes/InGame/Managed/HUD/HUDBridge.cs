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
    int finalBossCount;
    int finalBossCurrentHealth;
    int finalBossMaxHealth;
    int level = 1;
    int experience;
    int experienceToNextLevel = 1;
    int score;
    int health = 1;
    int maxHealth = 1;
    float elapsedSeconds;
    float durationSeconds = 600f;
    int threatLevel = 1;
    int enemiesDefeated;
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
        int maximumHealth,
        int nextFinalBossCount,
        int nextFinalBossCurrentHealth,
        int nextFinalBossMaxHealth)
    {
        if (count > championCount)
        {
            statusNotification = "チャンピオン接近！";
            notificationRemaining = 4f;
        }

        championCount = count;
        championCurrentHealth = currentHealth;
        championMaxHealth = maximumHealth;
        finalBossCount = nextFinalBossCount;
        finalBossCurrentHealth = nextFinalBossCurrentHealth;
        finalBossMaxHealth = nextFinalBossMaxHealth;
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
        int nextEnemiesDefeated,
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
            statusNotification = $"脅威度上昇！  レベル {nextThreatLevel}";
            notificationRemaining = 3f;
        }

        threatLevel = nextThreatLevel;
        enemiesDefeated = Mathf.Max(0, nextEnemiesDefeated);
        isGameOver = nextIsGameOver;
        isVictory = nextIsVictory;
        autoAttackEnabled = nextAutoAttackEnabled;
        skillStats = nextSkillStats;
        Refresh();
    }

    public void ShowSkillApplied(PlayerSkillKind kind, int newLevel)
    {
        statusNotification = $"レベルアップ！  {GetSkillName(kind)} → Lv{newLevel}";
        notificationRemaining = 2.5f;
        Refresh();
    }

    public void ShowChampionDefeated(int experience, int scoreReward)
    {
        statusNotification =
            $"チャンピオン撃破！  経験値 +{experience}  スコア +{scoreReward}";
        notificationRemaining = 4f;
        Refresh();
    }

    public void ShowPlayerHealed(int amount)
    {
        statusNotification = $"回復の欠片  HP +{Mathf.Max(0, amount)}";
        notificationRemaining = 2.5f;
        Refresh();
    }

    public void ShowPlayerRevived(int restoredHealth, int chargesRemaining)
    {
        statusNotification =
            $"起死回生！  HP {Mathf.Max(1, restoredHealth)}  " +
            $"残り{Mathf.Max(0, chargesRemaining)}回";
        notificationRemaining = 4f;
        Refresh();
    }

    public void ShowHordeSurge(int wave, int enemyCount, int currentThreat)
    {
        statusNotification =
            $"大群襲来 第{Mathf.Max(1, wave)}波！  " +
            $"敵 {Mathf.Max(0, enemyCount)}体  脅威度 {Mathf.Max(1, currentThreat)}";
        notificationRemaining = 4f;
        Refresh();
    }

    public void ShowFinalBossSpawned(int currentThreat)
    {
        statusNotification =
            $"最終ボス接近！  脅威度 {Mathf.Max(1, currentThreat)}";
        notificationRemaining = 6f;
        Refresh();
    }

    public void ShowFinalBossEnraged()
    {
        statusNotification = "最終ボス激昂！  第2形態";
        notificationRemaining = 6f;
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
            finalBossCount,
            finalBossCurrentHealth,
            finalBossMaxHealth,
            level,
            experience,
            experienceToNextLevel,
            score,
            health,
            maxHealth,
            elapsedSeconds,
            durationSeconds,
            threatLevel,
            enemiesDefeated,
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
                return "早業";

            case PlayerSkillKind.MoveSpeed:
                return "俊足";

            case PlayerSkillKind.Area:
                return "広域化";

            case PlayerSkillKind.Regeneration:
                return "活力";

            case PlayerSkillKind.MaxHealth:
                return "強靭";

            case PlayerSkillKind.PickupRange:
                return "磁力";

            case PlayerSkillKind.MeleeArc:
                return "剣術";

            case PlayerSkillKind.RapidBolt:
                return "速射弾";

            case PlayerSkillKind.PiercingLance:
                return "貫通槍";

            case PlayerSkillKind.ExplosiveOrb:
                return "爆裂球";

            case PlayerSkillKind.CriticalChance:
                return "慧眼";

            case PlayerSkillKind.CriticalDamage:
                return "獰猛";

            case PlayerSkillKind.Armor:
                return "鉄壁";

            case PlayerSkillKind.Multistrike:
                return "連撃";

            case PlayerSkillKind.Executioner:
                return "処刑人";

            case PlayerSkillKind.SecondWind:
                return "起死回生";

            case PlayerSkillKind.Wisdom:
                return "英知";

            case PlayerSkillKind.Longshot:
                return "遠射";

            case PlayerSkillKind.BossHunter:
                return "強敵狩り";

            case PlayerSkillKind.Penetration:
                return "貫通";

            case PlayerSkillKind.Fortune:
                return "幸運";

            case PlayerSkillKind.Berserker:
                return "狂戦士";

            case PlayerSkillKind.Damage:
            default:
                return "剛力";
        }
    }
}
