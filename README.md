<p align="center"><img height="165" alt="Verlet Rope Logo" src="/icon.svg"/></p>
<h3 align="center">Verlet Rope</h3>
<p align="center">
  A versatile addon for implementing performant and realistic physics-based ropes in 3D scenes for Godot 4 .NET projects.
  <br>
  <a href="https://github.com/Tshmofen/verlet-rope-4/wiki"><strong>Explore Wiki Docs »</strong></a>
  <br>
  <br>
  <a href="https://github.com/Tshmofen/verlet-rope-4/releases">Releases</a>
  ·
  <a href="https://github.com/Tshmofen/verlet-rope-4/wiki/Guide-%E2%80%90-Installation">Installation</a>
  ·
  <a href="https://github.com/Tshmofen/verlet-rope-4/wiki/Guide-%E2%80%90-FAQ">FAQ</a>
</p>

# Godot 4 Verlet Rope (.NET)
It is an implementation of [Verlet Intergration](https://en.wikipedia.org/wiki/Verlet_integration) for physics ropes on Godot 4.4 .NET similar to the ones seen in `Half-Life 2` or generally in `Source` engine.

This addon allows creation of dynamic and physics-based ropes, specifically offering following specialized nodes to fit different needs:
* **`VerletRopeSimulated`**: Customizable and performant node for visual ropes that react to wind, gravity, and collisions with smooth, realistic behavior.
* **`VerletRopeRigidBody`**: Node elevating internal rigid bodies for simulation, useful for cases that need ropes to physically interact with and push other objects in the game world.
* And corresponding utility **`Joint`** nodes allowing you to connect ropes to custom locations or physical objects limiting their movement.

> [!TIP]
> Don't forget to check out [GitHub Wiki](https://github.com/Tshmofen/verlet-rope-4/wiki) for additional details about addon, nodes properties, FAQs and guides! 

## Examples
<p align="center">
 <img height=295 src="/images/example_physics_01.gif"/>
 <img height=295 src="/images/example_physics_02.gif"/>
 <img height=295 src="/images/example_physics_03.gif"/>
 <img height=295 src="/images/example_game_01.gif"/>
</p>

## Features
1. **Generation of flat-plane rope meshes** using tessellation with Catmull-Rom splines.
2. **Two variants of rope simulation**: fully verlet-based (optionally affected by other bodies) and built-in physics-based (both affects and is affected by other bodies).
3. **Intergrated joint nodes** allowing to connect ropes to other nodes and physical bodies, constraining their movement.
4. **Many adjustable parameters**: particle & segment counts; lengths & widths; custom simulation rates; wind & gravity forces; customizable damping; customizable visuals; and other fine-tunning settings specific to each rope variant.
5. **`VisibleOnScreenNotifier3D` optional support** (integrated and automatic) for performance improvements when required.
6. **Simualted advanced performance-friendly slide collisions** for `VerletRopeSimulated` with static mode `O(n)` and dynamic mode `O(n*m)` raycasts complexity (`n` - rope particles, `m` - affected dynamic bodies).
7. **Editor-specific tooling** to make ropes configuration easier.
   * Different `[Tool]` buttons for quick joint creation, rope resets, structure copying and quick configuration presets;
   * Internal meta-stamping for ropes duplications/copypaste support;
   * Custom editor-collisions for precise rope click-selectors.

## Installation
Ensure you have **Godot 4.4.1+ .NET** and the **.NET 8 SDK** installed.
1. **Download the addon** using latest link from the `Releases` section.
2. **Extract archive** by putting `addons/verlet_rope_4` folder into your project's `addons/` directory.
3. **Build the binaries** by running .NET build in open Godot project.
4. **Enable the plugin** in the project settings.

> [!NOTE]  
> Checkout full installation guide if you need additional details or demo steps at [this wiki page](https://github.com/Tshmofen/verlet-rope-4/wiki/Guide-%E2%80%90-Installation).

## Quick Start
The followind nodes are being added to the `Create Child Node` menu, try them out yourself and see their corresponding wiki pages by the following links, you can find specific properties descriptions and recommendations there:
* **[`VerletRopeSimulated`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeSimulated)** -> `Node3D` - First of the two rope nodes, provides access to fully verlet-simualted rope with the most settings available.
  * **[`VerletJointSimulated`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointSimulated)** -> `Node` - Corresponding simulated joint utility node.
* **[`VerletRopeRigidBody`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeRigidBody)** -> `Node3D` - Second rope node, provides access to rigid-bodies-based rope, that allows it to properly interact with physics. 
  * **[`VerletJointRigid`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletJointRigid)** -> `Node` - Corresponding rigid joint utility node.
* **[`DistanceForceJoint`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-DistanceForceJoint)** -> `Node` - Utility node providing access to internal distance joint, can be used without ropes in case you just need to join two bodies based on distance. 
* **[`VerletRopeMesh`](https://github.com/Tshmofen/verlet-rope-4/wiki/Documentation-%E2%80%90-VerletRopeMesh)** -> `MeshInstance3D` - Utility node providing access to custom rope mesh generation, only works when particle positions are provided programmatically.

> [!NOTE]  
> All ropes and joints require quick reset after settings change, you can find a quick action button on the top of the inspector - `Reset Rope/Joint (Apply Changes)`.

> [!WARNING]  
> Unfortunately C# XML documentation is currently not supported by Godot, please refer to the wiki pages until [corresponding issue](https://github.com/godotengine/godot-proposals/issues/8269) gets resolved.

## License & Thanks
Code and documentation are released under the [MIT License](/LICENSE).  
Big thanks to the creator of the [addon base for Godot 3](https://github.com/2nafish117/godot-verlet-rope) - [@2nafish117](https://github.com/2nafish117)!

<sub>That's it, enjoy cool ropes! - Timofey Ivanov / tshmofen</sub>
