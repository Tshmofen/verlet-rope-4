# Use Cases & Tutorials

This page provides practical step‑by‑step tutorials for common scenarios using the Verlet Rope addon.  
Each guide highlights a specific use case, explains which nodes and properties are involved, and includes code snippets where needed.

> [!TIP]
> Every section below has a finished, self-demonstrating scene under `demo/examples/`, and the smaller **demo scene** (`demo/demo.tscn`) collects the basic setups – we recommend opening them alongside this guide to see the setups in action.

---

## Contents

1. [Swinging Rope / Vine](#1-swinging-rope--vine)  
2. [Tether / Leash](#2-tether--leash)  
3. [Wrecking Ball](#3-wrecking-ball)  
4. [Grappling Hook](#4-grappling-hook)  
5. [Fishing Rod](#5-fishing-rod)

---

## 1. Swinging Rope / Vine

**Goal** – Create a rope attached at one end that sways under gravity and wind – the simplest way to get started.

**Nodes Required**
- `VerletRopeSimulated`

> [!TIP]
> The finished setup is the `demo/examples/1_swinging_rope.tscn` scene, and it sways the branch the vine hangs from so it demonstrates itself - open it next to this section to see every value mentioned below in context. The vine itself has no script: the sway lives on the branch above it (`SwingingRopeDemo`), which is all the rope needs to move.

**Step‑by‑Step Setup**
1. Add a `VerletRopeSimulated` node to your scene (or as child to any `Node3D`).
   - Set `SimulationBehavior` to `Editor` if you want to see it move in the editor.
   - Adjust `RopeLength`, `SimulationParticles`, `RopeSmoothing`, and `RopeWidth` to your liking, as well as `MaterialOverride` if needed.
2. The rope’s start point will be anchored to the node’s `GlobalPosition`. You can freely move the node or parent it to another object – the rope will follow.
   - A `VerletJointSimulated` is only needed if you want to attach both ends to separate objects.
3. Enable wind by checking `ApplyWind` and creating a new `FastNoiseLite` resource for `WindNoise`. Adjust its `Frequency` to control turbulence.
   - The same noise resource can be shared between ropes: it samples position, so each rope still gets its own motion out of it.

**Example Settings**
| Node | Property | Value |
|------|----------|-------|
| Vine rope | `SimulationParticles` | 15 |
| Vine rope | `RopeLength` | 3, the length the vine hangs at |
| Vine rope | `RopeWidth` / `RopeSmoothing` | 0.06 / 0.75 |
| Vine rope | `ApplyWind` / `WindNoise` | enabled / `FastNoiseLite` with `Frequency` 0.03 |

**Troubleshooting**
- If the rope does not appear or its behavior doesn't change, ensure `SimulationBehavior` is not `None` - the editor only simulates a rope that is set to `Editor` or that is currently selected.

**See Also**
- [VerletRopeSimulated](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated)
- [VerletRopeMesh – Material Override](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh#material-override)

---

## 2. Tether / Leash

**Goal** – Connect two physics bodies with a rope that acts as a leash: the bodies can move freely until the rope runs out of slack, then the leash drags the attached body along.

**Nodes Required**
- `VerletRopeRigid`
- `VerletJointRigid`

> [!NOTE]
> The leash is built from rigid segments here, because it has to physically interact with the bodies it connects. `VerletRopeSimulated` with `VerletJointSimulated` is the lighter option, and can be used instead whenever the rope is decorative, or when it only has to collide with static geometry.

> [!TIP]
> The finished setup is the `demo/examples/2_tether_leash.tscn` scene, and it walks the leashed body back and forth so it demonstrates itself - open it next to this section to see every value mentioned below in context. The walking lives on the leashed body itself (`TetherLeashDemo`), so the leash is driven exactly like it would be by real movement.

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

**Example Settings**
| Node | Property | Value |
|------|----------|-------|
| Leash rope | `RopeLength` | 1.8, the distance the bodies can drift apart before the leash pulls |
| Leash rope | `SimulationSegments` / `TotalRopeMass` | 10 / 5, more segments are smoother but heavier, a heavier leash pulls harder |
| Leash rope | `MeshType` | `Tube`, so the rendered segments match their collision shapes |
| Leash rope | `CollisionLayer` / `CollisionMask` | 2 / 3, the layers the bodies and the obstacles sit on |

**Troubleshooting**
- If the bodies are not being pulled, make sure the rope actually goes taut - `RopeLength` should be close to the distance between the attachment points, otherwise the leash only hangs.
- If the leash drags the attached body down, lower `TotalRopeMass`.
- If the rope passes through obstacles, check `CollisionLayer` / `CollisionMask` and consider increasing `CollisionWidthMargin`.
- If the rope does not appear or its behavior doesn't change, ensure that you have pressed **Reset Joint** after joint changes - a rope property applies itself.

**See Also**
- [VerletJointRigid](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointRigid)
- [VerletRopeRigid](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeRigid)

---

## 3. Wrecking Ball

**Goal** – Create a heavy ball hanging on a rope that swings and physically knocks over objects – the ball does the damage, while the rope follows it.

**Nodes Required**
- `VerletRopeSimulated`
- `VerletJointSimulated`
- `RigidBody3D` (for the ball)
- Optional: obstacles (crates, barrels, etc.) as `RigidBody3D`

> [!TIP]
> The finished setup is the `demo/examples/3_wrecking_ball.tscn` scene, and it kicks the ball on its first physics frame so it demonstrates itself - open it next to this section to see every value mentioned below in context. The kick lives on the ball itself (`WreckingBallDemo`).

**Step‑by‑Step Setup**
1. Add a `VerletRopeSimulated` node and place it as a child of a crane or a static anchor.
   - Set `SimulationBehavior` to `Editor` if you want to see it move in the editor.
2. Add a child `VerletJointSimulated` (can be done via `Add Simulated Joint` button).
   - Leave `StartBody` unset to anchor the rope to its own position, or assign it if the rope start has to follow a moving anchor.
   - Assign `EndBody` to your ball's `RigidBody3D` and `EndCustomLocation` to an empty `Node3D` placed where the rope should attach to the ball.
3. In the **Distance Joint** subsection of the joint:
   - Set `JointMaxDistance` to the distance the ball should hang at - it is the joint that holds the ball, not the rope.
   - Keep `RopeLength` at or below that distance, so the rope is stretched taut while the ball hangs instead of sagging around the joint.
   - Raise `JointMaxForce` until the ball is held in place - it has to counteract the ball's weight, as the ball is not attached to the rope in any other way.
4. On the rope, set `SimulationParticles` to 10–20. `RopeLength` does not have to cover the whole swing - the rope is stretched over the distance the joint allows.
5. Add some obstacles (crates, barrels) with `RigidBody3D` and give them a collision layer that the ball can hit - the ball is a regular physics body, so it pushes them like any other collider.
6. Release the ball off-axis, or apply an impulse to it, so it starts swinging.

**Example Settings**
| Node | Property | Value |
|------|----------|-------|
| Wrecking rope | `SimulationParticles` / `RopeWidth` | 15 / 0.06 |
| Wrecking rope | `RopeLength` | 1, well below the swing, so the rope is stretched taut |
| Wrecking joint | `JointMaxDistance` | 2.3, the distance the ball hangs at |
| Wrecking joint | `JointMaxForce` | 2000, enough to hold the `20` kg ball |
| Wrecking ball / crates | `Mass` | 20 / 0.5, so the ball knocks the crates over |

**Troubleshooting**
- If the ball sinks too low, increase `JointMaxForce` - the joint is what holds the ball.
- If the ball barely swings, check its `Mass` against the obstacles' - a ball that is too light bounces off instead of knocking them over.
- If the rope looks rubbery while the ball swings (its segments visibly stretching and snapping back), raise `StiffnessIterations`.
- If the rope disappears while the ball swings off-screen, disable `IsDisabledWhenInvisible` on the rope.
- If the rope does not appear or its behavior doesn't change, ensure that you have pressed **Reset Joint** after joint changes - a rope property applies itself.

**See Also**
- [VerletJointSimulated – Distance Joint section](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointSimulated#distance-joint-end)
- [VerletRopeSimulated](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated)

---

## 4. Grappling Hook

**Goal** – Create a grappling hook that can be launched, sticks to surfaces and reels the player in, with the rope following it on its own.

**Nodes Required**
- `VerletRopeSimulated` (the rope)
- `VerletJointSimulated` (leashes the player to the hook and acts as the winch)
- `RigidBody3D` (for the player and the hook)
- `Node3D` markers for the hand, the rope attachment points and the aim point

> [!TIP]
> The finished setup is the `demo/examples/4_grappling_hook.tscn` scene, and it fires the hook on a timer so it demonstrates itself - open it next to this section to see every value mentioned below in context. The hook logic lives on the hook body itself (`GrapplingHookRig`) and the timer in a separate `DemoCycle` node (`GrapplingHookDemo`).

**Step‑by‑Step Setup**

1. **Rope Setup**
   - Add a `VerletRopeSimulated` as its own node - it does not have to be a child of the player (joint will attach it anyway).
   - Set `SimulationParticles` to 15–25 for a smooth rope.
   - Set `RopeLength` to the length the rope should have while the hook is out. The rope is a distance constraint, so it stretches: the example's `2.0` reads as taut over the ~7 m the hook flies, and stays tight at the `3` m the player is reeled in to.
   - Set `RenderMode` to `Process` when the rope start can move - the physics-only modes can leave the rope a frame behind the body it is tied to.
   - Disable `IsDisabledWhenInvisible` if the rope is allowed to leave the screen while the hook is out, otherwise its simulation pauses and the rope has to catch up when it comes back.

2. **Joint Setup**
   - Add a child `VerletJointSimulated` to the rope - this is the node that pulls the player.
   - Set `StartBody` to the player's `RigidBody3D`.
   - Add an empty `Node3D` to the player, place it where the hand should be and set it as `StartCustomLocation`. Without it the rope starts in the middle of the player.
   - Set `EndBody` to the hook's `RigidBody3D`.
   - Add another empty `Node3D` to the hook, place it where the rope should hold it and set it as `EndCustomLocation`. The rope end is pinned to this node, so the rope tracks the flying hook on its own.
   - Keep `IgnoreStartBodyCollision` and `IgnoreEndBodyCollision` enabled, so the rope does not push the player or the hook around.
   - Make `JointMaxForce` strong enough to hold the player (the example uses `3000` for a `30` kg player).

   > [!NOTE]
   > The joint can only move `RigidBody3D` bodies. If the player is a `CharacterBody3D`, set `StartCustomLocation` to the hand as usual, but pull the body with your own movement code instead.

3. **Parking and Launching**
   - Keep the hook in the hand while it is not in use: leave it frozen with `Freeze Mode` set to `Kinematic` and copy the hand's `GlobalPosition` onto it every frame, so it rides along with the player.
   - Fire it with `ApplyCentralImpulse`, aiming slightly above the target to compensate for the drop during the flight. Scaling the impulse by `Mass` is what keeps `LaunchSpeed` a plain speed, and the hook has to be un-frozen in the same call or it takes the impulse without moving.
   - Enable `Continuous Cd` on the hook, so a fast hook cannot tunnel through the surface it is supposed to hit.
   - Increase `JointMaxDistance` (e.g. `40`) and call `ResetJoint(false)` before the launch, otherwise the joint starts pulling while the hook is still in flight.

4. **Attaching**
   - Enable `Contact Monitor` and `Max Contacts Reported` on the hook and connect its `BodyEntered` signal.
   - On hit, stop the hook: set `Freeze Mode` to `Kinematic` and `Freeze` to `true`, then write its `GlobalPosition` once more, as kinematic bodies take their transform from the node and the physics server can otherwise snap the hook back to the transform it buffered while it was parked.
   - Set `JointMaxDistance` to the current player to hook distance and call `ResetJoint(false)`, so the rope goes taut and the player starts swinging instead of being yanked.
   - Ignore the signal unless the hook is in flight, otherwise it sticks to the first thing it brushes on the way home.

5. **Reeling In** – Only the leash changes here, the rope itself is left alone.
   - Animate `JointMaxDistance` down towards the hook and call `ResetJoint(false)` after every change - the joint needs it to pick up the new length, and passing `false` keeps the rope from being rebuilt.
   - Do not reel with `RopeLength` for this: changing it shortens the line itself and drags the whole rope after the hook, while the leash is what is supposed to move the player.
   - Make sure the pulled body has `Can Sleep` disabled, otherwise it might go to sleep while resting and silently ignores the joint.

6. **Recalling and Cleanup** – Bring the hook home and put the rope away in that order.
   - Un-freeze the hook and move it back towards the hand, or teleport it home if you do not want to show it travelling.
   - Retract the rope before hiding it: set `RopeLength` to a small value (the example uses `0.1`). The hook is in the hand by then, so the rope reels itself into it within a few frames, while a rope that is put away at full length is bound to pop out of view. Call `CreateRope()` as well only when the rope should be put away without any of it showing - that re-lays every particle in one go instead of letting the rope travel.
   - Only then hide the rope node and freeze the hook back in the hand, with `JointMaxDistance` opened up again. Freeing a node that is still assigned to `EndBody` / `EndCustomLocation` leaves the rope pointing at a destroyed body.

**Keeping the Hook's States in Sync**

Each step above belongs to one state of the hook, and the two lengths - the leash and the rope - are the ones that are easy to mix up:

| Hook state | Hook body | `JointMaxDistance` | `RopeLength` |
|------------|-----------|--------------------|------|
| Parked in the hand | frozen kinematically, position copied from the hand | opened to `40`, so it cannot pull | retracted to `0.1` and hidden |
| Flying | un-frozen, one impulse, `Continuous Cd` on | still open, so it cannot pull yet | at its working length (`2.0`) and shown |
| Attached | frozen kinematically, hit position written once | set to the player to hook distance | untouched |
| Reeling in | stays frozen, it is the anchor | animated down towards the hook | untouched |
| Retracting | un-frozen again, and no longer attached | stays at the reeled in length | untouched |

> [!NOTE]
> `JointMaxDistance` and `RopeLength` are both distances, but they do different jobs. The leash is what moves the player and can be changed as often as you like. `RopeLength` is the length of the rope itself - a simulated rope takes on a new one at any time and reels itself to it, which is what the example uses to gather the rope into the hand before hiding it.

> [!WARNING]
> Rebuilding the rope (`CreateRope()`) lays the particles out between the two bodies again, which is exactly what it is for - putting a rope that has drifted, or one that has been teleported past, back where it belongs in a single frame. It is not needed for a property change anymore, and doing it every frame makes the rope flicker between its simulated pose and a straight line.

**Example Settings**
| Node | Property | Value |
|------|----------|-------|
| Rope | `SimulationParticles` | 20 |
| Rope | `RopeLength` | `2.0` while the hook is out, `0.1` while it is parked |
| Rope | `RopeWidth` / `RopeSmoothing` | 0.05 / 0.5 |
| Rope | `RenderMode` | `Process` |
| Joint | `JointMaxDistance` | `40` while parked or flying, the attach distance once hooked |
| Joint | `JointMaxForce` | 3000 |
| Hook | `Mass` / `Continuous Cd` | 2 / enabled |
| Hook | `Contact Monitor` | enabled, `Max Contacts Reported` 4 |
| Player | `Can Sleep` | disabled, it is the body that gets pulled |

**Troubleshooting**
- If the hook snaps back to its starting position right after it attaches, re-apply its `GlobalPosition` after setting `Freeze`.
- If the player is not being pulled, check `Can Sleep` on the pulled body first - a sleeping body ignores joint forces.
- If the rope appears in the spot the hook was in before the recall, retract it (`RopeLength`) before hiding it, and set the working length back when the hook is fired.
- If the rope snaps across the map after the player or the hook is teleported, call `CreateRope()` in the same frame - the particles are pulled towards their pinned ends and take their time to follow.

**See Also**
- [VerletJointSimulated – Distance Joint section](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointSimulated#distance-joint-end)
- [VerletRopeSimulated](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated)

---

## 5. Fishing Rod

**Goal** – Simulate a fishing rod with a line that is payed out and reeled back in, and a hook hanging off the moving rod tip.

**Nodes Required**
- `MeshInstance3D` (or any `Node3D`) for the rod itself.
- `VerletRopeSimulated` for the fishing line.
- `VerletJointSimulated` (leashes the hook to the rod tip and acts as the reel - it creates the `DistanceForceJoint` for you)
- `RigidBody3D` (for the hook)

> [!TIP]
> The finished setup is the `demo/examples/5_fishing_rod.tscn` scene, and it casts the rod on a timer so it demonstrates itself - open it next to this section to see every value mentioned below in context. The rod logic lives on the rod itself (`FishingRodRig`) and the timer in a separate `DemoCycle` node (`FishingRodDemo`), so the rod can be reused without the demo around it.

**Step‑by‑Step Setup**

1. **Rod Setup** – the rod, and the hand that holds it.
   - Add an empty `Node3D` for the hand, and a `MeshInstance3D` under it for the rod.
   - Add an empty `Node3D` as a child of the hand at the tip of the rod. It is the point the line is tied to, and because the rod is rigid, the marker follows it exactly.
   - Swing the rod by moving the hand only: the rod, the tip marker and the line all come along with it.

2. **Line Setup**
   - Add a `VerletRopeSimulated` and set `RopeLength` to the length the hook hangs at. The hook is parked under the rod tip at exactly that length, so the line is neither stretched nor slack while it waits - and if the hook is thrown further out than that, it simply pulls the line taut, which is what a line being payed out looks like.
   - Set `RenderMode` to `Process` when the line is tied to a moving rod tip - the physics-only modes can leave the line a frame behind it.
   - Set `IsDisabledWhenInvisible` to `false` on the line: a joint is driving it, and a rope that pauses while it is off-screen de-syncs from the hook.

3. **Hooking Up the Line**
   - Add a `RigidBody3D` hook and place it next to the Rod, configure a `Node3D` as a child to mark the connection of the line.
   - Add a child `VerletJointSimulated` to the line, set `EndBody` to the hook's `RigidBody3D` and `EndCustomLocation` to a mark on it - the rope end is pinned there, so the line follows the hook on its own.
   - Set `JointMaxDistance` to the length the hook is meant to hang at and `JointMaxForce` high enough to hold it (the example uses `0.4` and `200` for a `0.5` kg hook).
   - Set `StartCustomLocation` to the `Node3D` sitting at the rod tip.

4. **Casting**
   - Swing the hand by moving the node the rod is held by.
   - Treat the hook as a thrown body: set its `LinearVelocity` to the aim direction times the cast speed rather than adding an impulse, so the cast goes where the rod is pointed instead of on top of whatever swing the hook had while it was hanging, and clear its angular velocity in the same call.
   - Open up `JointMaxDistance` (e.g. `30`) and call `ResetJoint(false)` before the throw, otherwise the joint drags the hook back while it is still in the air.
   - Make sure the hook's `Can Sleep` is disabled - a sleeping body ignores the joint that is holding it.

5. **Paying Out and Reeling In** – The leash is what pays the line out and reels it back in; the rope only gets stretched.
   - `RopeLength` is the line's own length, and the line is the shortest it ever gets while the hook hangs under the tip - the joint then lowers the line at that same length, so the hook is at the end of a line that is neither stretched nor slack.
   - Do every leash change through `JointMaxDistance`: open it up past the distance of the cast before the throw, and close it to the length of the line again while the hook hangs.
   - To reel in, lower `JointMaxDistance` a little below the current distance every frame - the joint drags the hook home behind it and the line stays taut as it comes.
   - Clamp the leash to the furthest you ever want the hook to reach, and put a hook that ends up further out than that back under the rod tip rather than dragging it across the whole map.
   - Damp the hook while it hangs (`LinearDamp`, `3` in the example) so a hook that was just reeled in stops swinging before the next cast. Clear the damping again when it is thrown.

**Keeping the Rod's States in Sync**

Every step above belongs to one state of the rod, and the line and the leash are the two things that are easy to mix up:

| Rod state | Hand | Rod | Line | Hook |
|-----------|------|-----|------|------|
| Hanging at rest | at rest | rigid, pointing where the hand aims | at its own length, neither stretched nor slack | hangs under the tip, damped until it stops swinging |
| Backswing | drawn back and up | carried along as one piece | stretched a little as the hook lags behind | carried along by the tip, still on the short leash |
| Cast | thrown forward | swept through the arc | pulled out straight behind the hook | thrown at `CastSpeed`, leash opened up |
| Settling | held at the end of the throw | held where the throw ended it | stretched out to the landed hook | flies, lands, rolls to a stop |
| Reeling | held | still pointing forward | shortens as the hook comes back, never going slack | dragged home by the leash, then parked under the tip |

> [!NOTE]
> `RopeLength` and `JointMaxDistance` are both distances, but only one of them is meant to move every frame. `RopeLength` is the length of the line itself - a simulated rope can take on a new one at any time, but changing it reels the whole line in or pays it out, so the example pays the cast out with the leash instead. `JointMaxDistance` is the leash that holds the hook and pulls it in: it can be changed as often as you like, and it is what actually pays the line out.

**Example Settings**
| Node | Property | Value |
|------|----------|-------|
| Line rope | `SimulationParticles` / `RopeWidth` | 16 / 0.01 |
| Line rope | `RopeLength` | `0.4`, the length it hangs at while the hook waits under the tip |
| Line rope | `DampingFactor` | 80 |
| Line joint | `JointMaxDistance` | `0.4` while the hook hangs (the length of the line), `30` while the hook is out, lowered below the hook's distance to reel in |
| Line joint | `JointMaxForce` | 200 |
| Hook | `Mass` / `Can Sleep` | 0.5 / disabled |
| Hook | `Angular Damp` | 4, so a landed hook stops rolling |

**Troubleshooting**
- If the line looks like it is being reset while the hook flies, its `RopeLength` is being changed - the line reels itself in or out towards the new length, which is not what a cast on a fixed line should look like. Keep the length fixed and move the leash instead.
- If the line hangs in a loop while the hook waits under the rod tip, `RopeLength` is longer than the distance the hook hangs at. Make the two the same, so the line is only ever pulled tight.
- If the landed hook keeps rolling away, raise its `Angular Damp`.
- If the rod needs to bend as it is cast, that is a job for the mesh (curve it, or drive it with bones).

**See Also**
- [VerletJointSimulated – Distance Joint section](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointSimulated#distance-joint-end)
- [VerletRopeMesh – Material Override](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh#material-override)
- [VerletRopeSimulated](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated)

---

## Need More Help?

If you have a specific scenario not covered here, please open an [issue](https://github.com/Tshmofen/verlet-rope-4/issues) or refer to the [FAQ](https://github.com/Tshmofen/verlet-rope-4/wiki/Guide-%E2%80%90-FAQ) for common questions
