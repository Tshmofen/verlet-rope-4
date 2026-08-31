# `VerletRopeMesh` Node
**This node is a dedicated visual component that generates and renders a dynamic rope mesh from a provided set of points.** It handles all aspects of the rope's appearance, but contains no physical logic itself - physics to be driven by a simulation provider, which calculates particle positions externally and updates them every frame. 

This node works by generating plane mesh in relation to the current main `Camera3D` position and the distance to it. It will not produce any results without external programmatic access (checkout corresponding section below for details). Also, see properties below for allowed behavior configuration.

<img width=440 src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/example_physics_03.gif"/>

## Usage
This node can be added anywhere using `Create New Node` dialog - here it can be found under `Node3D` -> `VisualInstance3D` -> `GeometryInstance3D` -> `MeshInstance3D` tree.

<img width="440" alt="Verlet Rope Mesh create example" src="https://github.com/user-attachments/assets/cdf463e0-785e-426b-840b-a77d79be5e6f" />

Once `VerletRopeMesh` is in the working tree you are only partially set - only future visuals adjustment is available here. For rope to display actual mesh, you need to push particle positions to it via scripts.

You can see properties descriptions below to see available visuals or refer to `Programmatic Access` to find out how to use it via scripts.

## Export Description
> [!WARNING]
> This node is only needed if you want to write your own custom physics provider for the rope. It will not do anything without corresponding wrapper logic. If you just need the fully-featured rope, please refer to [`VerletRopeSimulated`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated) or [`VerletRopeRigid`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeRigid).

### Visuals
| Export variable | How it works |
|--|--|
| Rope Length                    | Determines total target length of the rope, it is just a base value and actual length might be different depending on physics provider and configured behavior. |
| Rope Width                     |  Determines visual width of the rope, does not affect rope behavior. Ropes are flat, but always look at the camera, so width effectively behaves as a diameter.|
| Subdivision Lod Distance       | If distance to the particle is greater than this value, the corresponding segment is not subdivided for rendering. |
| Use Visible On Screen Notifier | Creates a child `VisibleOnScreenNotifier3D` for the rope when enabled. Is only triggered on `_Ready` calls. |
| Use Debug Particles            | Draws orientation axis from every actual particle positions when enabled. |

## Programmatic Access
The node is designed to be driven by an external simulation. To use it programmatically, you must generate the particle data representing the rope's shape and call its main drawing function every time you want it to be updated (usually every frame).

| Access Point | How it works |
|--|--|
| Method `void DrawRopeParticles(RopeParticleData particles)` | The primary method used to update and render the rope mesh. You must pass in a `RopeParticleData` object containing the current positions of all simulation particles. This method handles all mesh-related generation automatically. |
| Property `bool IsRopeVisible` | A property that returns `true` if the rope is currently visible on screen (requires `UseVisibleOnScreenNotifier` property to be enabled). This can be used to skip simulation logic for off-screen ropes to improve performance. |

### `RopeParticleData` Class
This class is a container for the array of particles that define the rope's shape. It is required to use in the `DrawRopeParticles` method.

| Static Method | How it works |
|--|--|
| `RopeParticleData GenerateParticleData(List<Vector3> particlePositions)` | Generates particle data from a custom list of positions. It allows for complete control over the initial rope shape. |
| `RopeParticleData GenerateParticleData(Vector3 start, Vector3 end, Vector3 initialAcceleration, int particleCount, float segmentLength)` | Generates particle data for a straight rope between two points. |

### `RopeParticle` Struct
This structure holds the data for a single particle in the rope simulation. It is expected to be fetched from `RopeParticleData` as reference and corresponding values to be updated every logical frame.

| Property | Description |
|--|--|
| `Vector3 PositionCurrent`  | The current position of the particle for this frame - used for mesh generation. |
| `Vector3 PositionPrevious` | Bookmark property - the position of the particle from the previous frame - used to calculate velocity. |
| `Vector3 Acceleration`     | Bookmark property - The acceleration applied to this particle (i.e. combined from gravity, wind or any other forces). |
| `bool IsAttached`          | Bookmark property - indicates whether particle's position is locked and not simulated (e.g. for attachment points). |
| `Vector3D Tangent`         | Internal property - provides currently calculated visual tangent particle vector. |
| `Vector3D Normal`          | Internal property - provides currently calculated visual normal particle vector. |
| `Vector3D Binormal`        | Internal property - provides currently calculated visual binormal particle vector. |

> [!NOTE]
> **\*** *All `bookmark` properties are here only for external simulation provider bookkeeping and only indicate how it have to be handled on the following steps, the only property provided from outside that is used by mesh generation is `PositionCurrent`.*  
> **\*** *All `internal` properties are being calculated and assigned by the Mesh generator, they can be fetched and used if needed, but should not be assigned from outside.*

<sup>(TODO: Make providers implement interface/base with only `PositionCurrent`, so that bookmark properties were not exposed when not needed)</sup>

### Simulation Logic

A typical simulation loop in a parent node would look like this:

1.  **Initialize**: Generate the initial `RopeParticleData` in `_Ready()` and prepare Mesh instance, e.g. `_verletRopeMesh`.
2.  **Simulate**: In `_PhysicsProcess(double delta)`, update the `PositionCurrent` and `PositionPrevious` of each particle based on your custom physics, constraints, and collisions.
3.  **Render**: After updating the particle positions, call `_verletRopeMesh.DrawRopeParticles(myParticleData)` to render the current state.

> [!NOTE]
> This design separates the visual representation from the physics logic, allowing to create custom rope behaviors while leveraging the built-in, completely separated mesh generation.

## Related Pages
* [VerletRopeSimulated](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated) - `VerletRopeMesh` is being used internally for visuals rendering.
* [VerletRopeRigid](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeRigid) - `VerletRopeMesh` is being used internally for visuals rendering.

