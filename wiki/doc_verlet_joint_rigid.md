# `VerletJointRigid` Node
**This node is used to physically attach a `VerletRopeRigid` to other objects in the scene** using the engine's physics joints. It is helpful for creating interactive setups, e.g. a platform with swinging rope or a wrecking ball chained to a crane, or any other ropey physical interaction.

It functions by creating internal `PinJoint3D` connections, allowing you to pin either the start, the end, or both points of the rope to the specific `PhysicsBody3D` nodes or static points in the world.

<img width="440" alt="Verlet Joint Rigid example" src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/example_verlet_joint_rigid_01.gif" />

## Usage
> [!NOTE]
> As this node relies on internal physics engine, connected bodies and the rope will always be affecting each other. If you want to attach rope to something without affecting physics refer to [`VerletRopeSimulated`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated) and corresponding joint.

This node relies on having a reference to `VerletRopeRigid`. It can be created using `Add Rigid Joint` tool button of the corresponding rope panel or just added it anywhere using `Create New Node` dialog - here it can be found under `Node` tree.

<img width="440" alt="Verlet Joint Rigid creation example" src="https://github.com/user-attachments/assets/85ba07a4-7f9b-4cca-b688-d398ba2a2db0" />
<img width="440" alt="Verlet Joint Rigid tool example" src="https://github.com/user-attachments/assets/84017c0f-0b10-447f-8215-11b0ccda3c36" />

Once `VerletJointRigid` is in the working tree, you just need to assign `Start Body` and `End Body` nodes in it's settings for it to work. See properties descriptions below to understand how to alter the joint behavior.

> [!NOTE]
> The behavior of the joint depends on the masses of the connected bodies and the rope - there is no separate adjustment settings as in simulated variant, because this one just uses internal physics engine for constraint resolution.

## Export Description
> [!WARNING]
> The settings of this node only gonna be applied after `Reset Joint` tool button is clicked or the scene is reloaded. Make sure to do that once you change any settings to see them applied.

### Actions
| Button Action | How it works |
|--|--|
| Reset Joint (Apply Changes) | Propagates all updated values to the internal logic and resets the joint and connect rope to the initial state. |

### References
| Export variable | How it works |
|--|--|
| Verlet Rope | A `VerletRopeRigid` node instance to which joint constraints will be applied to. Automatically assigns current parent if it is of needed type and the value is currently unset. |

### Rope Start
| Export variable | How it works |
|--|--|
| Start Body                  | A `PhysicsBody3D` node that will be joined to the start of the Rope - by default `GlobalPosition` is used as connection point for underlying `PinJoint3D`. |
| Start Custom Location       | A custom location for the start of the Rope and corresponding `PinJoint3D`. If `Start Body` is specified, is used as custom joint location for physics calculations - otherwise behaves as simple `PinJoint3D` initial `GlobalPosition`. |

### Rope End
| Export variable | How it works |
|--|--|
| End Body                  | A `PhysicsBody3D` node that will be joined to the end of the Rope - by default `GlobalPosition` is used as connection point for underlying `PinJoint3D`. |
| End Custom Location       | A custom location for the end of the Rope and corresponding `PinJoint3D`. If `End Body` is specified, is used as custom joint location for physics calculations - otherwise behaves as simple `PinJoint3D` initial `GlobalPosition`. |

> [!NOTE]
> Corresponding internal pins `Bias`, `Dumping` and `Impulse Clamp` settings are being taken from corresponding settings of the referenced `VerletRopeRigid`.

> [!WARNING]
> Mentioned pins settings are not applicable for `Jolt` version of the physics engine, they will not alter anything.

## Programmatic Access
The joint can also be manipulated via code, it exposes all the properties mentioned above and the following public method.
| Method | How it works |
|--|--|
| `void ResetJoint(bool toResetRope = true)` | Resets the joined rope (if requested and created) and all joint properties, have to be called after any property changes. It is being called when you press `Reset Joint` quick button. |

## Related Pages
* [VerletRopeRigid](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeRigid) - is the rope variant this joint applies constraints to.
