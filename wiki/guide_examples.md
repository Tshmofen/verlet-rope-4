# Use Cases & Tutorials

This page provides practical step‑by‑step tutorials for common scenarios using the Verlet Rope addon.  
Each guide highlights a specific use case, explains which nodes and properties are involved, and includes code snippets where needed.

> [!TIP]
> Many of these examples can be found in the **demo scene** (`demo/demo.tscn`) – we recommend opening it alongside this guide to see the setups in action.

---

## Contents

1. [Swinging Rope / Vine](#1-swinging-rope--vine)  
2. [Tether / Leash](#2-tether--leash)  
3. [Wrecking Ball](#3-wrecking-ball)  
4. [Grappling Hook](#4-grappling-hook)  
5. [Fishing Rod](#5-fishing-rod)  
6. [Pulley / Winch](#6-pulley--winch)

---

## 1. Swinging Rope / Vine

**Goal** – Create a rope attached at one end that sways under gravity and wind – the simplest way to get started.

**Nodes Required**
- `VerletRopeSimulated`

**Step‑by‑Step Setup**
1. Add a `VerletRopeSimulated` node to your scene (or as child to any `Node3D`).
   - Set `SimulationBehavior` to `Editor` if you want to see it move in the editor.
   - Adjust `RopeLength`, `SimulationParticles`, `RopeSmoothing`, and `RopeWidth` to your liking, as well as `MaterialOverride` if needed.
2. The rope’s start point will be anchored to the node’s `GlobalPosition`. You can freely move the node or parent it to another object – the rope will follow.
   - A `VerletJointSimulated` is only needed if you want to attach both ends to separate objects.
3. Enable wind by checking `ApplyWind` and creating a new `FastNoiseLite` resource for `WindNoise`. Adjust its `Frequency` to control turbulence.

**Key Settings**
| Property | Recommended Value | Note |
|----------|-------------------|------|
| `SimulationParticles` | 10–20 | More particles give smoother curves. |
| `ApplyGravity` | `true` | Essential for natural sagging. |
| `ApplyWind` | `true` | Adds organic motion. |
| `WindNoise` | `FastNoiseLite` | Required for wind; adjust `Frequency` for turbulence. Can be shared between ropes to use one unified wind setting (samples position, so will be different enough for each rope). |

**Troubleshooting**
- If the rope does not appear or its behavior doesn't change, ensure `SimulationBehavior` is not `None` and that you have pressed **Reset Rope** after changes.

---

## 2. Tether / Leash

**Goal** – Connect two physics bodies with a rope that acts as a leash: the bodies can move freely until the rope runs out of slack, then the leash drags the attached body along.

**Nodes Required**
- `VerletRopeRigid`
- `VerletJointRigid`

> [!NOTE]
> The leash is built from rigid segments here, because it has to physically interact with the bodies it connects. `VerletRopeSimulated` with `VerletJointSimulated` is the lighter option, and can be used instead whenever the rope is decorative, or when it only has to collide with static geometry.

**Step‑by‑Step Setup**
1. Place a `VerletRopeRigid` node in the scene.
2. Add a child `VerletJointRigid` (can be done via `Add Rigid Joint` button).
3. In the joint, assign:
   - `StartBody` -> the first `PhysicsBody3D` (e.g., a character or crate).
   - `EndBody` -> the second `PhysicsBody3D`.
   - (Optionally) set `StartCustomLocation` / `EndCustomLocation` if you want the attachment points to be offset from the bodies' origins (you will need to create empty `Node3D`'s to point to).
4. On the rope, set `RopeLength` to the leash length - the segments are spread across the attachment points, so the leash starts pulling as soon as it runs out of slack.
5. Adjust `SimulationSegments` and `TotalRopeMass` to balance how smooth and how heavy the leash is.
6. Ensure `CollisionLayer` and `CollisionMask` are set to interact with the environment and with the connected bodies.

**Key Settings**
| Property | Recommended Value | Note |
|----------|-------------------|------|
| `RopeLength` | 1–5 | The leash length, how far the bodies can drift apart before the rope pulls. |
| `SimulationSegments` | 8–12 | More segments give a smoother leash, but are heavier. |
| `TotalRopeMass` | 2–10 | Higher values make the leash heavier and its pull stronger. |
| `CollisionLayer` / `Mask` | Match bodies | Essential for the leash to collide with obstacles and other bodies. |
| `MeshType` | `Tube` | Renders the segments as a chain, matching their collision shapes. |

**Troubleshooting**
- If the bodies are not being pulled, make sure the rope actually goes taut - `RopeLength` should be close to the distance between the attachment points, otherwise the leash only hangs.
- If the leash drags the attached body down, lower `TotalRopeMass`.
- If the rope passes through obstacles, check `CollisionLayer` / `CollisionMask` and consider increasing `CollisionWidthMargin`.
- If the rope does not appear or its behavior doesn't change, ensure that you have pressed **Reset Rope** (and/or **Reset Joint**) after changes.

**See Also**
- [VerletJointRigid](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointRigid)
- [VerletRopeRigid](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeRigid)

---

## 3. Wrecking Ball

**Goal** – Create a heavy ball hanging on a rope that swings and physically knocks over objects - the ball does the damage, while the rope follows it.

**Nodes Required**
- `VerletRopeSimulated`
- `VerletJointSimulated`
- `RigidBody3D` (for the ball)
- Optional: obstacles (crates, barrels, etc.) as `RigidBody3D`

**Step‑by‑Step Setup**
1. Add a `VerletRopeSimulated` node and place it as a child of a crane or a static anchor.
   - Set `SimulationBehavior` to `Editor` if you want to see it move in the editor.
2. Add a child `VerletJointSimulated` (can be done via `Add Simulated Joint` button).
   - Leave `StartBody` unset to anchor the rope to its own position, or assign it if the rope start has to follow a moving anchor.
   - Assign `EndBody` to your ball's `RigidBody3D` and `EndCustomLocation` to an empty `Node3D` placed where the rope should attach to the ball.
3. In the **Distance Joint** subsection of the joint:
   - Set `JointMaxDistance` slightly below `RopeLength`, so the rope stays taut while the ball hangs.
   - Raise `JointMaxForce` until the ball is held in place - it has to counteract the ball's weight, as the ball is not attached to the rope in any other way.
4. On the rope, set `SimulationParticles` to 10–20 and `RopeLength` to the length of the swing.
5. Add some obstacles (crates, barrels) with `RigidBody3D` and give them a collision layer that the ball can hit - the ball is a regular physics body, so it pushes them like any other collider.
6. Release the ball off-axis, or apply an impulse to it, so it starts swinging.

**Key Settings**
| Property | Recommended Value | Note |
|----------|-------------------|------|
| `SimulationParticles` | 10–20 | More particles give a smoother curve. |
| `RopeLength` | 2–5 | The length of the swing. |
| `JointMaxDistance` | Slightly below `RopeLength` | The distance the ball can reach before the joint starts pulling. |
| `JointMaxForce` | 1000+ | Has to hold the weight of the ball; higher values make it hang tighter. |
| `Stiffness` / `StiffnessIterations` | 0.9 / 2–4 | Raise if the rope looks stretched while the ball swings. |

**Troubleshooting**
- If the ball sinks too low, increase `JointMaxForce` - the joint is what holds the ball.
- If the ball barely swings, check its `Mass` against the obstacles' - a ball that is too light bounces off instead of knocking them over.
- If the rope appears stretched at the bottom of the swing, increase `StiffnessIterations`.
- If the rope disappears while the ball swings off-screen, disable `IsDisabledWhenInvisible` on the rope.
- If the rope does not appear or its behavior doesn't change, ensure that you have pressed **Reset Rope** (and/or **Reset Joint**) after changes.

**See Also**
- [VerletJointSimulated – Distance Joint section](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointSimulated#distance-joint-end)
- [VerletRopeSimulated](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated)

---

## 4. Grappling Hook

**Goal** – Create a grappling hook that can be launched, sticks to surfaces and reels the player in, with the rope following it on its own.

**Nodes Required**
- `VerletRopeSimulated`
- `VerletJointSimulated` (to leash the rope to the player and the hook)
- `RigidBody3D` (for the player and the hook)

**Step‑by‑Step Setup**

1. **Rope Setup**
   - Add a `VerletRopeSimulated` as a separate node, or as a child of the player.
   - Set `SimulationParticles` to 15–25 for a smooth rope.
   - Enable `ApplyGravity` and adjust `Stiffness` to your taste.
   - Set `RopeLength` shorter than the distance the hook travels, so the rope reads as taut while the hook is out, and keep it longer than the shortest distance you reel the player in to, so the rope does not end up in a loose pile.

2. **Joint Setup**
   - Add a child `VerletJointSimulated` to the rope - this is the node that pulls the player.
   - Set `StartBody` to the player's `RigidBody3D`.
   - Add an empty `Node3D` to the player, place it where the hand should be and set it as `StartCustomLocation`. Without it the rope starts in the middle of the player.
   - Set `EndBody` to the hook's `RigidBody3D`.
   - Add another empty `Node3D` to the hook, place it where the rope should hold it and set it as `EndCustomLocation`. The rope end follows this node, so the rope tracks the flying hook on its own.
   - Keep `IgnoreStartBodyCollision` and `IgnoreEndBodyCollision` enabled, so the rope does not push the player or the hook around.

   > [!NOTE]
   > The joint can only move `RigidBody3D` bodies. If the player is a `CharacterBody3D`, set `StartCustomLocation` to the hand as usual, but pull the body with your own movement code instead.

3. **Launching**
   - Apply an impulse to the hook with `ApplyCentralImpulse` when firing, aiming slightly above the target to compensate for the drop during the flight.
   - Enable `Continuous Cd` on the hook, so a fast hook cannot tunnel through the surface it is supposed to hit.
   - Open up `JointMaxDistance` (e.g. `40`) and call `ResetJoint(false)` before the launch, otherwise the joint starts pulling while the hook is still in flight.

4. **Attaching**
   - Enable `Contact Monitor` and `Max Contacts Reported` on the hook and connect its `BodyEntered` signal.
   - On hit, stop the hook: set `Freeze Mode` to `Kinematic` and `Freeze` to `true`, then write its `GlobalPosition` once more, as kinematic bodies take their transform from the node and the physics server can otherwise snap the hook back to the transform it buffered while it was parked.
   - Set `JointMaxDistance` to the current player to hook distance and call `ResetJoint(false)`, so the rope goes taut and the player starts swinging instead of being yanked.

5. **Retracting / Pulling**
   - To retract, animate `JointMaxDistance` down towards the hook and call `ResetJoint(false)` after every change - the joint needs it to pick up the new length, and passing `false` keeps the rope from being rebuilt.
   - Do not reach for `RopeLength` and `CreateRope()` for this: rebuilding the rope re-spreads every particle, so doing it per frame pops visibly.
   - Make sure the pulled body has `Can Sleep` disabled, otherwise it might go to sleep while resting and silently ignores the joint.

6. **Cleanup** – When the hook is recalled, freeze it back at the player's hand and open up `JointMaxDistance` again. Freeing a node that is still assigned to `EndBody` / `EndCustomLocation` leaves the rope pointing at a destroyed body.

**Key Scripting Snippet**
```csharp
// Stick the hook to the surface it hit and use it as the winch anchor.
private void Attach()
{
    var attachPoint = hook.GlobalPosition;
    hook.FreezeMode = RigidBody3D.FreezeModeEnum.Kinematic;
    hook.Freeze = true;
    hook.GlobalPosition = attachPoint;
    hook.LinearVelocity = Vector3.Zero;
    hook.AngularVelocity = Vector3.Zero;

    _attachedDistance = hand.GlobalPosition.DistanceTo(attachPoint);
    SetLeashLength(_attachedDistance);
}

// Reeling in is only a joint length change, the rope follows on its own.
private void SetLeashLength(float distance)
{
    joint.JointMaxDistance = distance;
    joint.ResetJoint(false);
}
```

**Troubleshooting**
- If the hook snaps back to its starting position right after it attaches, re-apply its `GlobalPosition` after setting `Freeze`.
- If the player is not being pulled, check `Can Sleep` on the pulled body first - a sleeping body ignores joint forces.
- If the rope looks stretched while reeling in, increase `StiffnessIterations` to compensate.

**See Also**
- [VerletJointSimulated – Distance Joint section](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointSimulated#distance-joint-end)
- [VerletRopeSimulated](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated)

---

## 5. Fishing Rod

**Goal** – Simulate a flexible fishing rod (short, stiff rope) with a line and a bobber that reacts to physics.

**Nodes Required**
- `VerletRopeSimulated` (for the rod)
- (Optional) `VerletRopeSimulated` (for the fishing line) – or use a single rope for both.
- `DistanceForceJoint` (to connect the bobber to the line end)

**Step‑by‑Step Setup**

1. **Rod Setup**  
   - Add a `VerletRopeSimulated` with very few particles (e.g., 3–5) and a short `RopeLength` (e.g., 0.5–1.0).  
   - Set `Stiffness` high (1.2–1.5) to make it rigid, and `RopeSmoothing` low to keep it responsive.  
   - Attach the start to the player's hand (using `VerletJointSimulated`).

2. **Line Setup**  
   - Add a second `VerletRopeSimulated` (or use the same rope) with more particles and a longer length.  
   - Attach its start to the tip of the rod (the end particle of the rod rope).

3. **Bobber**  
   - Add a `RigidBody3D` (the bobber) and connect it to the line's end particle using a `DistanceForceJoint` (or a `VerletJointSimulated` with Distance Joint).  
   - The bobber can have its own gravity and buoyancy logic.

4. **Visuals**  
   - Use different materials for the rod (e.g., brown) and the line (e.g., transparent/white) via `MaterialOverride`.  
   - Enable `UseDebugParticles` to inspect the rod's orientation.
   - Consider setting `RenderMode` to `Process` for the rod to follow the hand smoothly without lag.

**Key Settings**
| Node | Property | Value |
|------|----------|-------|
| Rod rope | `SimulationParticles` | 4 |
| Rod rope | `Stiffness` | 1.3 |
| Rod rope | `RopeSmoothing` | 0.2 |
| Rod rope | `RenderMode` | `Process` (for instant response) |
| Line rope | `SimulationParticles` | 15 |
| Line rope | `RopeLength` | 3–5 |

**Scripting** – The rods can be controlled by updating the start particle position to follow the hand, and the line's start to follow the rod tip.

**Troubleshooting**
- If the rod bends too much, increase `StiffnessIterations`.
- To prevent the line from clipping through the rod, enable `RopeCollisionBehavior` with appropriate masks.

**See Also**
- [VerletRopeMesh – Material Override](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh#material-override)

---

## 6. Pulley / Winch

**Goal** – Redirect a rope over one or more pulleys, changing its direction, and allow it to lift a weight.

**Nodes Required**
- Multiple `VerletRopeSimulated` instances (one per rope segment between pulleys).
- `VerletJointSimulated` (to connect segments end‑to‑start).
- (Optional) `PinJoint3D` or `DistanceForceJoint` for mechanical coupling.

**Step‑by‑Step Setup** (simple two‑pulley example)

1. **Create Pulley Points** – Place `Node3D`s at the pulley locations (e.g., top left, top right, and a bottom weight).
2. **Segment 1** – Rope from start anchor to first pulley.
3. **Segment 2** – Rope from first pulley to second pulley.
4. **Segment 3** – Rope from second pulley to the weight.
5. **Connect segments** using `VerletJointSimulated`:
   - For each joint, set `StartCustomLocation` to the start pulley point and `EndCustomLocation` to the next pulley point.
   - Enable the Distance Joint only on the last segment if you want tension.
6. **Sync movement** – To simulate a winch, adjust the start point of the first segment (e.g., move it along a path) and the rope will follow.

**Alternative** – Use a single rope and apply custom constraints in code to force it through waypoints (more advanced).

**Key Settings**
- Each segment should have the same `RopeLength` and `SimulationParticles` to keep consistent tension.
- Use `RopeCollisionBehavior = None` to avoid internal collisions between segments.
- Set `RenderMode` to `PhysicsAndMovement` to prevent flicker when the pulley moves.

**Scripting** – You may need to manually update the start/end particle positions each frame to match the pulley positions if you are not using joints.

**Troubleshooting**
- Ropes may drift between segments – use `Stiffness` and `StiffnessIterations` to keep them tight.
- For a winch, gradually change the length of the first segment (by changing `RopeLength` and calling `CreateRope()`) to simulate spooling.

**See Also**
- [SegmentPlaceUtility](https://github.com/Tshmofen/verlet-rope-4/blob/master/addons/verlet_rope_4/Utility/SegmentPlaceUtility.cs) (for generating initial rope shapes)

---

## Combining Use‑Cases

Many complex interactions can be built by combining the above patterns. For example:

- **Grappling hook + tow cable** – a hook that pulls the player and also tows a crate.
- **Fishing rod + tether** – a line that can reel in a fish (simulating tension).

Feel free to experiment and share your own creations!

---

## Need More Help?

If you have a specific scenario not covered here, please open an [issue](https://github.com/Tshmofen/verlet-rope-4/issues) or refer to the [FAQ](https://github.com/Tshmofen/verlet-rope-4/wiki/Guide-%E2%80%90-FAQ) for common questions
