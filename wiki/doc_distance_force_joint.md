# `DistanceForceJoint` Node
**This node implements missing built-in distance joint.** It is possible to configure distance-like joint using custom configurations of `Generic6DofJoint`, but it doesn't provide needed flexibility and behavior for specific usage - this node is used internally in `Simulated` version of the rope to address this. 

It works by applying force on both connected bodies at the corresponding points of connection, but only when the distance between them exceeds set value. See properties below to understand how to configure this behavior.

<img width="440" alt="Distance Force Joint example" src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/example_distance_force_joint_01.gif" />

## Usage
> [!NOTE]
> This node is automatically being created internally by `VerletJointSimulated` when you configure `Distance Joint` settings and it has two `PhysicsBody3D` nodes attached - in those cases you don't have to configure this node separately.

If you want to join two physics bodies based on distance without using rope or joint, this node can be used completely separately. Just add it anywhere using `Create New Node` dialog - it can be found under `Node` tree.

<img width="440" alt="Distance Force Joint Creation" src="https://github.com/user-attachments/assets/778e3614-7a0b-4178-b0e6-6309cc29f491" />

Once `DistanceForceJoint` is in the working tree, you just need to assign `Body A` and `Body B` nodes in it's settings for it to work. See properties descriptions below to understand how to alter the joint behavior.

> [!NOTE]
> The behavior heavily depends on the masses of the connected bodies and the current `MaxForce` - make sure to play around with this setting to see that the joint is affecting bodies properly.
> * If force is too weak, the bodies will easily separate with each other no matter the distance between them.
> * But if it's too strong, they might start moving explosively once the terminal distance is reached.

## Export Description

### Connection Settings
| Export variable | How it works |
|--|--|
| Body A            | Physical body used in joint calculations, by default `GlobalPosition` is used as connection point. The force will be applied only if it is instance of `RigidBody3D`. |
| Custom Location A | A custom location for `BodyA` distance calculations and force applying. |
| Body B            | A `PhysicsBody3D` node that will be used as part of distance resolving - by default it's `GlobalPosition` is used as connection point. |
| Custom Location B | A custom location for `BodyB` distance calculations and force applying. |

### Movement Settings
| Export variable | How it works |
|--|--|
| Max Distance | The distance before joint force is start being applied. When is set to zero - constraint is not processed. |
| Max Force    | Max physical force that can be applied between connected bodies to reduce the distance between them below specified max value. |
| Force Easing | Determines force easing once it's being applied, is only relevant while force is less than `Max Force` and determines how fast it's rising depending on the distance. |

## Programmatic Access
The joint can also be manipulated via code, it exposes all the properties mentioned above and the following public property.
| Method | How it works |
|--|--|
| `Func<bool> IsAppliedCustomCondition` | Determines custom condition for force applying, if returns `false` - the joint does nothing. Can be used to disable joint when external context requires it (e.g. `VerletRopeSimulated` is not created yet). |

## Related Pages
* [VerletJointSimulated](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointSimulated) - Simulated joint is using this node internally when corresponding `Distance Joint` settings are enabled.


