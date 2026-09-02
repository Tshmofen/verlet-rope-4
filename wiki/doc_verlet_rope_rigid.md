# `VerletRopeRigid` Node
**This node implements rope fully relying on the engine's built-in physics system**, allowing it to cover cases when the rope needs to physically interact with and exert forces on other objects in the scene, such as pushing crates or swinging into characters.

It implements a physics-based rope by connecting a chain of internal `RigidBody3D` nodes using `PinJoint3D`s. However, this reliance on rigid bodies results in a less smooth and more computationally expensive simulation compared to the `VerletRopeSimulated` node. It offers fewer direct simulation controls and can appear more "crude" in its movement, trading nicer visual for robust physical interactions with the environment.

<img width="440" alt="Verlet Joint Rigid example" src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/example_verlet_joint_rigid_01.gif" />

## Usage
> [!NOTE]
> This node relies on internal physics for behavior, if you don't need that and only want nice visuals, please refer to [`VerletRopeSimulated`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated) as it provides more options and is more performant.

> [!CAUTION]
> As this rope is fully physical it should not be updated in `_Process` or moved using `Position` properties (even via parent). All the movement should be done via `_PhysicsProcess` and by applying forces. Otherwise as any other physical body it might lose collisions or move incorrectly.

This node can be added anywhere using `Create New Node` dialog - here it can be found under `Node3D` tree.

<img width="440" alt="Verlet Rope Rigid create example" src="https://github.com/user-attachments/assets/5a9decf5-e455-4d16-a8aa-69be5149bd14" />

Once `VerletRopeRigid` is in the working tree you are set - adjust the simulation and physics properties as you need. You can connect the rope to other bodies using [`VerletJointRigid`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointRigid) that can be created using the exposed tool button.

<img width="440" alt="Verlet Joint Rigid tool example" src="https://github.com/user-attachments/assets/84017c0f-0b10-447f-8215-11b0ccda3c36" />

You can see properties descriptions below to understand how to alter the rope's behavior.

## Export Description
> [!WARNING]
> The settings of this node only gonna be applied after `Reset Rope` tool button is clicked or the scene is reloaded. Make sure to do that once you change any settings to see them applied.

### Actions
| Button Action | How it works |
|--|--|
| Reset Rope (Apply Changes) | Propagates all updated values to the internal logic and recreates the rope with the initial state. |
| Clone Rigid Bodies         | Clones internal physics configuration of the rope into editable sibling of the rope. |
| Add Rigid Joint            | Adds a child `VerletJointRigid` node to the current rope in the edited scene. |

### Simulation
| Export variable | How it works |
|--|--|
| Is Created On Ready | Determines whether rope is immediately created on `_Ready` call or have to be manually created via `CreateRope` method. |
| Simulation Segments | Determines amount of separate physical segments used in simulation, formally total particles amount for mesh visuals is `SimulationSegments` + 1. |

### Physics - Collision
| Export variable | How it works |
|--|--|
| Collision Width Margin     | Adjusts the radius of rope segment collision. Final collision width equals to `RopeWidth` + `CollisionWidthMargin`. |
| Collision Layer            | The collision layer that will be propagated to internal physics of each segment. |
| Collision Mask             | The collision mask that will be propagated to internal physics of each segment. |
| Is Continuous Collision    | Determines whether internal physics using continuous collision checks. |
| Show Collision Shape Debug | Renders meshes with the same shape and size as internal `CollisionShape3D`-s used in segments physics. |

### Physics - Segments
| Export variable | How it works |
|--|--|
| Total Rope Mass           | Determines overall mass of the rope, each segment will have weight equal to `TotalRopeMass` divided by `SimulationSegments` count. |
| Gravity Scale             | The gravity scale that will be propagated to internal physics of each segment. |
| Physics Material Override | Engine physics material that will be propagated to internal physics of each segment, if not specified default engine values are used. |
| Linear Damp Mode          | Determines `Linear Damp Mode` value for each separate internal `RigidBody3D`. |
| Linear Damp               | Determines `Linear Damp` value for each separate internal `RigidBody3D`. If value is zero - uses `Default Linear Damp` from project settings.  |
| Angular Damp Mode         | Determines `Angular Damp Mode` value for each separate internal `RigidBody3D`. |
| Angular Damp              | Determines `Angular Damp` value for each separate internal `RigidBody3D`. If value is zero - uses `Default Angular Damp` from project settings. |

