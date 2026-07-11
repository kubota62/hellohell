using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// プレイヤー成長マスタを参照するための軽量ID。
/// 将来、キャラクターや難易度ごとに成長曲線を切り替える入口にする。
/// </summary>
public enum PlayerProgressMasterId
{
    Default = 1,
}

/// <summary>
/// プレイヤーの初期レベルと、次レベルまでの必要経験値曲線。
/// </summary>
public struct PlayerProgressMasterData
{
    public PlayerProgressMasterId Id;
    public int InitialLevel;
    public int InitialExperienceToNextLevel;
    public int ExperienceToNextLevelAdd;
}

/// <summary>
/// BakerがScriptableObjectの成長マスタをECS側へ渡すためのバッファ要素。
/// </summary>
public struct PlayerProgressMasterElement : IBufferElementData
{
    public PlayerProgressMasterId Id;
    public int InitialLevel;
    public int InitialExperienceToNextLevel;
    public int ExperienceToNextLevelAdd;

    public static PlayerProgressMasterElement FromMaster(PlayerProgressMasterData master)
    {
        return new PlayerProgressMasterElement
        {
            Id = master.Id,
            InitialLevel = master.InitialLevel,
            InitialExperienceToNextLevel = master.InitialExperienceToNextLevel,
            ExperienceToNextLevelAdd = master.ExperienceToNextLevelAdd,
        };
    }

    public PlayerProgressMasterData ToRuntimeMaster()
    {
        return new PlayerProgressMasterData
        {
            Id = Id,
            InitialLevel = math.max(1, InitialLevel),
            InitialExperienceToNextLevel = math.max(1, InitialExperienceToNextLevel),
            ExperienceToNextLevelAdd = math.max(0, ExperienceToNextLevelAdd),
        };
    }
}
