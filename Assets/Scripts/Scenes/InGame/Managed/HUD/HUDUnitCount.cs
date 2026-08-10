using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ECS で集計したユニット数、弾数、成長状況を画面左下の HUD に表示する。
/// 既存シーンでは TMP_Text が 2 つ近い位置に置かれているため、表示は 1 枠に集約する。
/// </summary>
public class HUDUnitCount : MonoBehaviour
{
    const float ChampionBarWidth = 720f;
    const float ExperienceBarWidth = 920f;
    const float PlayerHealthBarWidth = 420f;
    const float LowHealthThreshold = 0.25f;
    const float LowHealthPulseSpeed = 5.5f;
    const string JapaneseFontResourcePath =
        "Fonts/noto-sans-jp/NotoSansJP-Medium SDF";
    const string RequiredHudCharacters =
        " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
        "!！#%()+,-./:→—、" +
        "レベルアップキーでもう一度勝利敗北再挑戦経験値スコア脅威度" +
        "経過残り最終ボス戦敵エリートチャンピオン撃破弾数攻撃自動手動" +
        "特殊威力速度範囲会心連撃処刑強敵狂戦士防御装甲再生生命起死回生" +
        "武器斬速射貫通槍爆裂球補助移動磁力英知遠幸運抽選候補除外使用済み" +
        "早業俊足広域化活力強靭剣術慧眼獰猛鉄壁人狩り剛力間隔最大吸引" +
        "解放強化被追加以下一度復活秒間無獲得飛び道具ごとの大群襲来第波" +
        "体接近上昇回復欠片激昂形態生存時間残" +
        "連続倍率最高後次まで＋与平均ダメージ戦闘";

    public TMP_Text unitText;
    public TMP_Text bulletText;

    GameObject championHealthRoot;
    RectTransform championHealthFill;
    Image championHealthFillImage;
    TMP_Text championHealthLabel;
    GameObject experienceProgressRoot;
    RectTransform experienceProgressFill;
    TMP_Text experienceProgressLabel;
    GameObject playerHealthRoot;
    RectTransform playerHealthFill;
    Image playerHealthFillImage;
    TMP_Text playerHealthLabel;
    GameObject lowHealthWarningRoot;
    Image[] lowHealthWarningEdges;
    bool lowHealthWarningActive;
    GameObject threatAnnouncementRoot;
    Image threatAnnouncementBackground;
    TMP_Text threatAnnouncementLabel;
    float threatAnnouncementRemaining;
    Color threatAnnouncementAccent;
    GameObject threatIndicatorRoot;
    RectTransform threatIndicatorRect;
    RectTransform threatIndicatorArrowRect;
    TMP_Text threatIndicatorArrow;
    TMP_Text threatIndicatorLabel;
    Camera threatIndicatorCamera;
    bool threatIndicatorActive;
    int threatIndicatorRank;
    Vector3 threatIndicatorWorldPosition;
    GameObject upcomingThreatRoot;
    Image upcomingThreatBackground;
    TMP_Text upcomingThreatLabel;
    GameObject runResultRoot;
    TMP_Text runResultLabel;
    GameObject upgradeChoiceRoot;
    TMP_Text upgradeChoiceLabel;
    TMP_FontAsset japaneseFont;

    void Awake()
    {
        japaneseFont = Resources.Load<TMP_FontAsset>(
            JapaneseFontResourcePath);
        PrepareJapaneseFont();
        ApplyJapaneseFont(unitText);
        ApplyJapaneseFont(bulletText);
        ConfigureText(
            unitText,
            new Vector2(24f, 24f),
            new Vector2(1000f, 160f),
            28f);
        ConfigureText(
            bulletText,
            new Vector2(24f, 194f),
            new Vector2(1600f, 210f),
            20f);
        CreatePlayerHealthBar();
        CreateLowHealthWarning();
        CreateExperienceProgressBar();
        CreateChampionHealthBar();
        CreateThreatAnnouncement();
        CreateThreatIndicator();
        CreateUpcomingThreatCountdown();
        CreateRunResultOverlay();
        CreateUpgradeChoiceOverlay();
    }

    void Update()
    {
        UpdateLowHealthWarning();
        UpdateThreatAnnouncement();
        UpdateThreatIndicator();
    }

    void UpdateLowHealthWarning()
    {
        if (!lowHealthWarningActive || lowHealthWarningRoot == null)
        {
            return;
        }

        var pulse = CalculateLowHealthPulse(Time.unscaledTime);
        var warningColor = GetLowHealthWarningColor(pulse);
        for (var i = 0; i < lowHealthWarningEdges.Length; i++)
        {
            lowHealthWarningEdges[i].color = warningColor;
        }

        if (playerHealthRoot != null)
        {
            playerHealthRoot.transform.localScale =
                Vector3.one * Mathf.Lerp(1.02f, 1.07f, pulse);
        }

        if (playerHealthLabel != null)
        {
            playerHealthLabel.color = Color.Lerp(
                Color.white,
                new Color(1f, 0.52f, 0.52f, 1f),
                pulse);
        }
    }

    void UpdateThreatAnnouncement()
    {
        if (threatAnnouncementRoot == null ||
            threatAnnouncementRemaining <= 0f)
        {
            return;
        }

        threatAnnouncementRemaining = Mathf.Max(
            0f,
            threatAnnouncementRemaining - Time.unscaledDeltaTime);
        if (threatAnnouncementRemaining <= 0f)
        {
            threatAnnouncementRoot.SetActive(false);
            threatAnnouncementRoot.transform.localScale = Vector3.one;
            return;
        }

        var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
        threatAnnouncementRoot.transform.localScale =
            Vector3.one * Mathf.Lerp(1f, 1.035f, pulse);
        threatAnnouncementBackground.color = new Color(
            threatAnnouncementAccent.r * 0.18f,
            threatAnnouncementAccent.g * 0.12f,
            threatAnnouncementAccent.b * 0.12f,
            0.9f);
        threatAnnouncementLabel.color = Color.Lerp(
            Color.white,
            threatAnnouncementAccent,
            0.35f + pulse * 0.35f);
    }

