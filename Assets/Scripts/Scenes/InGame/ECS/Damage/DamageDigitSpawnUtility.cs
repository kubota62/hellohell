using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public static class DamageDigitSpawnUtility
{
    public const float DigitSpacing = 0.7f;

    public static void SpawnDamageDigits(
        EntityCommandBuffer ecb,
        Entity digitPrefab,
        int damage,
        float3 worldPosition)
    {
        if (digitPrefab == Entity.Null)
            return;

        var digits = new FixedList32Bytes<int>();
        var value = math.abs(damage);

        if (value == 0)
        {
            digits.Add(0);
        }
        else
        {
            while (value > 0)
            {
                digits.Add(value % 10);
                value /= 10;
            }
        }

        var count = digits.Length;
        for (var i = 0; i < count; i++)
        {
            var digitValue = digits[i];
            var columnIndex = digitValue == 0 ? 9 : digitValue - 1;
            var horizontalOffset = ((count - 1 - i) - (count - 1) * 0.5f) * DigitSpacing;

            var entity = ecb.Instantiate(digitPrefab);
            ecb.AddComponent(entity, LocalTransform.FromPosition(worldPosition));
            ecb.AddComponent(entity, new DamageDigit
            {
                StartPosition = worldPosition,
                HorizontalOffset = horizontalOffset,
                Elapsed = 0f,
                Lifetime = 0.4f,
            });
            ecb.AddComponent(entity, new DigitIndexProperty { Value = columnIndex });
            ecb.AddComponent(entity, new URPMaterialPropertyBaseColor
            {
                Value = new float4(1f, 1f, 1f, 1f),
            });
        }
    }
}

