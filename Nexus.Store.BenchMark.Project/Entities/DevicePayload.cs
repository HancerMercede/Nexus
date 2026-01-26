using MessagePack;

namespace Nexus.Store.BenchMark.Project.Entities;

[MessagePackObject]
public record DevicePayload(
    [property: Key(0)] int Id,
    [property: Key(1)] string DeviceName,
    [property: Key(2)] double Value,
    [property: Key(3)] DateTime Timestamp
);