    public void SetCount(
        int unitNum,
        int bulletNum,
        int eliteNum,
        int eliteCurrentHealth,
        int eliteMaxHealth,
        int championNum,
        int championCurrentHealth,
        int championMaxHealth,
        int finalBossNum,
        int finalBossCurrentHealth,
        int finalBossMaxHealth,
        int level,
        int experience,
        int experienceToNextLevel,
        int score,
        int health,
        int maxHealth,
        float elapsedSeconds,
        float durationSeconds,
        int threatLevel,
        int enemiesDefeated,
        int killStreak,
        int bestKillStreak,
        int killStreakScoreMultiplier,
        float killStreakRemainingSeconds,
        long totalDamageDealt,
        float currentDps,
        float peakDps,
        bool isGameOver,
        bool isVictory,
        bool autoAttackEnabled,
        PlayerSkillStats skillStats,
        bool hasUpgradeChoice,
        PlayerUpgradeChoice upgradeChoice,
        string statusNotification)
    {
        var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        var attackMode = autoAttackEnabled ? "自動" : "手動";
        UpdatePlayerHealthBar(health, maxHealth);
        UpdateExperienceProgressBar(
            level,
            experience,
            experienceToNextLevel);
        UpdateChampionHealthBar(
            eliteNum,
            eliteCurrentHealth,
            eliteMaxHealth,
            championNum,
            championCurrentHealth,
            championMaxHealth,
            finalBossNum,
            finalBossCurrentHealth,
            finalBossMaxHealth);
        UpdateRunResultOverlay(
            isGameOver,
            isVictory,
            minutes,
            seconds,
            level,
            score,
            threatLevel,
            enemiesDefeated,
            totalDamageDealt,
            peakDps);
        UpdateUpgradeChoiceOverlay(
            hasUpgradeChoice,
            upgradeChoice,
            skillStats);

        unitText.text =
            $"スコア {score}\n" +
            $"{FormatRunClock(elapsedSeconds, durationSeconds, isGameOver)}   脅威度 {threatLevel}" +
            (isGameOver
                ? isVictory
                    ? "\n勝利！   Rキーでもう一度"
                    : "\n敗北   Rキーで再挑戦"
                : string.Empty);
        bulletText.text =
            (string.IsNullOrEmpty(statusNotification)
                ? string.Empty
                : $"{statusNotification}\n") +
            FormatKillStreak(
                killStreak,
                bestKillStreak,
                killStreakScoreMultiplier,
                killStreakRemainingSeconds) +
            $"敵 {Mathf.Max(0, unitNum - 1)}   エリート {Mathf.Max(0, eliteNum)}   " +
            $"チャンピオン {Mathf.Max(0, championNum)}   " +
            $"撃破 {Mathf.Max(0, enemiesDefeated)}   " +
            $"弾数 {bulletNum}   攻撃 {attackMode}\n" +
            FormatCombatStats(
                totalDamageDealt,
                currentDps,
                peakDps) + "\n" +
            FormatBuildStats(skillStats);
    }

    public static string FormatCombatStats(
        long totalDamageDealt,
        float currentDps,
        float peakDps)
    {
        var safeTotal = totalDamageDealt < 0 ? 0 : totalDamageDealt;
        return
            $"戦闘  与ダメージ {safeTotal:N0}   " +
            $"DPS {Mathf.Max(0f, currentDps):0}   " +
            $"最高DPS {Mathf.Max(0f, peakDps):0}";
    }

    public static string FormatKillStreak(
        int killStreak,
        int bestKillStreak,
        int scoreMultiplier,
        float remainingSeconds)
    {
        var safeBest = Mathf.Max(0, bestKillStreak);
        if (killStreak <= 0)
        {
            return safeBest > 0
                ? $"最高連続撃破 {safeBest}\n"
                : string.Empty;
        }

        return
            $"連続撃破 {killStreak}   スコア x{Mathf.Clamp(scoreMultiplier, 1, 3)}   " +
            $"残り {Mathf.Max(0f, remainingSeconds):0.0}秒   " +
            $"最高 {Mathf.Max(safeBest, killStreak)}\n";
    }

    public static string FormatRunClock(
        float elapsedSeconds,
        float durationSeconds,
        bool isGameOver)
    {
        var safeElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
        var safeDurationSeconds = Mathf.Max(0f, durationSeconds);
        var totalSeconds = Mathf.FloorToInt(safeElapsedSeconds);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        if (!isGameOver &&
            safeDurationSeconds > 0f &&
            safeElapsedSeconds >= safeDurationSeconds)
        {
            return $"経過 {minutes:00}:{seconds:00}   最終ボス戦";
        }

        var remainingTotalSeconds = Mathf.Max(
            0,
            Mathf.CeilToInt(
                safeDurationSeconds - safeElapsedSeconds));
        var remainingMinutes = remainingTotalSeconds / 60;
        var remainingSeconds = remainingTotalSeconds % 60;
        return
            $"経過 {minutes:00}:{seconds:00}   " +
            $"残り {remainingMinutes:00}:{remainingSeconds:00}";
    }

    public static string FormatBuildStats(PlayerSkillStats skillStats)
    {
        return
            $"攻撃  威力 Lv{skillStats.DamageLevel} x{1f + skillStats.DamageMultiplierAdd:0.00}   " +
            $"速度 Lv{skillStats.AttackSpeedLevel} +{skillStats.CooldownMultiplierReduction * 100f:0}%   " +
            $"範囲 Lv{skillStats.AreaLevel} x{1f + skillStats.AreaMultiplierAdd:0.00}\n" +
            $"特殊  会心 Lv{skillStats.CriticalChanceLevel} {skillStats.CriticalChance * 100f:0}%   " +
            $"会心威力 Lv{skillStats.CriticalDamageLevel} " +
            $"x{1.5f + skillStats.CriticalDamageMultiplierAdd:0.00}   " +
            $"連撃 Lv{skillStats.MultistrikeLevel} {skillStats.MultistrikeChance * 100f:0}%   " +
            $"処刑 Lv{skillStats.ExecutionerLevel} +{skillStats.ExecutionDamageMultiplierAdd * 100f:0}%   " +
            $"強敵 Lv{skillStats.BossHunterLevel} +{skillStats.EliteDamageMultiplierAdd * 100f:0}%   " +
            $"狂戦士 Lv{skillStats.BerserkerLevel} +{skillStats.LowHealthDamageMultiplierAdd * 100f:0}%\n" +
            $"防御  装甲 Lv{skillStats.ArmorLevel} -{skillStats.DamageReduction * 100f:0}%   " +
            $"再生 Lv{skillStats.RegenerationLevel} {skillStats.HealthRegenerationPerSecond:0.0}/秒   " +
            $"生命 Lv{skillStats.MaxHealthLevel} +{skillStats.MaxHealthAdd:0} HP   " +
            $"起死回生 Lv{skillStats.SecondWindLevel} 残り {skillStats.SecondWindChargesRemaining}\n" +
            $"武器  斬撃 Lv{skillStats.MeleeArcLevel}   " +
            $"速射 Lv{skillStats.RapidBoltLevel}   " +
            $"貫通槍 Lv{skillStats.PiercingLanceLevel}   " +
            $"爆裂球 Lv{skillStats.ExplosiveOrbLevel}\n" +
            $"補助  移動 Lv{skillStats.MoveSpeedLevel} x{1f + skillStats.MoveSpeedMultiplierAdd:0.00}   " +
            $"磁力 Lv{skillStats.PickupRangeLevel} +{skillStats.PickupRadiusAdd:0.0}m   " +
            $"英知 Lv{skillStats.WisdomLevel} " +
            $"x{1f + skillStats.ExperienceMultiplierAdd:0.00} XP   " +
            $"遠射 Lv{skillStats.LongshotLevel} " +
            $"x{1f + skillStats.ProjectileLifetimeMultiplierAdd:0.00}   " +
            $"貫通 Lv{skillStats.PenetrationLevel} " +
            $"+{PlayerAutoSkillSystem.GetProjectilePierceAdd(skillStats)}   " +
            $"幸運 Lv{skillStats.FortuneLevel} " +
            $"再抽選{PlayerAutoSkillSystem.GetUpgradeRerollCount(skillStats)}回";
    }

