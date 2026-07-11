using Unity.Entities;

/// <summary>
/// Masters/Player のID定義。
/// Player定義を参照するための軽量ID。
/// 将来プレイアブルキャラが増えた場合も、このIDを選択結果として扱う。
/// </summary>
public enum PlayerMasterId
{
    Default = 1,
}

/// <summary>
/// Playerの初期装備や基礎能力をまとめたランタイム用マスタ値。
/// レベルアップで変化する値はPlayerSkillMaster、経験値曲線はPlayerProgressMasterへ分ける。
/// </summary>
public struct PlayerMasterData
{
    public PlayerMasterId Id;
    public AttackMasterId PrimaryAttack;
    public float MoveSpeed;
}

/// <summary>
/// BakerがScriptableObjectのPlayerマスタをECS側へ渡すためのバッファ要素。
/// </summary>
public struct PlayerMasterElement : IBufferElementData
{
    public PlayerMasterId Id;
    public AttackMasterId PrimaryAttack;
    public float MoveSpeed;

    public static PlayerMasterElement FromMaster(PlayerMasterData master)
    {
        return new PlayerMasterElement
        {
            Id = master.Id,
            PrimaryAttack = master.PrimaryAttack,
            MoveSpeed = master.MoveSpeed,
        };
    }

    public PlayerMasterData ToRuntimeMaster()
    {
        return new PlayerMasterData
        {
            Id = Id,
            PrimaryAttack = PrimaryAttack,
            MoveSpeed = MoveSpeed,
        };
    }
}
