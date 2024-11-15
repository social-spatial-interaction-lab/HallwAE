## Fix Monkey Mask Issue

In cullingMask field of centerEyeAnchor, remove the layer "Mirror".

## Components
- XRAvatarIK: Track head position and rotation.
- XRAvatarVisuals: Update visuals based on head position and rotation.
- LobbyManager: Handle lobby and relay.


## Unity Relay

An "alloc" (allocation) in Unity's Relay service represents a reservation of network resources for your multiplayer game session. Let's break down the allocation concept:

```csharp
// Example of creating a Relay allocation
public async Task<RelayServerData> CreateRelay()
{
    try 
    {
        // Create allocation for max number of players
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

        // The allocation contains:
        string allocationId = allocation.AllocationId;        // Unique identifier
        byte[] allocationBytes = allocation.AllocationIdBytes;// Binary form of ID
        byte[] connectionData = allocation.ConnectionData;    // Data needed to connect
        byte[] key = allocation.Key;                         // Encryption key
        
        // Server info for connection
        RelayServer relayServer = allocation.RelayServer;     
        string ipv4Address = relayServer.IpV4;               // Server IP
        ushort port = (ushort)relayServer.Port;             // Server port
        string region = allocation.Region;                   // Server region

        // Get join code for other players
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocationId);

        return new RelayServerData(...)
    }
    catch (RelayServiceException e)
    {
        Debug.LogError($"Relay allocation failed: {e.Message}");
        throw;
    }
}
```

Key Components of an Allocation:

1. **Network Resources**:
```csharp
// Host creating allocation
var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

// Client joining using allocation
var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
```

2. **Connection Data**:
```csharp
// Setting up host transport
transport.SetHostRelayData(
    allocation.RelayServer.IpV4,           // Server IP
    (ushort)allocation.RelayServer.Port,   // Server port
    allocation.AllocationIdBytes,          // Allocation ID
    allocation.Key,                        // Encryption key
    allocation.ConnectionData              // Connection data
);

// Setting up client transport
transport.SetClientRelayData(
    joinAllocation.RelayServer.IpV4,
    (ushort)joinAllocation.RelayServer.Port,
    joinAllocation.AllocationIdBytes,
    joinAllocation.Key,
    joinAllocation.ConnectionData,
    joinAllocation.HostConnectionData
);
```

3. **Join Codes**:
```csharp
// Host generates join code
string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

// Store in lobby data for others to find
var options = new CreateLobbyOptions
{
    Data = new Dictionary<string, DataObject>
    {
        {
            "RelayJoinCode", 
            new DataObject(
                DataObject.VisibilityOptions.Public, 
                joinCode
            )
        }
    }
};
```

The allocation process:
1. Host requests allocation from Relay service
2. Relay service reserves network resources
3. Host gets connection details and join code
4. Clients use join code to connect to the same allocation
5. All players communicate through the relay server

Think of it like reserving a private room (allocation) in a conference center (relay service):
- The room has a capacity (maxPlayers)
- You get a room number (allocationId)
- You get a key card (connection data)
- You can share an invite code (joinCode)
- The room has an address and directions (server IP/port)

Here's a practical example combining Lobby and Relay:

```csharp
public class NetworkGameManager : MonoBehaviour
{
    private UnityTransport transport;
    
    public async Task CreateNetworkGame()
    {
        try
        {
            // 1. Create Relay allocation
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 2. Setup transport with allocation data
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // 3. Create lobby with relay join code
            var lobbyOptions = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { 
                        "RelayJoinCode", 
                        new DataObject(
                            DataObject.VisibilityOptions.Public, 
                            joinCode
                        )
                    }
                }
            };
            
            var lobby = await LobbyService.Instance.CreateLobbyAsync(
                "My Game", 
                maxPlayers, 
                lobbyOptions
            );

            // 4. Start hosting
            NetworkManager.Singleton.StartHost();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create game: {e.Message}");
        }
    }

    public async Task JoinNetworkGame(string lobbyId)
    {
        try
        {
            // 1. Get lobby data
            var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
            var joinCode = lobby.Data["RelayJoinCode"].Value;

            // 2. Join relay allocation
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // 3. Setup transport with join allocation data
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // 4. Start client
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join game: {e.Message}");
        }
    }
}
```

This system ensures secure and reliable networking through Unity's managed relay servers, rather than direct peer-to-peer connections which can be problematic with firewalls and NAT traversal.