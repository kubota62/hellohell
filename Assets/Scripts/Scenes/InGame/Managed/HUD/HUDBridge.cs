using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class HUDBridge : MonoBehaviour
{
    public static HUDBridge Instance;

    public HUDUnitCount unitCount;

    int actorCount;
    int projectileCount;
    int eliteCount;
    int swiftEliteCount;
    int juggernautEliteCount;
    int eliteCurrentHealth;
    int eliteMaxHealth;
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
    int killStreak;
    int bestKillStreak;
    int killStreakScoreMultiplier = 1;
    float killStreakRemainingSeconds;
    long totalDamageDealt;
    float currentDps;
    float peakDps;
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
        SetEliteCount(count, 0, 0);
    }

    public void SetEliteCount(
        int count,
        int nextSwiftEliteCount,
        int nextJuggernautEliteCount)
    {
        eliteCount = Mathf.Max(0, count);
        swiftEliteCount = Mathf.Max(0, nextSwiftEliteCount);
        juggernautEliteCount = Mathf.Max(
            0,
            nextJuggernautEliteCount);
        Refresh();
    }

    public void SetThreatIndicator(
        bool isActive,
        int threatRank,
        float3 worldPosition)
    {
        unitCount.SetThreatIndicator(
            isActive,
            threatRank,
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                worldPosition.z));
    }

    public void SetUpcomingThreat(
        UpcomingThreatEventKind kind,
        float remainingSeconds)
    {
        unitCount.SetUpcomingThreat(kind, remainingSeconds);
    }

    public void SetChampionState(
        int nextEliteCurrentHealth,
        int nextEliteMaxHealth,
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

        eliteCurrentHealth = Mathf.Max(0, nextEliteCurrentHealth);
        eliteMaxHealth = Mathf.Max(0, nextEliteMaxHealth);
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
        int nextKillStreak,
        int nextBestKillStreak,
        int nextKillStreakScoreMultiplier,
        float nextKillStreakRemainingSeconds,
        long nextTotalDamageDealt,
        float nextCurrentDps,
        float nextPeakDps,
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
        killStreak = Mathf.Max(0, nextKillStreak);
        bestKillStreak = Mathf.Max(0, nextBestKillStreak);
        killStreakScoreMultiplier = Mathf.Clamp(
            nextKillStreakScoreMultiplier,
            1,
            3);
        killStreakRemainingSeconds = Mathf.Max(
            0f,
            nextKillStreakRemainingSeconds);
        totalDamageDealt = nextTotalDamageDealt < 0
            ? 0
            : nextTotalDamageDealt;
        currentDps = Mathf.Max(0f, nextCurrentDps);
        peakDps = Mathf.Max(0f, nextPeakDps);
        isGameOver = nextIsGameOver;
        isVictory = nextIsVictory;
        autoAttackEnabled = nextAutoAttackEnabled;
        skillStats = nextSkillStats;
        Refresh();
    }

    public void ShowSkillApplied(PlayerSkillKind kind, int newLevel)
    {
        var evolutionName =
            PlayerWeaponEvolutionUtility.GetEvolutionName(kind);
        var evolved =
            newLevel >= PlayerWeaponEvolutionUtility.EvolutionLevel &&
            !string.IsNullOrEmpty(evolutionName);
        statusNotification = evolved
            ? $"武器進化！  {evolutionName}"
            : $"レベルアップ！  {GetSkillName(kind)} → Lv{newLevel}";
        notificationRemaining = evolved ? 4f : 2.5f;
        if (evolved)
        {
            unitCount.ShowThreatAnnouncement(
                $"武器進化！\n{evolutionName}",
                3.5f,
                new Color(1f, 0.72f, 0.12f, 1f));
        }
        Refresh();
    }

    public void ShowChampionDefeated(int experience, int scoreReward)
    {
        statusNotification =
            $"チャンピオン撃破！  経験値 +{experience}  スコア +{scoreReward}";
        notificationRemaining = 4f;
        Refresh();
    }

    public void ShowSlaughterMilestone(
        int milestone,
        int experienceReward,
        int scoreReward)
    {
        var safeMilestone = Mathf.Max(0, milestone);
        var safeExperience = Mathf.Max(0, experienceReward);
        var safeScore = Mathf.Max(0, scoreReward);
        statusNotification =
            $"撃破節目 {safeMilestone}体！  " +
            $"経験値 +{safeExperience}  スコア +{safeScore}  " +
            "経験値を一斉回収！";
        notificationRemaining = 5f;
        unitCount.ShowThreatAnnouncement(
            $"{safeMilestone}体撃破！\n討伐報酬＋経験値一斉回収",
            4.2f,
            new Color(1f, 0.68f, 0.08f, 1f));
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
        unitCount.ShowThreatAnnouncement(
            $"大群襲来 第{Mathf.Max(1, wave)}波！",
            2.2f,
            new Color(1f, 0.18f, 0.06f, 1f));
        Refresh();
    }

    public void ShowHordeWarning(
        int wave,
        float secondsUntilArrival,
        int currentThreat)
    {
        var seconds = Mathf.Max(
            1,
            Mathf.CeilToInt(secondsUntilArrival));
        var message =
            $"大群接近 第{Mathf.Max(1, wave)}波  " +
            $"{seconds}秒後 襲来";
        statusNotification =
            $"{message}  脅威度 {Mathf.Max(1, currentThreat)}";
        notificationRemaining = Mathf.Max(2.5f, secondsUntilArrival);
        unitCount.ShowThreatAnnouncement(
            message,
            Mathf.Max(2.5f, secondsUntilArrival),
            new Color(1f, 0.56f, 0.08f, 1f));
        Refresh();
    }

    public void ShowChampionWarning(
        int wave,
        float secondsUntilArrival,
        int currentThreat,
        bool includesHorde)
    {
        var seconds = Mathf.Max(
            1,
            Mathf.CeilToInt(secondsUntilArrival));
        var threatName = includesHorde
            ? "大群＋チャンピオン"
            : "チャンピオン";
        var message =
            $"{threatName}接近 第{Mathf.Max(1, wave)}波  " +
            $"{seconds}秒後";
        statusNotification =
            $"{message}  脅威度 {Mathf.Max(1, currentThreat)}";
        notificationRemaining = Mathf.Max(2.5f, secondsUntilArrival);
        unitCount.ShowThreatAnnouncement(
            message,
            Mathf.Max(2.5f, secondsUntilArrival),
            includesHorde
                ? new Color(1f, 0.28f, 0.72f, 1f)
                : new Color(0.76f, 0.24f, 1f, 1f));
        Refresh();
    }

    public void ShowFinalBossSpawned(int currentThreat)
    {
        statusNotification =
            $"最終ボス接近！  脅威度 {Mathf.Max(1, currentThreat)}";
        notificationRemaining = 6f;
        unitCount.ShowThreatAnnouncement(
            "最終ボス接近！",
            4f,
            new Color(1f, 0.08f, 0.14f, 1f));
        Refresh();
    }

    public void ShowFinalBossEnraged()
    {
        statusNotification = "最終ボス激昂！  第2形態";
        notificationRemaining = 6f;
        unitCount.ShowThreatAnnouncement(
            "最終ボス激昂！  第2形態",
            4f,
            new Color(0.88f, 0.12f, 1f, 1f));
        Refresh();
    }

    public void ShowChampionEnraged()
    {
        statusNotification = "チャンピオン激昂！ 攻撃・移動強化";
        notificationRemaining = 4f;
        unitCount.ShowThreatAnnouncement(
            "チャンピオン激昂！",
            3.2f,
            new Color(1f, 0.08f, 0.38f, 1f));
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
            swiftEliteCount,
            juggernautEliteCount,
            eliteCurrentHealth,
            eliteMaxHealth,
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
            killStreak,
            bestKillStreak,
            killStreakScoreMultiplier,
            killStreakRemainingSeconds,
            totalDamageDealt,
            currentDps,
            peakDps,
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
