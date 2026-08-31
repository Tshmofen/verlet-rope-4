<p align="center"><img height="165" alt="Verlet Rope Logo" src="https://github.com/Tshmofen/verlet-rope-4/blob/master/icon.svg"/></p>
<h1 align="center">Welcome to the Verlet Rope 4 Wiki!</h1>

**This is a .NET 8 addon for Godot 4.4+ .NET that provides a flexible toolkit for creating dynamic and physics-based ropes in 3D scenes.** It offers two specialized types of ropes: 
* The customizable `VerletRopeSimulated` for smooth and performant visual ropes that react to environment, wind and gravity using Verlet Integration;
* And the physics-engine based `VerletRopeRigid` for ropes that can push and collide with other objects in the game world.

Using those nodes it is possible to create a lot of different interactive situations: simple swinging chains, complex grappling hooks, draping cables, rope-connected physics bodies, realistic pulley systems, interactive vines, and many other things. Refer to the documentation below to see specific nodes descriptions and guides on setup and usage.

<p align="center">
 <img width=440 src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/example_physics_01.gif"/>
 <img width=440 src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/example_physics_02.gif"/>
 <img width=440 src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/example_physics_03.gif"/>
 <img width=440 src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/example_game_01.gif"/>
</p>

*And if you have any questions, feel free to ask in the [Issues](https://github.com/Tshmofen/verlet-rope-4/issues) section of this GitHub page. Thanks for the visit! <sub>Tshmofen / Timofey Ivanov <sub>*

## Contents
See pages on the following topics:
* [Installation Guide](/Tshmofen/verlet-rope-4/wiki/Guide-%E2%80%90-Installation)
* [Migration Guide](/Tshmofen/verlet-rope-4/wiki/Guide-%E2%80%90-Migration)
* **`Documentation`**
  - `Full-featured Ropes`
    * [`VerletRopeSimulated`](/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated)
    * [`VerletRopeRigid`](/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeRigid)
  - `Custom Joints`
    * [`VerletJointSimulated`](/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointSimulated)
    * [`VerletJointRigid`](/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointRigid)
    * [`DistanceForceJoint`](/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-DistanceForceJoint)
  - `Mesh Generation`
    * [`VerletRopeMesh`](/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh)
* [FAQ](/Tshmofen/verlet-rope-4/wiki/Guide-%E2%80%90-FAQ)

## Features
**Currently, the addon provides the following features:**
- **Adds `VerletRopeMesh` node** that generates the rope mesh using tessellation with Catmull-Rom splines *(Used internally by built-in nodes, but can be accessed directly in the code to generate a mesh manually).*
  * The rendering configuration allows to chose between flat camera‑facing ribbons or 3D tubes meshes with configurable segment count, both are optimized and support custom materials.
- **Adds different variants of Rope nodes**:
  * `VerletRopeSimulated` - purely based on [verlet-integration](https://en.wikipedia.org/wiki/Verlet_integration) technique and provides beautiful rope-like movements with support for lightweight external collisions, but doesn't allow rope to affect other physical bodies.
  * `VerletRopeRigid` - relies on engine physics for rope movement (using pre-generated bodies and joints) and allows full two-way interaction with the environment, but is less rope-like and is more performance heavy. 
- **Adds different variants of Rope joint nodes**:
  * `VerletJointSimulated` - Allows connecting `VerletRopeSimulated` ends to `Node3D` and/or to `PhysicsBody3D` objects. When physics objects are connected, it also allows to create `CustomDistanceJoint` internally (see description below).
  * `VerletJointRigid` - Allows connecting `VerletRopeRigid` ends to `Node3D` and/or to `PhysicsBody3D` objects. Uses engine's built-in `PinJoint3D` internally to connect physics bodies.
  * `DistanceForceJoint` - General purpose node implementing a distance joint between two physics objects, it is pulling them with specified force when set max distance is exceeded.
- **Exposes many adjustable parameters for ropes**: particle & segment counts; length & width; custom simulation rates; wind & gravity forces; self-collisions; customizable damping; customizable visuals; and other fine-tunning settings specific to each rope variant.
- **Provides `VisibleOnScreenNotifier3D` support** (optional, integrated and automatic) for performance improvement when needed.
- **Implements advanced performance-friendly slide collisions** (for `VerletRopeSimulated`) with static mode `O(n)` and dynamic mode `O(n*m)` raycasts complexity (where `n` - rope particles, `m` - affected dynamic bodies).
- **Provides editor-specific tooling** to make ropes configuration easier, such as: different `[Tool]` buttons for quick joint creation, rope resets, structure copying, quick configuration presets, etc.; internal meta-stamping for ropes duplications/copypaste support; custom editor-collisions for precise rope click-selectors.

## Background
Originally the code was based on [addon from @2nafish117](https://github.com/2nafish117/godot-verlet-rope), so many thanks to the original author!  
* Current codebase is heavily refactored and completely reworked from scratch compared to the original version in both behavior, features and internal structure - so while covering all the original features and adding a lot of new ones, it is not reverse-compatible and all ropes will have to be created from the ground if you plan on migrating from it.