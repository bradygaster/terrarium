using System.Runtime.Serialization;
using System.ServiceModel;

namespace Terrarium.Grpc;

/// <summary>
/// Server-to-server gRPC service contract for future multi-server scaling.
/// NOT for client communication — clients use SignalR via TerrariumHub.
/// Code-first using protobuf-net.Grpc for .NET-native ergonomics.
/// </summary>
[ServiceContract(Name = "terrarium.ServerSync")]
public interface IServerSyncService
{
    /// <summary>
    /// Transfers a creature between server instances during cross-silo teleportation.
    /// </summary>
    [OperationContract]
    Task<TeleportResult> TransferCreatureAsync(ServerTeleportRequest request);

    /// <summary>
    /// Syncs population data between server instances for aggregate reporting.
    /// </summary>
    [OperationContract]
    Task<PopulationSyncResult> SyncPopulationAsync(PopulationSyncRequest request);

    /// <summary>
    /// Health check / heartbeat between server instances.
    /// </summary>
    [OperationContract]
    Task<ServerHeartbeatResponse> HeartbeatAsync(ServerHeartbeatRequest request);

    /// <summary>
    /// Requests the list of ecosystems hosted on a server instance.
    /// </summary>
    [OperationContract]
    Task<EcosystemListResponse> ListEcosystemsAsync(EcosystemListRequest request);
}

[DataContract]
public sealed class ServerTeleportRequest
{
    [DataMember(Order = 1)]
    public string TeleportId { get; set; } = "";

    [DataMember(Order = 2)]
    public string OrganismId { get; set; } = "";

    [DataMember(Order = 3)]
    public string SpeciesAssemblyName { get; set; } = "";

    [DataMember(Order = 4)]
    public string StatePayload { get; set; } = "";

    [DataMember(Order = 5)]
    public byte[]? AssemblyPayload { get; set; }

    [DataMember(Order = 6)]
    public string SourceServerId { get; set; } = "";

    [DataMember(Order = 7)]
    public string TargetEcosystemId { get; set; } = "";
}

[DataContract]
public sealed class TeleportResult
{
    [DataMember(Order = 1)]
    public bool Success { get; set; }

    [DataMember(Order = 2)]
    public string? ErrorMessage { get; set; }

    [DataMember(Order = 3)]
    public string? TargetConnectionId { get; set; }
}

[DataContract]
public sealed class PopulationSyncRequest
{
    [DataMember(Order = 1)]
    public string ServerId { get; set; } = "";

    [DataMember(Order = 2)]
    public string EcosystemId { get; set; } = "";

    [DataMember(Order = 3)]
    public long TickNumber { get; set; }

    [DataMember(Order = 4)]
    public List<SpeciesCount> Species { get; set; } = [];
}

[DataContract]
public sealed class SpeciesCount
{
    [DataMember(Order = 1)]
    public string SpeciesName { get; set; } = "";

    [DataMember(Order = 2)]
    public int Count { get; set; }
}

[DataContract]
public sealed class PopulationSyncResult
{
    [DataMember(Order = 1)]
    public bool Acknowledged { get; set; }
}

[DataContract]
public sealed class ServerHeartbeatRequest
{
    [DataMember(Order = 1)]
    public string ServerId { get; set; } = "";

    [DataMember(Order = 2)]
    public int ActiveEcosystems { get; set; }

    [DataMember(Order = 3)]
    public int ConnectedPeers { get; set; }
}

[DataContract]
public sealed class ServerHeartbeatResponse
{
    [DataMember(Order = 1)]
    public bool Healthy { get; set; }

    [DataMember(Order = 2)]
    public DateTimeOffset ServerTime { get; set; }
}

[DataContract]
public sealed class EcosystemListRequest
{
    [DataMember(Order = 1)]
    public string RequestingServerId { get; set; } = "";
}

[DataContract]
public sealed class EcosystemListResponse
{
    [DataMember(Order = 1)]
    public List<EcosystemSummary> Ecosystems { get; set; } = [];
}

[DataContract]
public sealed class EcosystemSummary
{
    [DataMember(Order = 1)]
    public string EcosystemId { get; set; } = "";

    [DataMember(Order = 2)]
    public int PeerCount { get; set; }

    [DataMember(Order = 3)]
    public long CurrentTick { get; set; }
}
