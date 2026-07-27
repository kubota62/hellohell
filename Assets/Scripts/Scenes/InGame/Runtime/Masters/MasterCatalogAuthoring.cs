using Unity.Entities;
using UnityEngine;

/// <summary>
/// ScriptableObjectの各マスターを、実行時に参照するECS Bufferへ変換する。
/// 未登録の標準定義は各Catalogのフォールバック値で補完される。
/// </summary>
public class MasterCatalogAuthoring : MonoBehaviour
{
    [Header("Attack")]
    public AttackMasterAsset[] AttackMasters;

    [Header("Enemy")]
    public EnemyMasterAsset[] EnemyMasters;

    [Header("Spawn")]
    public SpawnMasterAsset[] SpawnMasters;

    [Header("Player")]
    public PlayerMasterAsset[] PlayerMasters;
    public PlayerSkillMasterAsset[] PlayerSkillMasters;
    public PlayerProgressMasterAsset[] PlayerProgressMasters;

    private class Baker : Baker<MasterCatalogAuthoring>
    {
        public override void Bake(MasterCatalogAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent<MasterCatalogTag>(entity);

            BakeAttackMasters(entity, authoring.AttackMasters);
            BakeEnemyMasters(entity, authoring.EnemyMasters);
            BakeSpawnMasters(entity, authoring.SpawnMasters);
            BakePlayerMasters(entity, authoring.PlayerMasters);
            BakePlayerSkillMasters(entity, authoring.PlayerSkillMasters);
            BakePlayerProgressMasters(entity, authoring.PlayerProgressMasters);
        }

        private void BakeAttackMasters(
            Entity entity,
            AttackMasterAsset[] assets)
        {
            var buffer = AddBuffer<AttackMasterElement>(entity);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset != null)
                    {
                        buffer.Add(AttackMasterElement.FromMaster(
                            asset.ToRuntimeMaster()));
                    }
                }
            }

            for (var i = 0; i < AttackMasterIdUtility.All.Length; i++)
            {
                EnsureAttackMaster(buffer, AttackMasterIdUtility.All[i]);
            }
        }

        private void BakeEnemyMasters(
            Entity entity,
            EnemyMasterAsset[] assets)
        {
            var buffer = AddBuffer<EnemyMasterElement>(entity);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset != null)
                    {
                        buffer.Add(EnemyMasterElement.FromMaster(
                            asset.ToRuntimeMaster()));
                    }
                }
            }

            for (var i = 0; i < EnemyMasterCatalog.AllTypeIds.Length; i++)
            {
                EnsureEnemyMaster(buffer, EnemyMasterCatalog.AllTypeIds[i]);
            }
        }

        private void BakeSpawnMasters(
            Entity entity,
            SpawnMasterAsset[] assets)
        {
            var buffer = AddBuffer<SpawnMasterElement>(entity);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset != null)
                    {
                        buffer.Add(SpawnMasterElement.FromMaster(
                            asset.ToRuntimeMaster()));
                    }
                }
            }

            EnsureSpawnMaster(buffer, SpawnMasterId.Default);
        }

        private void BakePlayerMasters(
            Entity entity,
            PlayerMasterAsset[] assets)
        {
            var buffer = AddBuffer<PlayerMasterElement>(entity);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset != null)
                    {
                        buffer.Add(PlayerMasterElement.FromMaster(
                            asset.ToRuntimeMaster()));
                    }
                }
            }

            EnsurePlayerMaster(buffer, PlayerMasterId.Default);
        }

        private void BakePlayerSkillMasters(
            Entity entity,
            PlayerSkillMasterAsset[] assets)
        {
            var buffer = AddBuffer<PlayerSkillMasterElement>(entity);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset != null)
                    {
                        buffer.Add(PlayerSkillMasterElement.FromMaster(
                            asset.ToRuntimeMaster()));
                    }
                }
            }

            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.DamageBoost);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.AttackSpeedBoost);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.MoveSpeedBoost);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.AreaBoost);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.RegenerationBoost);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.MaxHealthBoost);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.PickupRangeBoost);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.MeleeArcMastery);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.RapidBoltMastery);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.PiercingLanceMastery);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.ExplosiveOrbMastery);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.CriticalChanceBoost);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.CriticalDamageBoost);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.ArmorBoost);
            EnsurePlayerSkillMaster(buffer, PlayerSkillMasterId.MultistrikeBoost);
        }

        private void BakePlayerProgressMasters(
            Entity entity,
            PlayerProgressMasterAsset[] assets)
        {
            var buffer = AddBuffer<PlayerProgressMasterElement>(entity);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset != null)
                    {
                        buffer.Add(PlayerProgressMasterElement.FromMaster(
                            asset.ToRuntimeMaster()));
                    }
                }
            }

            EnsurePlayerProgressMaster(buffer, PlayerProgressMasterId.Default);
        }

        private static void EnsureAttackMaster(
            DynamicBuffer<AttackMasterElement> buffer,
            AttackMasterId id)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Id == id)
                {
                    return;
                }
            }

            buffer.Add(AttackMasterElement.FromMaster(AttackMasterCatalog.Get(id)));
        }

        private static void EnsureEnemyMaster(
            DynamicBuffer<EnemyMasterElement> buffer,
            int typeId)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].TypeId == typeId)
                {
                    return;
                }
            }

            buffer.Add(EnemyMasterElement.FromMaster(EnemyMasterCatalog.Get(typeId)));
        }

        private static void EnsureSpawnMaster(
            DynamicBuffer<SpawnMasterElement> buffer,
            SpawnMasterId id)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Id == id)
                {
                    return;
                }
            }

            buffer.Add(SpawnMasterElement.FromMaster(SpawnMasterCatalog.Get(id)));
        }

        private static void EnsurePlayerMaster(
            DynamicBuffer<PlayerMasterElement> buffer,
            PlayerMasterId id)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Id == id)
                {
                    return;
                }
            }

            buffer.Add(PlayerMasterElement.FromMaster(PlayerMasterCatalog.Get(id)));
        }

        private static void EnsurePlayerSkillMaster(
            DynamicBuffer<PlayerSkillMasterElement> buffer,
            PlayerSkillMasterId id)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Id == id)
                {
                    return;
                }
            }

            buffer.Add(PlayerSkillMasterElement.FromMaster(
                PlayerSkillMasterCatalog.Get(id)));
        }

        private static void EnsurePlayerProgressMaster(
            DynamicBuffer<PlayerProgressMasterElement> buffer,
            PlayerProgressMasterId id)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Id == id)
                {
                    return;
                }
            }

            buffer.Add(PlayerProgressMasterElement.FromMaster(
                PlayerProgressMasterCatalog.Get(id)));
        }
    }
}
