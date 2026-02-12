// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Terrarium.Game;

/// <summary>
/// Interface for saving and loading game state.
/// Implementations handle serialization/deserialization of WorldState
/// and coordinate with storage backends (server, browser download, local file).
/// </summary>
public interface IGameStatePersistence
{
    /// <summary>
    /// Serializes the current WorldState to JSON.
    /// </summary>
    /// <param name="state">The world state to serialize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON string representation of the world state.</returns>
    Task<string> SerializeWorldStateAsync(WorldState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes JSON to restore a WorldState.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restored WorldState.</returns>
    Task<WorldState> DeserializeWorldStateAsync(string json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the serialized state to server-side storage.
    /// </summary>
    /// <param name="json">The serialized world state JSON.</param>
    /// <param name="saveName">Name/identifier for this save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if save succeeded.</returns>
    Task<bool> SaveToServerAsync(string json, string saveName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the serialized state as a browser download.
    /// </summary>
    /// <param name="json">The serialized world state JSON.</param>
    /// <param name="fileName">File name for the download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if download triggered successfully.</returns>
    Task<bool> SaveAsBrowserDownloadAsync(string json, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a saved state from the server.
    /// </summary>
    /// <param name="saveName">Name/identifier of the save to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The serialized world state JSON.</returns>
    Task<string?> LoadFromServerAsync(string saveName, CancellationToken cancellationToken = default);
}
