using Unity.Entities;

/// <summary>
/// Playerマスタへアクセスするための既定カタログ。
/// MasterCatalogAuthoringが未配置でも、既定Playerでゲームが動くようにする。
/// </summary>
public static class PlayerMasterCatalog
{
    public static PlayerMasterData Get(PlayerMasterId id)
    {
        return new PlayerMasterData
        {
            Id = PlayerMasterId.Default,
            PrimaryAttack = AttackMasterId.BasicMeleeArc,
            MoveSpeed = 5f,
        };
    }

    public static PlayerMasterData Get(
        DynamicBuffer<PlayerMasterElement> masters,
        PlayerMasterId id)
    {
        for (var i = 0; i < masters.Length; i++)
        {
            if (masters[i].Id == id)
            {
                return Sanitize(masters[i].ToRuntimeMaster());
            }
        }

        return Get(id);
    }

    private static PlayerMasterData Sanitize(PlayerMasterData master)
    {
        var fallback = Get(master.Id);
        master.MoveSpeed = master.MoveSpeed > 0f ? master.MoveSpeed : fallback.MoveSpeed;
        return master;
    }
}
