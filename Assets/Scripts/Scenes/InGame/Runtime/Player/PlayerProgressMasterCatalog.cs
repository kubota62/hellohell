using Unity.Entities;

/// <summary>
/// プレイヤー成長マスタへアクセスするための既定カタログ。
/// MasterCatalogAuthoringが未配置でも、従来の必要経験値でゲームを動かす。
/// </summary>
public static class PlayerProgressMasterCatalog
{
    public static PlayerProgressMasterData Get(PlayerProgressMasterId id)
    {
        switch (id)
        {
            case PlayerProgressMasterId.Default:
            default:
                return new PlayerProgressMasterData
                {
                    Id = PlayerProgressMasterId.Default,
                    InitialLevel = 1,
                    InitialExperienceToNextLevel = 5,
                    ExperienceToNextLevelAdd = 3,
                };
        }
    }

    public static PlayerProgressMasterData Get(
        DynamicBuffer<PlayerProgressMasterElement> masters,
        PlayerProgressMasterId id)
    {
        for (var i = 0; i < masters.Length; i++)
        {
            if (masters[i].Id == id)
            {
                return masters[i].ToRuntimeMaster();
            }
        }

        return Get(id);
    }
}