### Physics - Joints
| Export variable | How it works |
|--|--|
| Is Start Pinned   | Creates additional `PinJoint3D` at the start of the first segment when enabled. |
| Pin Bias          | Determines `Bias` for each separate `PinJoint3D` internal object. Does not change anything with `Jolt` physics. |
| Pin Damping       | Determines `Damping` for each separate `PinJoint3D` internal object. Does not change anything with `Jolt` physics. |
| Pin Impulse Clamp | Determines `Impulse Clamp` for each separate `PinJoint3D` internal object. Does not change anything with `Jolt` physics. |

### Visuals
This section is partially inherited from [VerletRopeMesh](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh) node.
| Export variable | How it works |
|--|--|
| Mesh Type                      | Determines the rope’s visual appearance: `Ribbon` (flat camera‑facing ribbon) or `Tube` (3D cylindrical mesh). |
| Rope Length                    | Determines total target length of the rope, it is just a base value and actual length might be different depending on physics and configured behavior. |
| Rope Width                     |  Determines visual width of the rope, does not affect rope behavior. Width effectively behaves as a diameter. |
| RenderMode                     | Determines when the rope’s mesh is drawn. `Physics` – only during physics ticks, the most performant mode. `PhysicsAndMovement` – also redraws immediately in the process frame when the node’s global position changes, preventing flicker when dragged/moved in the editor or at runtime. `Process` – redraws every process frame, giving the smoothest response at a small performance cost. |
| Rope Smoothing                 | Amount of smoothing applied to particle positions for rendering. Higher values make the rope appear gentler but less responsive. 0 disables smoothing. |
| Smooth Rope Start              | If `true`, smoothing is applied at the start of the rope. Disable when the start must stay rigidly attached to a moving point. |
| Smooth Rope End                | If `true`, smoothing is applied at the end of the rope. Disable when the end must stay rigidly attached to a moving point. |
| Tube Segments                  | Number of segments around the tube’s circumference. Only used when `Mesh Type = Tube`. |
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
| `void CreateJoint()` | Creates child `VerletJointRigid` node and adds it to the tree. Is being created via `Deferred`, so one frame have to be awaited to get the joint instance. |
| `public void CloneRigidBodies(int actionId = 0, bool toCreate = true)` | Recreates current internal structure of the rope in a sibling node. Is being created via `Deferred`, so one frame have to be awaited to get the joint instance. The exposed arguments are used for editor `UndoRedo` and can be ignored. |
| `RopeParticle? GetParticle(int)` | Retrieves particle data by index, supports negative indexes. The corresponding returned object is described [here](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh#ropeparticle-struct). |
| `int GetParticleCount()` | Returns current simulated particles amount. |
| `void SetAttachmentPoints(PhysicsBody3D startBody, Node3D startLocation, PhysicsBody3D endBody, Node3D endLocation)` | Manually sets attachment points of the Rope without using corresponding `VerletJoint` instance. Throws an exception if used when `VerletJoint` is already set. |
| `void SetJoint(BaseVerletJoint joint, bool toResetRope = true)` | Configures current joint of the rope to determine which points are used as rope connections, and recreates the rope if requested and was already created. |
| `bool IsRopeCreated { get; }` | Returns whether rope is created at the moment, managed via `CreateRope` and `DestroyRope` methods. |

> [!TIP]
> Don't forget to call `CreateRope()` after any property change, otherwise it will only be applied after next rope reset or scene reload.

## Related Pages
* [VerletJointRigid](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointRigid) - is being created by embed `Add Rigid Joint` tool button and is used to connect this rope instance to other bodies.
* [VerletRopeMesh](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh) - is used internally by this node to render the actual rope mesh.