using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Attack/Variants/MeleeArc の表示専用システム。
/// MeleeArcVfxRequest を消費し、半透明の扇形メッシュを短時間表示する。
/// ゲームロジックは ECS のリクエストだけを扱い、表示は GameObject 側に閉じ込める。
/// </summary>
[UpdateAfter(typeof(MeleeArcAttackSystem))]
public partial class MeleeArcVfxSystem : SystemBase
{
    private const int SegmentCount = 24;
    private static Material sharedMaterial;

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (request, entity) in
                 SystemAPI.Query<RefRO<MeleeArcVfxRequest>>()
                     .WithEntityAccess())
        {
            SpawnArc(request.ValueRO);
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private static void SpawnArc(MeleeArcVfxRequest request)
    {
        var root = new GameObject("MeleeArcVfx");
        root.transform.position = new Vector3(request.Position.x, request.Position.y + 0.06f, request.Position.z);
        root.transform.rotation = Quaternion.LookRotation(ToVector3(request.Direction), Vector3.up);

        var meshFilter = root.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateArcMesh(request.Radius, request.AngleDegrees);

        var meshRenderer = root.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = GetMaterial(request.Color);

        Object.Destroy(root, math.max(0.01f, request.Duration));
    }

    private static Mesh CreateArcMesh(float radius, float angleDegrees)
    {
        var clampedAngle = math.clamp(angleDegrees, 1f, 360f);
        var vertexCount = SegmentCount + 2;
        var vertices = new Vector3[vertexCount];
        var triangles = new int[SegmentCount * 6];
        var halfAngle = clampedAngle * 0.5f;

        vertices[0] = Vector3.zero;
        for (var i = 0; i <= SegmentCount; i++)
        {
            var t = (float)i / SegmentCount;
            var angle = math.radians(-halfAngle + clampedAngle * t);
            vertices[i + 1] = new Vector3(math.sin(angle) * radius, 0f, math.cos(angle) * radius);
        }

        for (var i = 0; i < SegmentCount; i++)
        {
            var triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;

            var reverseTriangleIndex = SegmentCount * 3 + triangleIndex;
            triangles[reverseTriangleIndex] = 0;
            triangles[reverseTriangleIndex + 1] = i + 2;
            triangles[reverseTriangleIndex + 2] = i + 1;
        }

        var mesh = new Mesh
        {
            name = "MeleeArcMesh",
            vertices = vertices,
            triangles = triangles,
        };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material GetMaterial(float4 color)
    {
        if (sharedMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Standard");
            sharedMaterial = new Material(shader)
            {
                name = "MeleeArcVfxMaterial",
            };
            ConfigureTransparentMaterial(sharedMaterial);
        }

        SetMaterialColor(sharedMaterial, color);
        return sharedMaterial;
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void SetMaterialColor(Material material, float4 color)
    {
        var unityColor = new Color(color.x, color.y, color.z, color.w);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", unityColor);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", unityColor);
        }
    }

    private static Vector3 ToVector3(float3 value)
    {
        return new Vector3(value.x, value.y, value.z);
    }
}
