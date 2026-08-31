# `VerletRopeSimulated` Node
**This node implements dynamic, simulation-driven rope** that (depending on configuration) can swing, sag, and collide with the environment even within the Editor. It can be used for simulating realistic cables, vines, chains, or any other flexible connectors that react to gravity, wind forces or external physical bodies.

The rope works by simulating a set of points using [Verlet Integration](https://en.wikipedia.org/wiki/Verlet_integration) approach and by applying additional constraints and forces on top of that. The final rendering is being done using internal `VerletRopeMesh` node. See the properties below that allow to configure this behavior.

<img width=440 src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/example_physics_01.gif"/>

## Usage
> [!NOTE]
> This node simulates physical behavior and only allows incoming collisions, if you need accurate physics interaction in both ways, please refer to [`VerletRopeRigid`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeRigid).

This node can be added anywhere using `Create New Node` dialog - here it can be found under `Node3D` tree.

<img width="440" alt="Verlet Rope Simulated create example" src="https://github.com/user-attachments/assets/11852c66-4191-4960-bdf5-43905f14272f" />

Once `VerletRopeSimulated` is in the working tree you are set - adjust the simulation and physics properties as you need. You can connect the rope to other bodies using [`VerletJointSimulated`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointSimulated) that can be created using the exposed tool button.

<img width="440" alt="Verlet Joint Simulated tool example" src="https://github.com/user-attachments/assets/d6275218-778f-48a5-842b-f5aa79e4715d" />

You can see properties descriptions below to understand how to alter the rope's behavior.

## Export Description
> [!WARNING]
> The settings of this node only gonna be applied after `Reset Rope` tool button is clicked or the scene is reloaded. Make sure to do that once you change any settings to see them applied.

### Actions
| Button Action | How it works |
|--|--|
| Reset Rope (Apply Changes) | Propagates all updated values to the internal logic and resets the rope to the initial position. |
| Add Simulated Joint        | Adds a child `VerletJointSimulated` to the current rope node in the edited scene. |

### Simulation
| Export variable | How it works |
|--|--|
| Is Created On Ready | Determines whether rope is immediately created on `_Ready` call or have to be manually created via `CreateRope` method. |
| Simulation Particles  | Determines amount of separate particles used is simulations, total segments amount is `SimulationParticles` minus 1. |
| Simulation Rate       | Determines amount of rope calculations per second, but never exceeds physics tick rate. when value is set to 0 - the rope is updated every physics frame. As it is relies on physics process, it cannot exceed physics rate. |
| Stiffness             | Akin to elasticity - it controls how much the verlet constraint corrects the rope to the expected positions. |
| Stiffness Iterations  | Number of stiffing cycles per frame, higher values gives more accurate simulation for lengthy ropes with many simulation particles. Adjust it if you find that the rope is sagging or stretching too much. |
| Preprocess Iterations | Number of frames (at `1/60` delta rate) to be precalculated on rope creation to make it start position to be in more natural state. |
| Delta Skip MS | Specifies milliseconds the physics processing frame takes after which the rope simulation will be skipped. Should be used to prevent jarrings of the rope on freezes or physics pauses.<br/> If needed should be set at least 2-3 times higher than the expected physics (default is `60 fps ~ 16 MS`) rate. Usually something like `300-500 MS` is expected just to prevent unexpected behavior during freezes. When set to 0 the option is effectively disabled. |
| Is Disabled When Invisible | Determines if the rope simulation is disabled when the rope is not on the screen (only available when `UseVisibleOn ScreenNotifier = true`). If `VerletJointSimulated` is used to connect bodies, it might be better to disable this option to prevent de-syncs. |
| Simulation Behavior   | Determines how rope is being simulated: <br/> `None` - Rope is disabled; <br/> `Game` - Only simulated in the game; <br/> `Editor` - Rope is simulated in both game and editor; <br/> `Selected` - Rope is simulated in game and only simulated in editor when selected. |

> [!TIP]
> Odd number of `SimulationParticles` is recommended for ropes attached on both sides (when using corresponding `VerletJointSimulated`) for a smoother rope at its lowest point.

> [!TIP]
> `SimulationRate` can be used to increase performance by lowering it when rope is not moving much or is far away.

### Gravity
| Export variable | How it works |
|--|--|
| Apply Gravity  | Determines if gravity force is enabled. |
| Gravity        | Gravity direction vector. |
| Gravity Scale  | A factor to uniformly scale the gravity vector. |

### Wind
| Export variable | How it works |
|--|--|
| Apply Wind     | Determines if wind force simulation is enabled, for it to work `WindNoise` must also be assigned. |
| Wind Direction | Vector that determines base force and direction of the wind. |
| Wind Noise     | `FastNoiseLite` object used as a base for wind, controls the turbulence of the wind. |
| Wind Noise Min | Determines min clamped value of the noise at any point. Recommended to have the same sign as max for consistent wind direction. |
| Wind Noise Max | Determines max clamped value of the noise at any point. Recommended to have the same sign as min for consistent wind direction. |

> [!TIP]
> Use global saved `FastNoiseLite` resource across different ropes for a global wind setting.

### Damping
| Export variable | How it works |
|--|--|
| Apply Damping  | Determines whether rope drag/damping is applied. Bringing rope back to rest with time even without collisions. |
| Damping Factor | Amount of damping applied. |

### Collision
| Export variable | How it works |
|--|--|
| Rope Collision Type      | Determines how rope collisions are being tracked: <br/> `StaticOnly` - Rope only collides with static objects specified in `StaticCollisionMask`, any *moving* `RigidBody3D` from this layer might not be handled correctly; <br/> `DynamicOnly` - Rope only collides with dynamic objects specified in `DynamicCollisionMask`, any `RigidBody3D` in the rope area will be tracked and their velocity interpolated for correct dynamic collision handling, is more performance heavy compared to static tracking; <br/> `All` - Both variants of collision tracking is enabled, see their relevant descriptions above. |
| Rope Collision Behavior  | Determines how rope collisions behave physically: <br/> `None` - Rope collisions are disabled, most performant option; <br/> `SlideStrech` - When rope particles collide, they stretch up to `SlideCollisionStretch` value, after that they slide along the collision normal up to `IgnoreCollisionStretch` value, afterwards collisions are considered unavoidable and are ignored. |
| Slide Collision Stretch  | Determines the length of the rope segment (relative to overall length) when it starts sliding along the current collision normal (if is set to '1' - the rope will be constantly sliding in different directions). |
| Ignore Collision Stretch | Determines the length of the rope segment (relative to overall length) when it starts ignoring collisions (if is set to `1` - the rope collisions are effectively disabled). |
| Max Dynamic Collisions   | Sets max amount of different bodies that will be taken into account for dynamic collisions. |
| Dynamic Collisions Tracking Margin | Sets the size of additional boundary around the rope's `Aabb` for incoming dynamic collision tracking. |
| Static Collision Mask    | The collision layers that will be affecting rope physics when it stumbles into them unmoving. |
| Dynamic Collision Mask   | The collision layers that will be affecting rope physics even while moving be being tracked internally. |
| Ray Cast Hit From Inside | Enables collision hits from inside the body for internal ray casting.  |
| Ray Cast Hit Back Faces  | Enables collision hits to back faces for internal ray casting. |

> [!NOTE]
> Dynamic collisions will provide best results with simple shapes like spheres, cylinders or capsules that are equally centered on itself, complex geometry might not collide very well.

### Quick Presets Actions
| Button Action | How it works |
|--|--|
| Preset - Base Wind | Creates reversible editor action and configures wind values: <br/> `ApplyWind = true` <br/> `WindDirection = (0, 0, 100)` <br/> `WindNoiseMin = 0.05` <br/> `WindNoiseMax = 1.0` <br/> `WindNoise = new FastNoiseLite with { Frequency = 0.03 }` |
| Preset - Floating Rope | Creates reversible editor action and configures float values: <br/> `Stiffness = 0.5` <br/> `StiffnessIterations = 2` <br/> `ApplyDamping = true` <br/> `DampingFactor = 2000.0`|
| Preset - All Collisions | Creates reversible editor action and configures collision values: <br/> `RopeCollisionType = All` <br/> `RopeCollisionBehavior = SlideStretch` <br/> `SlideCollisionStretch = 1.05` <br/> `IgnoreCollisionStretch = 5.0` <br/> `MaxDynamicCollisions = 4` <br/> `DynamicCollisionTrackingMargin = 1.0` |

### Visuals
This section is inherited from [VerletRopeMesh](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh) node.
| Export variable | How it works |
|--|--|
| Rope Length                    | Determines total target length of the rope, it is just a base value and actual length might be different depending on physics and configured behavior. |
| Rope Width                     |  Determines visual width of the rope, does not affect rope behavior. Ropes are flat, but always look at the camera, so width effectively behaves as a diameter.|
| Subdivision Lod Distance       | If distance to the particle is greater than this value, the corresponding segment is not subdivided for rendering. |
| Use Debug Particles            | Draws orientation axis from every actual particle positions when enabled. |
| Use Visible On Screen Notifier | Creates a child `VisibleOnScreenNotifier3D` for the rope when enabled. Is only triggered on `_Ready` calls. |
| Material Override              | Propagates custom material for internal `VerletRopeMesh` rendering. |

## Programmatic Access
The rope can also be manipulated via code, it exposes all the properties mentioned above and the following public methods.
| Method | How it works |
|--|--|
| `void CreateRope()` | Resets the rope and all corresponding properties, have to be called after any property changes. It is being called when you press `Reset Rope` quick button. |
| `void DestroyRope()` | Removes underlying particles data and disables rendering. Rope should be created using `CreateRope` to start working again. |
| `void CreateJoint()` | Creates child `VerletJointSimulated` node and adds it to the tree. Is being created via `Deferred`, so one frame have to be awaited to get the joint instance. |
| `RopeParticle? GetParticle(int)` | Retrieves particle data by index, supports negative indexes. The corresponding returned object is described [here](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh#ropeparticle-struct). |
| `int GetParticleCount()` | Returns current simulated particles amount. |
| `void SetAttachmentPoints(PhysicsBody3D startBody, Node3D startLocation, PhysicsBody3D endBody, Node3D endLocation)` | Manually sets attachment points of the Rope without using corresponding `VerletJoint` instance. Throws an exception if used when `VerletJoint` is already set. |
| `void SetJoint(BaseVerletJoint joint, bool toResetRope = true)` | Configures current joint of the rope to determine which points are used as rope connections, and recreates the rope if requested and was already created. |
| `bool IsRopeCreated { get; }` | Returns whether rope is created at the moment, managed via `CreateRope` and `DestroyRope` methods. |

> [!TIP]
> Don't forget to call `CreateRope()` after any property change, otherwise it will only be applied after next rope reset or scene reload.

## Related Pages
* [VerletJointSimulated](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointSimulated) - is being created by embed `Add Simulated Joint` tool button and is used to connect this rope instance to other bodies.
* [VerletRopeMesh](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh) - is used internally by this node to render the actual rope mesh.