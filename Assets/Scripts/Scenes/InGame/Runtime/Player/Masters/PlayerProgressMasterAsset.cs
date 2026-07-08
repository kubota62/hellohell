using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// プレイヤーのレベルアップ曲線を管理するScriptableObject。
/// コードを触らずに、初期必要経験値やレベルごとの増加量を調整できるようにする。
/// </summary>
[CreateAssetMenu(menuName = "HelloHell/Masters/Player Progress Master")]
public class PlayerProgressMasterAsset : ScriptableObject
{
    public PlayerProgressMasterId Id = PlayerProgressMasterId.Default;
    public int InitialLevel = 1;
    public int InitialExperienceToNextLevel = 5;
    public int ExperienceToNextLevelAdd = 3;

    public PlayerProgressMasterData ToRuntimeMaster()
    {
        var fallback = PlayerProgressMasterCatalog.Get(Id);
        return new PlayerProgressMasterData
        {
            Id = Id,
            InitialLevel = InitialLevel > 0 ? InitialLevel : fallback.InitialLevel,
            InitialExperienceToNextLevel = InitialExperienceToNextLevel > 0
                ? InitialExperienceToNextLevel
                : fallback.InitialExperienceToNextLevel,
            ExperienceToNextLevelAdd = math.max(0, ExperienceToNextLevelAdd),
        };
    }
}
