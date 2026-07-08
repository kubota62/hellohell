using UnityEngine;

/// <summary>
/// Playerの初期装備や基礎能力を管理するScriptableObject。
/// キャラ差し替えや初期武器変更は、Systemではなくこのマスタを編集して行う。
/// </summary>
[CreateAssetMenu(menuName = "HelloHell/Masters/Player Master")]
public class PlayerMasterAsset : ScriptableObject
{
    public PlayerMasterId Id = PlayerMasterId.Default;
    public AttackMasterId PrimaryAttack = AttackMasterId.BasicMeleeArc;
    public float MoveSpeed = 5f;

    public PlayerMasterData ToRuntimeMaster()
    {
        var fallback = PlayerMasterCatalog.Get(Id);
        return new PlayerMasterData
        {
            Id = Id,
            PrimaryAttack = PrimaryAttack,
            MoveSpeed = MoveSpeed > 0f ? MoveSpeed : fallback.MoveSpeed,
        };
    }
}
