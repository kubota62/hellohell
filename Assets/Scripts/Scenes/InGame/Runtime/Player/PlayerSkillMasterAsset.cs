using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// レベルアップ時に選ばれるプレイヤースキルを管理するScriptableObject。
/// DisplayNameとDescriptionは将来のスキル選択UI用で、ECS側の高速処理にはIDと数値だけを渡す。
/// </summary>
[CreateAssetMenu(menuName = "HelloHell/Masters/Player Skill Master")]
public class PlayerSkillMasterAsset : ScriptableObject
{
    public PlayerSkillMasterId Id = PlayerSkillMasterId.DamageBoost;
    public string DisplayName = "Damage Boost";
    [TextArea]
    public string Description = "攻撃の性能を強化する。";
    public PlayerSkillKind Kind = PlayerSkillKind.Damage;
    public int AddLevel = 1;
    public int MaxLevel = 0;
    public float EffectPerLevel = 0f;
    public int Weight = 1;

    public PlayerSkillMasterData ToRuntimeMaster()
    {
        var fallback = PlayerSkillMasterCatalog.Get(Id);
        return new PlayerSkillMasterData
        {
            Id = Id,
            Kind = Kind,
            AddLevel = AddLevel > 0 ? AddLevel : fallback.AddLevel,
            MaxLevel = MaxLevel > 0 ? MaxLevel : fallback.MaxLevel,
            EffectPerLevel = EffectPerLevel > 0f ? EffectPerLevel : fallback.EffectPerLevel,
            Weight = math.max(0, Weight),
        };
    }
}
