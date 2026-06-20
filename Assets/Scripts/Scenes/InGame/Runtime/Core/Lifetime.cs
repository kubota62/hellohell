using Unity.Entities;

/// <summary>
/// Projectile や VFX リクエストなど、一時エンティティの残り寿命秒数。
/// 将来のプール対応システムでは、0 到達時に破棄ではなく無効化へ差し替えられる。
/// </summary>
public struct Lifetime : IComponentData
{
    public float Remaining;
}
