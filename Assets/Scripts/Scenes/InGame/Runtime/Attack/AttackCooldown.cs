using Unity.Entities;

/// <summary>
/// Actorごとの攻撃クールダウン。
/// 攻撃マスタの Cooldown をここへ反映することで、敵種や武器ごとに発射間隔を変えられる。
/// </summary>
public struct AttackCooldown : IComponentData
{
    public float Remaining;
}
