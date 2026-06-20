using Unity.Entities;

public struct SyncColor : IBufferElementData
{
    public Entity SyncTarget;
}