    void CreateUpgradeChoiceOverlay()
    {
        upgradeChoiceRoot = new GameObject(
            "Upgrade Choice Overlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        upgradeChoiceRoot.layer = gameObject.layer;
        upgradeChoiceRoot.transform.SetParent(transform, false);

        var rootRect = upgradeChoiceRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(920f, 500f);

        var background = upgradeChoiceRoot.GetComponent<Image>();
        background.color = new Color(0.025f, 0.02f, 0.035f, 0.95f);
        background.raycastTarget = false;

        var labelObject = new GameObject(
            "Choices",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(upgradeChoiceRoot.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(48f, 34f);
        labelRect.offsetMax = new Vector2(-48f, -34f);

        upgradeChoiceLabel = labelObject.GetComponent<TextMeshProUGUI>();
        ApplyJapaneseFont(upgradeChoiceLabel);
        upgradeChoiceLabel.alignment = TextAlignmentOptions.Center;
        upgradeChoiceLabel.fontStyle = FontStyles.Bold;
        upgradeChoiceLabel.fontSize = 27f;
        upgradeChoiceLabel.color = Color.white;
        upgradeChoiceLabel.textWrappingMode = TextWrappingModes.NoWrap;
        upgradeChoiceLabel.raycastTarget = false;

        upgradeChoiceRoot.SetActive(false);
    }

    void UpdateUpgradeChoiceOverlay(
        bool isActive,
        PlayerUpgradeChoice choice,
        PlayerSkillStats stats)
    {
        if (upgradeChoiceRoot == null)
        {
            return;
        }

        upgradeChoiceRoot.SetActive(isActive);
        if (!isActive)
        {
            return;
        }

        var queuedLevels = choice.PendingLevels > 1
            ? $"  <size=22>(残り{choice.PendingLevels}回)</size>"
            : string.Empty;
        var rerollHint = choice.RerollsRemaining > 0
            ? $"<color=#7ED8FF>[R] 再抽選</color>  <size=20>残り{choice.RerollsRemaining}回</size>"
            : "<color=#777788>[R] 再抽選 使用済み</color>";
        var banishHint = choice.BanishesRemaining > 0
            ? $"<color=#FF9E7A>[Q/W/E] 候補を除外 1/2/3</color>  <size=20>残り{choice.BanishesRemaining}回</size>"
            : "<color=#777788>[Q/W/E] 除外 使用済み</color>";
        upgradeChoiceLabel.text =
            $"<color=#FFD75A><size=48>レベルアップ！</size></color>{queuedLevels}\n" +
            "<size=22>強化を選択 — 1、2、3キー</size>\n\n" +
            rerollHint + "     " + banishHint + "\n\n" +
            FormatUpgradeOption(1, choice.First, stats) + "\n\n" +
            FormatUpgradeOption(2, choice.Second, stats) + "\n\n" +
            FormatUpgradeOption(3, choice.Third, stats);
    }

    static string FormatUpgradeOption(
        int index,
        PlayerSkillMasterData skill,
        PlayerSkillStats stats)
    {
        var currentLevel = GetSkillLevel(skill.Kind, stats);
        var nextLevel = Mathf.Min(
            skill.MaxLevel > 0 ? skill.MaxLevel : int.MaxValue,
            currentLevel + Mathf.Max(1, skill.AddLevel));
        var effect = GetSkillEffectDescription(skill);
        return
            $"<color=#FFD75A>[{index}]</color>  " +
            $"<size=34>{GetUpgradeName(skill.Kind)}</size>  " +
            $"<color=#B7B7C8>Lv{currentLevel} → Lv{nextLevel}</color>\n" +
            $"<size=22>{effect}</size>";
    }

    static int GetSkillLevel(PlayerSkillKind kind, PlayerSkillStats stats)
    {
        switch (kind)
        {
            case PlayerSkillKind.AttackSpeed:
                return stats.AttackSpeedLevel;
            case PlayerSkillKind.MoveSpeed:
                return stats.MoveSpeedLevel;
            case PlayerSkillKind.Area:
                return stats.AreaLevel;
            case PlayerSkillKind.Regeneration:
                return stats.RegenerationLevel;
            case PlayerSkillKind.MaxHealth:
                return stats.MaxHealthLevel;
            case PlayerSkillKind.PickupRange:
                return stats.PickupRangeLevel;
            case PlayerSkillKind.MeleeArc:
                return stats.MeleeArcLevel;
            case PlayerSkillKind.RapidBolt:
                return stats.RapidBoltLevel;
            case PlayerSkillKind.PiercingLance:
                return stats.PiercingLanceLevel;
            case PlayerSkillKind.ExplosiveOrb:
                return stats.ExplosiveOrbLevel;
            case PlayerSkillKind.CriticalChance:
                return stats.CriticalChanceLevel;
            case PlayerSkillKind.CriticalDamage:
                return stats.CriticalDamageLevel;
            case PlayerSkillKind.Armor:
                return stats.ArmorLevel;
            case PlayerSkillKind.Multistrike:
                return stats.MultistrikeLevel;
            case PlayerSkillKind.Executioner:
                return stats.ExecutionerLevel;
            case PlayerSkillKind.SecondWind:
                return stats.SecondWindLevel;
            case PlayerSkillKind.Wisdom:
                return stats.WisdomLevel;
            case PlayerSkillKind.Longshot:
                return stats.LongshotLevel;
            case PlayerSkillKind.BossHunter:
                return stats.BossHunterLevel;
            case PlayerSkillKind.Penetration:
                return stats.PenetrationLevel;
            case PlayerSkillKind.Fortune:
                return stats.FortuneLevel;
            case PlayerSkillKind.Berserker:
                return stats.BerserkerLevel;
            case PlayerSkillKind.Damage:
            default:
                return stats.DamageLevel;
        }
    }

    static string GetUpgradeName(PlayerSkillKind kind)
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

    static string GetSkillEffectDescription(PlayerSkillMasterData skill)
    {
        switch (skill.Kind)
        {
            case PlayerSkillKind.AttackSpeed:
                return $"攻撃間隔 -{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.MoveSpeed:
                return $"移動速度 +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Area:
                return $"攻撃範囲 +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Regeneration:
                return $"HP再生 +{skill.EffectPerLevel:0.0}/秒";
            case PlayerSkillKind.MaxHealth:
                return $"最大HP +{skill.EffectPerLevel:0}";
            case PlayerSkillKind.PickupRange:
                return $"経験値の吸引範囲 +{skill.EffectPerLevel:0.0}m";
            case PlayerSkillKind.MeleeArc:
            case PlayerSkillKind.RapidBolt:
            case PlayerSkillKind.PiercingLance:
            case PlayerSkillKind.ExplosiveOrb:
                return $"武器を解放または強化・威力 +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.CriticalChance:
                return $"会心率 +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.CriticalDamage:
                return $"会心威力 +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Armor:
                return $"被ダメージ -{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Multistrike:
                return $"追加攻撃率 +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Executioner:
                return $"HP30%以下の敵へのダメージ +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.SecondWind:
                return $"一度だけHP{skill.EffectPerLevel * 100f:0}%で復活・2秒間無敵";
            case PlayerSkillKind.Wisdom:
                return $"獲得経験値 +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Longshot:
                return $"飛び道具の射程 +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.BossHunter:
                return $"エリート・チャンピオンへのダメージ +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Penetration:
                return $"飛び道具の貫通数 +{skill.EffectPerLevel:0}";
            case PlayerSkillKind.Fortune:
                return $"強化選択ごとの再抽選 +{skill.EffectPerLevel:0}回";
            case PlayerSkillKind.Berserker:
                return $"HP50%以下でダメージ +{skill.EffectPerLevel * 100f:0}%";
            case PlayerSkillKind.Damage:
            default:
                return $"ダメージ +{skill.EffectPerLevel * 100f:0}%";
        }
    }

    void CreateThreatAnnouncement()
    {
        threatAnnouncementRoot = new GameObject(
            "Threat Announcement",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        threatAnnouncementRoot.layer = gameObject.layer;
        threatAnnouncementRoot.transform.SetParent(transform, false);

        var rootRect = threatAnnouncementRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -128f);
        rootRect.sizeDelta = new Vector2(860f, 108f);

        threatAnnouncementBackground =
            threatAnnouncementRoot.GetComponent<Image>();
        threatAnnouncementBackground.raycastTarget = false;

        var labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(
            threatAnnouncementRoot.transform,
            false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 8f);
        labelRect.offsetMax = new Vector2(-24f, -8f);

        threatAnnouncementLabel = labelObject.GetComponent<TextMeshProUGUI>();
        ApplyJapaneseFont(threatAnnouncementLabel);
        threatAnnouncementLabel.alignment = TextAlignmentOptions.Center;
        threatAnnouncementLabel.fontStyle = FontStyles.Bold;
        threatAnnouncementLabel.fontSize = 38f;
        threatAnnouncementLabel.textWrappingMode =
            TextWrappingModes.NoWrap;
        threatAnnouncementLabel.raycastTarget = false;
        threatAnnouncementRoot.SetActive(false);
    }

    public void ShowThreatAnnouncement(
        string message,
        float durationSeconds,
        Color accent)
    {
        if (threatAnnouncementRoot == null ||
            string.IsNullOrEmpty(message))
        {
            return;
        }

        threatAnnouncementRemaining = Mathf.Max(
            0.1f,
            durationSeconds);
        threatAnnouncementAccent = accent;
        threatAnnouncementLabel.text = message;
        threatAnnouncementRoot.transform.localScale = Vector3.one;
        threatAnnouncementRoot.SetActive(true);
    }

    void CreateThreatIndicator()
    {
        threatIndicatorRoot = new GameObject(
            "Threat Indicator",
            typeof(RectTransform));
        threatIndicatorRoot.layer = gameObject.layer;
        threatIndicatorRoot.transform.SetParent(transform, false);

        threatIndicatorRect =
            threatIndicatorRoot.GetComponent<RectTransform>();
        threatIndicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        threatIndicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        threatIndicatorRect.pivot = new Vector2(0.5f, 0.5f);
        threatIndicatorRect.sizeDelta = new Vector2(190f, 92f);

        var arrowObject = new GameObject(
            "Arrow",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        arrowObject.layer = gameObject.layer;
        arrowObject.transform.SetParent(threatIndicatorRoot.transform, false);
        threatIndicatorArrowRect =
            arrowObject.GetComponent<RectTransform>();
        threatIndicatorArrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        threatIndicatorArrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        threatIndicatorArrowRect.pivot = new Vector2(0.5f, 0.5f);
        threatIndicatorArrowRect.anchoredPosition = new Vector2(0f, 12f);
        threatIndicatorArrowRect.sizeDelta = new Vector2(80f, 64f);

        threatIndicatorArrow = arrowObject.GetComponent<TextMeshProUGUI>();
        ApplyJapaneseFont(threatIndicatorArrow);
        threatIndicatorArrow.text = "→";
        threatIndicatorArrow.alignment = TextAlignmentOptions.Center;
        threatIndicatorArrow.fontStyle = FontStyles.Bold;
        threatIndicatorArrow.fontSize = 54f;
        threatIndicatorArrow.raycastTarget = false;

        var labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(threatIndicatorRoot.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, -31f);
        labelRect.sizeDelta = new Vector2(190f, 32f);

        threatIndicatorLabel = labelObject.GetComponent<TextMeshProUGUI>();
        ApplyJapaneseFont(threatIndicatorLabel);
        threatIndicatorLabel.alignment = TextAlignmentOptions.Center;
        threatIndicatorLabel.fontStyle = FontStyles.Bold;
        threatIndicatorLabel.fontSize = 20f;
        threatIndicatorLabel.textWrappingMode = TextWrappingModes.NoWrap;
        threatIndicatorLabel.raycastTarget = false;
        threatIndicatorRoot.SetActive(false);
    }

    public void SetThreatIndicator(
        bool isActive,
        int rank,
        Vector3 worldPosition)
    {
        threatIndicatorActive = isActive;
        threatIndicatorWorldPosition = worldPosition;
        if (rank != threatIndicatorRank)
        {
            threatIndicatorRank = rank;
            ApplyThreatIndicatorRank();
        }

        if (!isActive && threatIndicatorRoot != null)
        {
            threatIndicatorRoot.SetActive(false);
        }
    }

    void UpdateThreatIndicator()
    {
        if (!threatIndicatorActive || threatIndicatorRoot == null)
        {
            return;
        }

        if (threatIndicatorCamera == null)
        {
            threatIndicatorCamera = Camera.main;
        }

        var canvasRect = transform as RectTransform;
        if (threatIndicatorCamera == null || canvasRect == null)
        {
            threatIndicatorRoot.SetActive(false);
            return;
        }

        var viewportPosition = threatIndicatorCamera.WorldToViewportPoint(
            threatIndicatorWorldPosition);
        if (IsThreatInsideViewport(viewportPosition))
        {
            threatIndicatorRoot.SetActive(false);
            return;
        }

        threatIndicatorRoot.SetActive(true);
        threatIndicatorRect.anchoredPosition =
            CalculateThreatIndicatorPosition(
                viewportPosition,
                canvasRect.rect.size,
                new Vector2(110f, 82f));

        var direction = threatIndicatorRect.anchoredPosition.normalized;
        threatIndicatorArrowRect.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
        threatIndicatorArrowRect.localScale =
            Vector3.one * Mathf.Lerp(1f, 1.12f, pulse);
    }

    void ApplyThreatIndicatorRank()
    {
        if (threatIndicatorLabel == null || threatIndicatorArrow == null)
        {
            return;
        }

        Color accent;
        switch (threatIndicatorRank)
        {
            case 3:
                threatIndicatorLabel.text = "最終ボス";
                accent = new Color(1f, 0.1f, 0.16f, 1f);
                break;
            case 2:
                threatIndicatorLabel.text = "チャンピオン";
                accent = new Color(0.78f, 0.28f, 1f, 1f);
                break;
            default:
                threatIndicatorLabel.text = "エリート";
                accent = new Color(1f, 0.58f, 0.12f, 1f);
                break;
        }

        threatIndicatorArrow.color = accent;
        threatIndicatorLabel.color = accent;
    }

    public static bool IsThreatInsideViewport(Vector3 viewportPosition)
    {
        return viewportPosition.z > 0f &&
               viewportPosition.x >= 0.06f &&
               viewportPosition.x <= 0.94f &&
               viewportPosition.y >= 0.08f &&
               viewportPosition.y <= 0.92f;
    }

    public static Vector2 CalculateThreatIndicatorPosition(
        Vector3 viewportPosition,
        Vector2 canvasSize,
        Vector2 edgePadding)
    {
        var centered = new Vector2(
            viewportPosition.x - 0.5f,
            viewportPosition.y - 0.5f);
        if (viewportPosition.z < 0f)
        {
            centered = -centered;
        }

        var pixelDirection = new Vector2(
            centered.x * canvasSize.x,
            centered.y * canvasSize.y);
        if (pixelDirection.sqrMagnitude < 0.0001f)
        {
            pixelDirection = Vector2.up;
        }

        var direction = pixelDirection.normalized;
        var halfSize = Vector2.Max(
            Vector2.zero,
            canvasSize * 0.5f - edgePadding);
        var horizontalDistance = halfSize.x /
            Mathf.Max(Mathf.Abs(direction.x), 0.0001f);
        var verticalDistance = halfSize.y /
            Mathf.Max(Mathf.Abs(direction.y), 0.0001f);
        return direction * Mathf.Min(
            horizontalDistance,
            verticalDistance);
    }

    void CreateUpcomingThreatCountdown()
    {
        upcomingThreatRoot = new GameObject(
            "Upcoming Threat Countdown",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        upcomingThreatRoot.layer = gameObject.layer;
        upcomingThreatRoot.transform.SetParent(transform, false);

        var rootRect = upcomingThreatRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -44f);
        rootRect.sizeDelta = new Vector2(440f, 58f);

        upcomingThreatBackground = upcomingThreatRoot.GetComponent<Image>();
        upcomingThreatBackground.raycastTarget = false;

        var labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(upcomingThreatRoot.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 4f);
        labelRect.offsetMax = new Vector2(-16f, -4f);

        upcomingThreatLabel = labelObject.GetComponent<TextMeshProUGUI>();
        ApplyJapaneseFont(upcomingThreatLabel);
        upcomingThreatLabel.alignment = TextAlignmentOptions.Center;
        upcomingThreatLabel.fontStyle = FontStyles.Bold;
        upcomingThreatLabel.fontSize = 25f;
        upcomingThreatLabel.textWrappingMode = TextWrappingModes.NoWrap;
        upcomingThreatLabel.raycastTarget = false;
        upcomingThreatRoot.SetActive(false);
    }

    public void SetUpcomingThreat(
        UpcomingThreatEventKind kind,
        float remainingSeconds)
    {
        if (upcomingThreatRoot == null)
        {
            return;
        }

        var message = FormatUpcomingThreat(kind, remainingSeconds);
        var isActive = !string.IsNullOrEmpty(message);
        upcomingThreatRoot.SetActive(isActive);
        if (!isActive)
        {
            upcomingThreatRoot.transform.localScale = Vector3.one;
            return;
        }

        var isImminent = remainingSeconds <= 10f;
        var accent = ResolveUpcomingThreatColor(kind);
        var pulse = isImminent
            ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f)
            : 0f;
        upcomingThreatLabel.text = message;
        upcomingThreatLabel.color = Color.Lerp(
            Color.white,
            accent,
            isImminent ? 0.45f + pulse * 0.45f : 0.28f);
        upcomingThreatBackground.color = new Color(
            accent.r * 0.12f,
            accent.g * 0.09f,
            accent.b * 0.07f,
            isImminent ? 0.92f : 0.78f);
        upcomingThreatRoot.transform.localScale = Vector3.one *
            (isImminent ? Mathf.Lerp(1f, 1.045f, pulse) : 1f);
    }

    public static string FormatUpcomingThreat(
        UpcomingThreatEventKind kind,
        float remainingSeconds)
    {
        if (kind == UpcomingThreatEventKind.None)
        {
            return string.Empty;
        }

        var totalSeconds = Mathf.Max(
            0,
            Mathf.CeilToInt(remainingSeconds));
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        string eventName;
        switch (kind)
        {
            case UpcomingThreatEventKind.FinalBoss:
                eventName = "最終ボス";
                break;
            case UpcomingThreatEventKind.ChampionWave:
                eventName = "チャンピオン";
                break;
            case UpcomingThreatEventKind.HordeAndChampion:
                eventName = "大群＋チャンピオン";
                break;
            default:
                eventName = "次の大群";
                break;
        }

        return $"{eventName}まで  {minutes:00}:{seconds:00}";
    }

    public static Color ResolveUpcomingThreatColor(
        UpcomingThreatEventKind kind)
    {
        switch (kind)
        {
            case UpcomingThreatEventKind.FinalBoss:
                return new Color(1f, 0.16f, 0.22f, 1f);
            case UpcomingThreatEventKind.ChampionWave:
                return new Color(0.76f, 0.24f, 1f, 1f);
            case UpcomingThreatEventKind.HordeAndChampion:
                return new Color(1f, 0.28f, 0.72f, 1f);
            default:
                return new Color(1f, 0.62f, 0.12f, 1f);
        }
    }

    void CreateRunResultOverlay()
    {
        runResultRoot = new GameObject(
            "Run Result Overlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        runResultRoot.layer = gameObject.layer;
        runResultRoot.transform.SetParent(transform, false);

        var rootRect = runResultRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(680f, 330f);

        var background = runResultRoot.GetComponent<Image>();
        background.color = new Color(0.025f, 0.02f, 0.035f, 0.92f);
        background.raycastTarget = false;

        var labelObject = new GameObject(
            "Result",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(runResultRoot.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(28f, 24f);
        labelRect.offsetMax = new Vector2(-28f, -24f);

        runResultLabel = labelObject.GetComponent<TextMeshProUGUI>();
        ApplyJapaneseFont(runResultLabel);
        runResultLabel.alignment = TextAlignmentOptions.Center;
        runResultLabel.fontStyle = FontStyles.Bold;
        runResultLabel.fontSize = 30f;
        runResultLabel.color = Color.white;
        runResultLabel.textWrappingMode = TextWrappingModes.NoWrap;
        runResultLabel.raycastTarget = false;

        runResultRoot.SetActive(false);
    }

    void UpdateRunResultOverlay(
        bool isGameOver,
        bool isVictory,
        int minutes,
        int seconds,
        int level,
        int score,
        int threatLevel,
        int enemiesDefeated,
        long totalDamageDealt,
        float peakDps)
    {
        if (runResultRoot == null)
        {
            return;
        }

        runResultRoot.SetActive(isGameOver);
        if (!isGameOver)
        {
            return;
        }

        var title = isVictory ? "勝利" : "敗北";
        var titleColor = isVictory ? "#FFD75A" : "#FF6767";
        runResultLabel.text =
            $"<color={titleColor}><size=52>{title}</size></color>\n" +
            $"生存時間  {minutes:00}:{seconds:00}    レベル  {level}\n" +
            $"スコア  {score}    撃破  {Mathf.Max(0, enemiesDefeated)}    " +
            $"脅威度  {threatLevel}\n" +
            $"与ダメージ  {(totalDamageDealt < 0 ? 0 : totalDamageDealt):N0}    " +
            $"最高DPS  {Mathf.Max(0f, peakDps):0}\n" +
            "<size=24>Rキーでもう一度</size>";
    }

    void CreateChampionHealthBar()
    {
        championHealthRoot = new GameObject(
            "Champion Health Bar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        championHealthRoot.layer = gameObject.layer;
        championHealthRoot.transform.SetParent(transform, false);

        var rootRect = championHealthRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -24f);
        rootRect.sizeDelta = new Vector2(ChampionBarWidth, 32f);

        var background = championHealthRoot.GetComponent<Image>();
        background.color = new Color(0.05f, 0.01f, 0.08f, 0.88f);
        background.raycastTarget = false;

        var fillObject = new GameObject(
            "Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fillObject.layer = gameObject.layer;
        fillObject.transform.SetParent(championHealthRoot.transform, false);
        championHealthFill = fillObject.GetComponent<RectTransform>();
        championHealthFill.anchorMin = new Vector2(0f, 0f);
        championHealthFill.anchorMax = new Vector2(0f, 1f);
        championHealthFill.pivot = new Vector2(0f, 0.5f);
        championHealthFill.anchoredPosition = new Vector2(3f, 0f);

        championHealthFillImage = fillObject.GetComponent<Image>();
        championHealthFillImage.color =
            new Color(0.72f, 0.08f, 1f, 0.95f);
        championHealthFillImage.raycastTarget = false;

        var labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(championHealthRoot.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        championHealthLabel = labelObject.GetComponent<TextMeshProUGUI>();
        ApplyJapaneseFont(championHealthLabel);
        championHealthLabel.fontSize = 18f;
        championHealthLabel.fontStyle = FontStyles.Bold;
        championHealthLabel.alignment = TextAlignmentOptions.Center;
        championHealthLabel.color = Color.white;
        championHealthLabel.raycastTarget = false;

        championHealthRoot.SetActive(false);
    }

    void CreateExperienceProgressBar()
    {
        experienceProgressRoot = new GameObject(
            "Experience Progress Bar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        experienceProgressRoot.layer = gameObject.layer;
        experienceProgressRoot.transform.SetParent(transform, false);

        var rootRect = experienceProgressRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -68f);
        rootRect.sizeDelta = new Vector2(ExperienceBarWidth, 28f);

        var background = experienceProgressRoot.GetComponent<Image>();
        background.color = new Color(0.025f, 0.035f, 0.055f, 0.9f);
        background.raycastTarget = false;

        var fillObject = new GameObject(
            "Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fillObject.layer = gameObject.layer;
        fillObject.transform.SetParent(experienceProgressRoot.transform, false);
        experienceProgressFill = fillObject.GetComponent<RectTransform>();
        experienceProgressFill.anchorMin = new Vector2(0f, 0f);
        experienceProgressFill.anchorMax = new Vector2(0f, 1f);
        experienceProgressFill.pivot = new Vector2(0f, 0.5f);
        experienceProgressFill.anchoredPosition = new Vector2(3f, 0f);

        var fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.18f, 0.72f, 1f, 0.96f);
        fillImage.raycastTarget = false;

        var labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(experienceProgressRoot.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        experienceProgressLabel = labelObject.GetComponent<TextMeshProUGUI>();
        ApplyJapaneseFont(experienceProgressLabel);
        experienceProgressLabel.fontSize = 18f;
        experienceProgressLabel.fontStyle = FontStyles.Bold;
        experienceProgressLabel.alignment = TextAlignmentOptions.Center;
        experienceProgressLabel.color = Color.white;
        experienceProgressLabel.raycastTarget = false;
    }

    void UpdateExperienceProgressBar(
        int level,
        int experience,
        int experienceToNextLevel)
    {
        if (experienceProgressRoot == null)
        {
            return;
        }

        var safeExperience = Mathf.Max(0, experience);
        var safeExperienceToNextLevel = Mathf.Max(
            1,
            experienceToNextLevel);
        var progress = CalculateExperienceProgress(
            safeExperience,
            safeExperienceToNextLevel);
        experienceProgressFill.sizeDelta = new Vector2(
            (ExperienceBarWidth - 6f) * progress,
            -6f);
        experienceProgressLabel.text =
            $"Lv {Mathf.Max(1, level)}   経験値 " +
            $"{safeExperience}/{safeExperienceToNextLevel}";
    }

    public static float CalculateExperienceProgress(
        int experience,
        int experienceToNextLevel)
    {
        return Mathf.Clamp01(
            (float)Mathf.Max(0, experience) /
            Mathf.Max(1, experienceToNextLevel));
    }

    void CreatePlayerHealthBar()
    {
        playerHealthRoot = new GameObject(
            "Player Health Bar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        playerHealthRoot.layer = gameObject.layer;
        playerHealthRoot.transform.SetParent(transform, false);

        var rootRect = playerHealthRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(24f, -24f);
        rootRect.sizeDelta = new Vector2(PlayerHealthBarWidth, 30f);

        var background = playerHealthRoot.GetComponent<Image>();
        background.color = new Color(0.07f, 0.015f, 0.02f, 0.92f);
        background.raycastTarget = false;

        var fillObject = new GameObject(
            "Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fillObject.layer = gameObject.layer;
        fillObject.transform.SetParent(playerHealthRoot.transform, false);
        playerHealthFill = fillObject.GetComponent<RectTransform>();
        playerHealthFill.anchorMin = new Vector2(0f, 0f);
        playerHealthFill.anchorMax = new Vector2(0f, 1f);
        playerHealthFill.pivot = new Vector2(0f, 0.5f);
        playerHealthFill.anchoredPosition = new Vector2(3f, 0f);

        playerHealthFillImage = fillObject.GetComponent<Image>();
        playerHealthFillImage.raycastTarget = false;

        var labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(playerHealthRoot.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        playerHealthLabel = labelObject.GetComponent<TextMeshProUGUI>();
        ApplyJapaneseFont(playerHealthLabel);
        playerHealthLabel.fontSize = 19f;
        playerHealthLabel.fontStyle = FontStyles.Bold;
        playerHealthLabel.alignment = TextAlignmentOptions.Center;
        playerHealthLabel.color = Color.white;
        playerHealthLabel.raycastTarget = false;
    }

    void UpdatePlayerHealthBar(int health, int maximumHealth)
    {
        if (playerHealthRoot == null)
        {
            return;
        }

        var safeHealth = Mathf.Max(0, health);
        var safeMaximumHealth = Mathf.Max(1, maximumHealth);
        var healthRatio = CalculatePlayerHealthRatio(
            safeHealth,
            safeMaximumHealth);
        playerHealthFill.sizeDelta = new Vector2(
            (PlayerHealthBarWidth - 6f) * healthRatio,
            -6f);
        playerHealthFillImage.color = GetPlayerHealthColor(healthRatio);
        playerHealthLabel.text = $"HP {safeHealth}/{safeMaximumHealth}";
        SetLowHealthWarningActive(
            ShouldShowLowHealthWarning(safeHealth, safeMaximumHealth));
    }

    void CreateLowHealthWarning()
    {
        lowHealthWarningRoot = new GameObject(
            "Low Health Warning",
            typeof(RectTransform));
        lowHealthWarningRoot.layer = gameObject.layer;
        lowHealthWarningRoot.transform.SetParent(transform, false);
        lowHealthWarningRoot.transform.SetAsFirstSibling();

        var rootRect = lowHealthWarningRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        lowHealthWarningEdges = new[]
        {
            CreateLowHealthWarningEdge(
                "Top",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, 42f)),
            CreateLowHealthWarningEdge(
                "Bottom",
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 42f)),
            CreateLowHealthWarningEdge(
                "Left",
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(42f, 0f)),
            CreateLowHealthWarningEdge(
                "Right",
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0.5f),
                new Vector2(42f, 0f)),
        };
        lowHealthWarningRoot.SetActive(false);
    }

    Image CreateLowHealthWarningEdge(
        string edgeName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta)
    {
        var edgeObject = new GameObject(
            edgeName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        edgeObject.layer = gameObject.layer;
        edgeObject.transform.SetParent(lowHealthWarningRoot.transform, false);

        var edgeRect = edgeObject.GetComponent<RectTransform>();
        edgeRect.anchorMin = anchorMin;
        edgeRect.anchorMax = anchorMax;
        edgeRect.pivot = pivot;
        edgeRect.anchoredPosition = Vector2.zero;
        edgeRect.sizeDelta = sizeDelta;

        var edgeImage = edgeObject.GetComponent<Image>();
        edgeImage.color = GetLowHealthWarningColor(0f);
        edgeImage.raycastTarget = false;
        return edgeImage;
    }

    void SetLowHealthWarningActive(bool isActive)
    {
        lowHealthWarningActive = isActive;
        if (lowHealthWarningRoot != null)
        {
            lowHealthWarningRoot.SetActive(isActive);
        }

        if (isActive)
        {
            return;
        }

        if (playerHealthRoot != null)
        {
            playerHealthRoot.transform.localScale = Vector3.one;
        }

        if (playerHealthLabel != null)
        {
            playerHealthLabel.color = Color.white;
        }
    }

    public static bool ShouldShowLowHealthWarning(
        int health,
        int maximumHealth)
    {
        return health > 0 &&
            CalculatePlayerHealthRatio(health, maximumHealth) <=
                LowHealthThreshold;
    }

    public static float CalculateLowHealthPulse(float unscaledTime)
    {
        return 0.5f + 0.5f * Mathf.Sin(
            Mathf.Max(0f, unscaledTime) * LowHealthPulseSpeed);
    }

    public static Color GetLowHealthWarningColor(float pulse)
    {
        return new Color(
            0.95f,
            0.025f,
            0.035f,
            Mathf.Lerp(0.08f, 0.34f, Mathf.Clamp01(pulse)));
    }

    public static float CalculatePlayerHealthRatio(
        int health,
        int maximumHealth)
    {
        return Mathf.Clamp01(
            (float)Mathf.Max(0, health) /
            Mathf.Max(1, maximumHealth));
    }

    public static Color GetPlayerHealthColor(float healthRatio)
    {
        if (healthRatio <= 0.25f)
        {
            return new Color(0.95f, 0.12f, 0.12f, 0.98f);
        }

        if (healthRatio <= 0.5f)
        {
            return new Color(1f, 0.5f, 0.08f, 0.98f);
        }

        return new Color(0.12f, 0.82f, 0.32f, 0.98f);
    }

    void UpdateChampionHealthBar(
        int eliteCount,
        int eliteCurrentHealth,
        int eliteMaximumHealth,
        int championCount,
        int currentHealth,
        int maximumHealth,
        int finalBossCount,
        int finalBossCurrentHealth,
        int finalBossMaximumHealth)
    {
        if (championHealthRoot == null)
        {
            return;
        }

        var hasFinalBoss =
            finalBossCount > 0 &&
            finalBossMaximumHealth > 0;
        var hasChampion =
            championCount > 0 &&
            maximumHealth > 0;
        var hasElite =
            eliteCount > 0 &&
            eliteMaximumHealth > 0;
        var isVisible =
            hasFinalBoss ||
            hasChampion ||
            hasElite;
        championHealthRoot.SetActive(isVisible);
        if (!isVisible)
        {
            return;
        }

        var displayedCurrentHealth = hasFinalBoss
            ? finalBossCurrentHealth
            : hasChampion
                ? currentHealth
                : eliteCurrentHealth;
        var displayedMaximumHealth = hasFinalBoss
            ? finalBossMaximumHealth
            : hasChampion
                ? maximumHealth
                : eliteMaximumHealth;
        var healthRatio = Mathf.Clamp01(
            (float)Mathf.Max(0, displayedCurrentHealth) /
            Mathf.Max(1, displayedMaximumHealth));
        championHealthFill.sizeDelta = new Vector2(
            (ChampionBarWidth - 6f) * healthRatio,
            -6f);
        if (championHealthFillImage != null)
        {
            championHealthFillImage.color = hasFinalBoss
                ? new Color(0.92f, 0.08f, 0.12f, 0.98f)
                : hasChampion
                    ? new Color(0.72f, 0.08f, 1f, 0.95f)
                    : new Color(1f, 0.36f, 0.06f, 0.95f);
        }

        championHealthLabel.text = FormatThreatHealthLabel(
            eliteCount,
            eliteCurrentHealth,
            eliteMaximumHealth,
            championCount,
            currentHealth,
            maximumHealth,
            finalBossCount,
            finalBossCurrentHealth,
            finalBossMaximumHealth);
    }

    public static string FormatThreatHealthLabel(
        int eliteCount,
        int eliteCurrentHealth,
        int eliteMaximumHealth,
        int championCount,
        int championCurrentHealth,
        int championMaximumHealth,
        int finalBossCount,
        int finalBossCurrentHealth,
        int finalBossMaximumHealth)
    {
        if (finalBossCount > 0 && finalBossMaximumHealth > 0)
        {
            return
                $"最終ボス   {Mathf.Max(0, finalBossCurrentHealth)}/" +
                $"{Mathf.Max(1, finalBossMaximumHealth)}";
        }

        if (championCount > 0 && championMaximumHealth > 0)
        {
            return championCount > 1
                ? $"チャンピオン x{championCount}   " +
                    $"{Mathf.Max(0, championCurrentHealth)}/" +
                    $"{Mathf.Max(1, championMaximumHealth)}"
                : $"チャンピオン   {Mathf.Max(0, championCurrentHealth)}/" +
                    $"{Mathf.Max(1, championMaximumHealth)}";
        }

        return eliteCount > 1
            ? $"エリート x{eliteCount}   " +
                $"{Mathf.Max(0, eliteCurrentHealth)}/" +
                $"{Mathf.Max(1, eliteMaximumHealth)}"
            : $"エリート   {Mathf.Max(0, eliteCurrentHealth)}/" +
                $"{Mathf.Max(1, eliteMaximumHealth)}";
    }

    void ApplyJapaneseFont(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        if (japaneseFont != null)
        {
            text.font = japaneseFont;
        }
        else if (unitText != null && unitText.font != null)
        {
            text.font = unitText.font;
        }
    }

    void PrepareJapaneseFont()
    {
        if (japaneseFont == null)
        {
            Debug.LogWarning(
                $"HUD用フォントが見つかりません: {JapaneseFontResourcePath}",
                this);
            return;
        }

        japaneseFont.isMultiAtlasTexturesEnabled = true;
        if (!japaneseFont.TryAddCharacters(
                RequiredHudCharacters,
                out var missingCharacters,
                true) &&
            !string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogWarning(
                $"HUD用フォントに追加できない文字があります: {missingCharacters}",
                this);
        }
    }

    static void ConfigureText(
        TMP_Text text,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize)
    {
        if (text == null)
        {
            return;
        }

        var rectTransform = text.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = Vector2.zero;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        text.alignment = TextAlignmentOptions.BottomLeft;
        text.fontSize = fontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.lineSpacing = 0f;
        text.raycastTarget = false;
    }
}
