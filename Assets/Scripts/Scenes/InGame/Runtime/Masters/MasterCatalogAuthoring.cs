using Unity.Entities;
using UnityEngine;

/// <summary>
/// Runtime/Masters 直下の全マスタをシーンからECSへ渡す集約入口。
/// ScriptableObjectの定義一覧をECSの定義バッファへ焼き込むAuthoring。
/// シーンに置くと、スポーンや攻撃生成は静的カタログではなくこの定義を優先して読む。
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

    class Baker : Baker<MasterCatalogAuthoring>
    {
        public override void Bake(MasterCatalogAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent<MasterCatalogTag>(entity);

            // 攻撃マスタはPlayer/EnemyのLoadoutから参照されるため、未設定でも既定値を必ず焼き込む。
            var attackBuffer = AddBuffer<AttackMasterElement>(entity);
            if (authoring.AttackMasters is { Length: > 0 })
            {
                foreach (var asset in authoring.AttackMasters)
                {
                    if (asset == null) continue;
                    attackBuffer.Add(AttackMasterElement.FromMaster(asset.ToRuntimeMaster()));
                }
            }

            EnsureAttackMaster(attackBuffer, AttackMasterId.BasicProjectile);
            EnsureAttackMaster(attackBuffer, AttackMasterId.BasicAura);
            EnsureAttackMaster(attackBuffer, AttackMasterId.BasicMeleeArc);
            EnsureAttackMaster(attackBuffer, AttackMasterId.BasicChainProjectile);

            // 敵マスタはスポーン時に身体・移動・攻撃・報酬へ分解して適用する。
            var enemyBuffer = AddBuffer<EnemyMasterElement>(entity);
            if (authoring.EnemyMasters is { Length: > 0 })
            {
                foreach (var asset in authoring.EnemyMasters)
                {
                    if (asset == null) continue;
                    enemyBuffer.Add(EnemyMasterElement.FromMaster(asset.ToRuntimeMaster()));
                }
            }

            EnsureEnemyMaster(enemyBuffer, EnemyMasterCatalog.ForwardEnemy);
            EnsureEnemyMaster(enemyBuffer, EnemyMasterCatalog.RandomEnemy);
            EnsureEnemyMaster(enemyBuffer, EnemyMasterCatalog.ChainEnemy);
            EnsureEnemyMaster(enemyBuffer, EnemyMasterCatalog.KiteEnemy);

            // スポーン間隔や出現距離はゲーム全体の進行値なので、専用のマスタとして扱う。
            var spawnBuffer = AddBuffer<SpawnMasterElement>(entity);
            if (authoring.SpawnMasters is { Length: > 0 })
            {
                foreach (var asset in authoring.SpawnMasters)
                {
                    if (asset == null) continue;
                    spawnBuffer.Add(SpawnMasterElement.FromMaster(asset.ToRuntimeMaster()));
                }
            }

            EnsureSpawnMaster(spawnBuffer, SpawnMasterId.Default);

            // Player本体の基礎値。成長値やスキル効果とは分けて、キャラ差し替えしやすくする。
            var playerBuffer = AddBuffer<PlayerMasterElement>(entity);
            if (authoring.PlayerMasters is { Length: > 0 })
            {
                foreach (var asset in authoring.PlayerMasters)
                {
                    if (asset == null) continue;
                    playerBuffer.Add(PlayerMasterElement.FromMaster(asset.ToRuntimeMaster()));
                }
            }

            EnsurePlayerMaster(playerBuffer, PlayerMasterId.Default);

            // レベルアップ時の候補。将来の選択UIもこのバッファを候補リストとして読める。
            var playerSkillBuffer = AddBuffer<PlayerSkillMasterElement>(entity);
            if (authoring.PlayerSkillMasters is { Length: > 0 })
            {
                foreach (var asset in authoring.PlayerSkillMasters)
                {
                    if (asset == null) continue;
                    playerSkillBuffer.Add(PlayerSkillMasterElement.FromMaster(asset.ToRuntimeMaster()));
                }
            }

            EnsurePlayerSkillMaster(playerSkillBuffer, PlayerSkillMasterId.DamageBoost);
            EnsurePlayerSkillMaster(playerSkillBuffer, PlayerSkillMasterId.AttackSpeedBoost);
            EnsurePlayerSkillMaster(playerSkillBuffer, PlayerSkillMasterId.MoveSpeedBoost);

            // 経験値カーブ。Player生成時の初期値と、レベルアップ後の次要求値に使う。
            var playerProgressBuffer = AddBuffer<PlayerProgressMasterElement>(entity);
            if (authoring.PlayerProgressMasters is { Length: > 0 })
            {
                foreach (var asset in authoring.PlayerProgressMasters)
                {
                    if (asset == null) continue;
                    playerProgressBuffer.Add(PlayerProgressMasterElement.FromMaster(asset.ToRuntimeMaster()));
                }
            }

            EnsurePlayerProgressMaster(playerProgressBuffer, PlayerProgressMasterId.Default);
        }

        private static void EnsureAttackMaster(
            DynamicBuffer<AttackMasterElement> attackBuffer,
            AttackMasterId id)
        {
            for (var i = 0; i < attackBuffer.Length; i++)
            {
                if (attackBuffer[i].Id == id)
                {
                    return;
                }
            }

            attackBuffer.Add(AttackMasterElement.FromMaster(AttackMasterCatalog.Get(id)));
        }

        private static void EnsureEnemyMaster(
            DynamicBuffer<EnemyMasterElement> enemyBuffer,
            int typeId)
        {
            for (var i = 0; i < enemyBuffer.Length; i++)
            {
                if (enemyBuffer[i].TypeId == typeId)
                {
                    return;
                }
            }

            enemyBuffer.Add(EnemyMasterElement.FromMaster(EnemyMasterCatalog.Get(typeId)));
        }

        private static void EnsureSpawnMaster(
            DynamicBuffer<SpawnMasterElement> spawnBuffer,
            SpawnMasterId id)
        {
            for (var i = 0; i < spawnBuffer.Length; i++)
            {
                if (spawnBuffer[i].Id == id)
                {
                    return;
                }
            }

            spawnBuffer.Add(SpawnMasterElement.FromMaster(SpawnMasterCatalog.Get(id)));
        }

        private static void EnsurePlayerMaster(
            DynamicBuffer<PlayerMasterElement> playerBuffer,
            PlayerMasterId id)
        {
            for (var i = 0; i < playerBuffer.Length; i++)
            {
                if (playerBuffer[i].Id == id)
                {
                    return;
                }
            }

            playerBuffer.Add(PlayerMasterElement.FromMaster(PlayerMasterCatalog.Get(id)));
        }

        private static void EnsurePlayerSkillMaster(
            DynamicBuffer<PlayerSkillMasterElement> playerSkillBuffer,
            PlayerSkillMasterId id)
        {
            for (var i = 0; i < playerSkillBuffer.Length; i++)
            {
                if (playerSkillBuffer[i].Id == id)
                {
                    return;
                }
            }

            playerSkillBuffer.Add(PlayerSkillMasterElement.FromMaster(PlayerSkillMasterCatalog.Get(id)));
        }

        private static void EnsurePlayerProgressMaster(
            DynamicBuffer<PlayerProgressMasterElement> playerProgressBuffer,
            PlayerProgressMasterId id)
        {
            for (var i = 0; i < playerProgressBuffer.Length; i++)
            {
                if (playerProgressBuffer[i].Id == id)
                {
                    return;
                }
            }

            playerProgressBuffer.Add(PlayerProgressMasterElement.FromMaster(PlayerProgressMasterCatalog.Get(id)));
        }
    }
}
