

# Entitas-Sync-Framework

## Features
- Automatic ECS world synchronization
- Client-server networking model
- Command messaging system
- Code generator based on T4 templating to create neat API, serializers, deserializers and compressors
- State buffering on the client
- [Uses ENet for networking](https://github.com/nxrighthere/ENet-CSharp)
- Native memory allocations
- Networking is handled by a separate thread, lockless communication with that thread
- Simple logger

## Overview
The framework is targeted at slow-paced genres. Gameplay should be delay-tolerant, as clients use state buffering to smoothly display ECS world changes. 
All packets are sent reliably using a single channel. It means you should not set tickrate too high. Otherwise single dropped packet will block all other already received packets from being executed and in extreme cases state queue will fail to smooth those pauses, producing visible stutter. 

| Overview  |  Creating networking command | Creating networking component | 
|--|--|--|
| [![][preview1]](https://www.youtube.com/watch?v=ACZ2bZECRfE) | [![][preview2]](https://www.youtube.com/watch?v=zoPJMG5a84A) | [![][preview3]](https://www.youtube.com/watch?v=GD5dm4FjkOQ) |

### Server
On the server each client has entity with a **Connection**, **ConnectionPeer** and **ClientDataBuffer** components. 
When you tell the server to enqueue command for a particular client - it is written into BitBuffer inside **ClientDataBuffer** component. 

Each tick server will execute all received Commands, then it will execute all gameplay systems.
After that, all changes to the ECS world are captured by reactive systems and written to the 4 bitbuffers which are common for all clients. **Only entities with Sync component attached are handled by automatic synchronization**

The last system for each client combines 1 personal and 4 common BitBuffers into a single byte array, copies data to native memory and publishes a request for a network thread to send data from that native memory to peer from **ConnectionPeer** component. After that, all BitBuffers are cleared.

All gameplay logic should be located inside ServerFeature.

### Client
The client uses state queue to smooth out ping jitter. 
The only way for the client to send something to the server is using Commands. 
When you tell the client to enqueue command - it is written into BitBuffer inside **ClientNetworkSystem**. 

Each tick client will execute all received Commands, then it will execute all gameplay systems.
After that, it will send all Commands which were enqueued.

The client knows the whole world all the time. In the first packet he receives all entities with Sync component and all their networking components. After that, he will only receive changes which happened in the world.

To react to changes in ECS world you can add systems into ClientFeature. You should not modify networking components in those systems or destroy/create entities with Sync attribute.

## Commands

Both client and server can enqueue commands. You create a struct, set data into fields and call `_server.Enqueue*(command)` or `_client.Enqueue*(command)`. Then that command will be received on connected peer/peers.

To create a new command:
- Create class or struct and mark it with CommandToServer or CommandToClient attribute
- Generate code
- Implement newly generated method from IServerHandler or IClientHandler

### Supported attributes
- CommandToServer - Should be applied on a type whose fields and name will represent a command which will be sent to the server. Type marked with that attribute is called **scheme**.
- CommandToClient - Should be applied to a type whose fields and name will represent a command which will be sent to the client.
- BoundedFloat(*min, max, precision*) - Should be applied on a field with float type inside the scheme. Example usage `[BoundedFloat(-1, 1, 0.01f) public float Value;` In that case values will be `-1.00, -0.99, -0.98,..., 0.98, 0.99, 1.00`
- BoundedVector2(*xMin, xMax, xPrecision, yMin, yMax, yPrecision*) - Should be applied on a field with UnityEngine.Vector2 type inside the scheme. Under the hood works like 2 float fields with BoundedFloat attributes.
- BoundedVector3(*xMin, xMax, xPrecision, yMin, yMax, yPrecision, zMin, zMax, zPrecision*)  - Should be applied on a field with UnityEngine.Vector3 type inside the scheme.

## Entity Synchronization 

All entities marked with Sync component will be sent to clients. Only networking components on those entities will be sent to the clients. Everything is handled automatically. You should not add/remove WasSync component from entities, as it will break logic and desync will happen.

Make sure to use only supported field types.

To create new networking component:
- Create regular Entitas component
- Add `[Sync]` attribute to the type
- Generate code

### Special components
- Connection - Contains client Id
- ConnectionPeer - Contains ENet.Peer struct, which is used to send packets to
- ClientDataBuffer  - Contains ushort and BitBuffer fields to represent the count of personal commands and their serialized data
- Id - Automatically attached to all created entities on the server. Highly used by network layer to find entities.
- Sync - Only those entities, which have Sync component attached will be synced to the clients. At the end of the tick when Sync component was added runs reactive system (**ServerCaptureCreatedEntitiesSystem**) to serialize that entity with all networking components and then that entity is marked with WasSynced component.
- WasSynced - All changes which happened to an entity with Sync AND WasSynced components will be serialized by generated systems. If an entity with WasSynced component receives Destroyed component, then is it processed by system (**ServerCaptureRemovedEntitiesSystem**), which serializes that entity as removed one.
- RequresWorldState - When the client is connected, the entity which represents his connection receives that component. Then the reactive system is triggered to serialize whole world state (**ServerCreateWorldStateSystem**)
- WorldState - When (**ServerCreateWorldStateSystem**) is triggered, then it creates one entity with reused BitBuffer, which contains all networking entities with all networking components on the server. That entity is removed at the end of the frame. If two clients connect at the same server tick, then both of them will use a single world state.
### Supported attributes
- Sync - Should be applied to a partial class, which is Entitas component. The class marked with that attribute is called **networking component**. Networking code is generated only for those components, which are marked with that attribute.
- BoundedFloat(*min, max, precision*) - Should be applied on a field with float type inside a networking component. Example usage `[BoundedFloat(-1, 1, 0.01f) public float Value;` In that case values will be `-1.00, -0.99, -0.98,..., 0.98, 0.99, 1.00`
- BoundedVector2(*xMin, xMax, xPrecision, yMin, yMax, yPrecision*) - Should be applied on a field with UnityEngine.Vector2 type inside a networking component. Under the hood works like 2 float fields with BoundedFloat attributes.
- BoundedVector3(*xMin, xMax, xPrecision, yMin, yMax, yPrecision, zMin, zMax, zPrecision*)  - Should be applied on a field with UnityEngine.Vector3 type inside a networking component.

## Field types supported
- byte
- int
- uint
- long
- ulong
- short
- ushort
- string
- boolean
- float
- UnityEngine.Vector2
- UnityEngine.Vector3
- non-flag enums

## Dependencies
- Entitas ECS 1.13.0
- Unity 2019.1
- ENet CSharp 2.2.6 

[preview1]: https://i.imgur.com/H4a5rTv.png
[preview2]: https://i.imgur.com/o0etJ5k.png
[preview3]: https://i.imgur.com/lty8gA0.png
