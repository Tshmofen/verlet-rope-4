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

**Goal** – Create a grappling hook that can be launched, attach to surfaces, retract/pull the player, and adjust its length dynamically.

**Nodes Required**
- `VerletRopeSimulated`
- `VerletJointSimulated` (to attach the rope to the player and the hook)
- (Optional) `Area3D` / `RayCast3D` for detection.

**Step‑by‑Step Setup**

1. **Rope Setup**  
   - Add a `VerletRopeSimulated` as a child of the player (or as a separate node).  
   - Set `SimulationParticles` to 15–25 for a smooth rope.  
   - Enable `ApplyGravity` and adjust `Stiffness` to your taste.

2. **Joint Setup**  
   - Add a child `VerletJointSimulated` and assign:
     - `StartBody` → the player's `RigidBody3D` (or use `StartCustomLocation` if the player is a `CharacterBody3D`).
     - `EndBody` → the hook's `RigidBody3D` (or `EndCustomLocation` if the hook is a `Node3D`).
   - Enable `IgnoreStartBodyCollision` and `IgnoreEndBodyCollision` to prevent rope‑body collisions.

3. **Launching & Attaching**  
   - Write a script to **detach the end** of the rope when firing (set `end.IsAttached = false`), apply an initial velocity to the hook, and later, when the hook hits a surface, **re‑attach** the end to that surface (set `end.IsAttached = true` and `end.PositionCurrent = hitPoint`).
   - Use `GetParticle(-1)` to access the end particle.

4. **Retracting / Pulling**  
   - To retract, gradually reduce the `RopeLength` property and call `CreateRope()` (or update the particle positions manually if you need smooth animation).  
   - Alternatively, apply a force to the player toward the hook point.

5. **Cleanup** – When the hook is detached, destroy the joint or reset the rope.

**Key Scripting Snippet**
```csharp
// Example: Attach end particle to a hit point
public void AttachHookEnd(Vector3 hitPoint)
{
    var endParticle = rope.GetParticle(-1);
    if (endParticle.HasValue)
    {
        var p = endParticle.Value;
        p.IsAttached = true;
        p.PositionCurrent = hitPoint;
        p.PositionPrevious = hitPoint;
        // Update particle data (rope.ParticleData is a reference)
    }
}
```

**Troubleshooting**
- When retracting, the rope may become stretched; increase `StiffnessIterations` to compensate.
- To avoid jitter, update particle positions in `_PhysicsProcess` and call `rope.CreateRope()` only when necessary.

**See Also**
- [RopeParticle struct](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh#ropeparticle-struct)

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
