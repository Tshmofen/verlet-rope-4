# `VerletJointSimulated` Node
**This node is used to attach a `VerletRopeSimulated` rope to both physical and non-physical objects in the scene.** It is intended to be used for visual elements like draping cables, moving hooks or tethers where only start and end positions are important for visuals and simulation.

By default it provides a one-way constraint, meaning the rope will follow the connected bodies, but it will not physically affect them, resulting in purely visual behavior. It also possible to enable `Distance Joint` allowing to configure additional constraint on connected `PhysicsBody3D` nodes providing illusion of rope preventing the bodies from separating.

<img width="440" alt="Verlet Joint Simulated example" src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/example_verlet_joint_simulated_01.gif" />

## Usage
> [!NOTE]
> As this rope and joint are simulated, the will never be affecting other bodies (except for distance joint). If you want to attach rope with actual physics refer to [VerletRopeRigid](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeRigid) and corresponding joint.

This node relies on having a reference to `VerletRopeSimulated`. It can be created using `Add Simulated Joint` tool button of the corresponding rope panel or just added it anywhere using `Create New Node` dialog - here it can be found under `Node` tree.

<img width="440" alt="Verlet Joint Simulated create example" src="https://github.com/user-attachments/assets/4fa45c4b-91e2-4ff7-ad43-5797b544a11a" />
<img width="440" alt="Verlet Joint Simulated tool example" src="https://github.com/user-attachments/assets/d6275218-778f-48a5-842b-f5aa79e4715d" />

Once `VerletJointSimulated` is in the working tree, you just need to assign `Start Body` and `End Body` nodes in it's settings for it to work as purely visual connection.

If you have provided `PhysicalBody3D` nodes in both, the `Distance Joint` settings will take effect and if `Max Distance` is greater than 0 - your bodies will be connected by internal `DistanceForceJoint`.

You can see properties descriptions below to understand how to alter the joint behavior.

> [!NOTE]
> Currently Godot physics engine doesn't have a preset distance joint implementation and custom configurations of `Generic6DofJoint` doesn't provide needed flexibility and behavior - so `DistanceForceJoint` is implemented and used internally to address this. 

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
| Verlet Rope | A `VerletRopeSimulated` node instance to which join constraints will be applied to. Automatically assigns current parent if it is of needed type and the value is currently unset. |

### Rope Start
| Export variable | How it works |
|--|--|
| Start Body                  | A `PhysicsBody3D` node that will be joined to the start of the Rope - by default `GlobalPosition` is used as connection point. |
| Start Custom Location       | A custom location for the start of the Rope. If `Start Body` is specified, is used as custom joint location for physics calculations - otherwise behaves as simple start particle constraint for `GlobalPosition`. |
| Ignore Start Body Collision | Determines whether rope will collide with the connected `Start Body`. |

### Rope End
| Export variable | How it works |
|--|--|
| End Body                  | A `PhysicsBody3D` node that will be joined to the end of the Rope - by default `GlobalPosition` is used as connection point. |
| End Custom Location       | A custom location for the end of the Rope. If `End Body` is specified, is used as custom joint location for physics calculations - otherwise behaves as simple end particle constraint for `GlobalPosition`. |
| Ignore End Body Collision | Determines whether rope will collide with the connected `End Body`. |

### Distance Joint End
| Export variable | How it works |
|--|--|
| Joint Max Distance | The distance before joint force is start being applied. When is set to zero - constraint is not applied. |
| Joint Max Force    | Max physical force that can be applied between connected bodies to reduce their distance below specified max value. |
| Joint Force Easing | Determines force easing once it's applied, is only relevant while force is less than `Max Force` and determines how fast it's rising depending on the distance. |

## Programmatic Access
The joint can also be manipulated via code, it exposes all the properties mentioned above and the following public method.
| Method | How it works |
|--|--|
| `void ResetJoint(bool toResetRope = true)` | Resets the joined rope (if requested and created) and all joint properties, have to be called after any property changes. It is being called when you press `Reset Joint` quick button. |
| `List<Rid> GetPhysicsExceptionRids()` |  Returns physics `Rid`-s of connected bodies that are to be ignored by parent `VerletRopeSimulated` instance. |

## Related Pages
* [VerletRopeSimulated](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated) - is the rope variant this joint applies constraints to.
* [DistanceForceJoint](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-DistanceForceJoint) - is used internally by this node to apply distance constraint.
