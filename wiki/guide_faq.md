# Frequently Asked Question

## Installation
* **Q: I get errors when trying to enable the addon. What should I do?**
  * **A**: This is typically related to the .NET version or missed setup steps. Ensure that you have followed all the steps from [Installation](https://github.com/Tshmofen/verlet-rope-4/wiki/Guide-%E2%80%90-Installation) article and all requirements are correctly installed.

* **Q: I don't have .NET installed and don't want to install it. Is there a GDScript version?**
  * **A**: Author is not interested in moving this addon away from .NET or supporting two separate versions.
    - You might find some luck checking up on [migration of the addon to GDScript](https://github.com/sanyabeast/verlet_rope_4_gd) from [@sanyabeast](https://github.com/sanyabeast), at the time `v1.2.0` was used as base.
    - Also you might refer to [the original addon](https://github.com/2nafish117/godot-verlet-rope/tree/master/addons/verlet_rope_gd) version for Godot 3 from [@2nafish117](https://github.com/2nafish117) that had full implementation in GDScript, but obviously is quite outdated.

## Nodes
* **Q: What's the difference between `VerletRopeSimulated` and `VerletRopeRigid`?**
  * **A**: The `VerletRopeSimulated` node uses a custom Verlet integration physics simulation: it offers smoother visuals, more simulation controls, and is best for ropes that need to look realistic but don't need to interact with physics engine directly. The `VerletRopeRigid` node is built from a chain of `RigidBody3D` nodes: it is less smooth and offers fewer controls, but can physically interact with and exert forces on other objects in the scene, making it the choice for interactive physics.

* **Q: How to attach a rope to a moving/static/physical object?**
  * **A**: Use the corresponding joint node (either `VerletJointSimulated` or `VerletJointRigid`) they all allow mentioned connections - there is also a quick action button at each rope panel that will create needed instance for you. But if you are creating the joint yourself, don't forget to assign the reference to the rope.

## Simulation
* **Q: A rope is stretching too much or sagging. How can it be fixed?**
  * **A**: Increase the `Stiffness` or `Stiffness Iterations` value to make the rope more elastic in case of `VerletRopeSimulated`. Higher values make the constraint solving more prominent, which prevents too much stretching. Though, for `VerletRopeRigid`, you cannot directly adjust that - using fewer segments or playing with physics settings might help.

* **Q: How can performance be managed for complex scenes with many ropes?**
  * **A**: Several strategies can help:
    * **Lower the Simulation Rate**: This reduces how many times per second the rope physics are calculated.
    * **Reduce Simulation Particles**: Use the fewest number of particles needed for the desired visual fidelity.
    * **Enable `Is Disabled When Invisible`**: This halts simulation when the rope is off-screen.
    * **Simplify Collisions**: Use `StaticOnly` collision type instead of `Dynamic` or `All` where possible, as dynamic body tracking is more performance-intensive.

* **Q: Why is the rope not colliding with another moving `RigidBody3D`?**
  * **A**: Ensure the `Rope Collision Type` is set to `DynamicOnly` or `All` and `Rope Collision Behavior` is not set to `None`. Also, verify that the moving body is on a layer included in the `Dynamic Collision Mask`. For best results, use simple collision shapes like spheres or capsules on the dynamic body, as complex geometry may not collide correctly.

* **Q: The `Simulation Rate` doesn't seem to go higher than 30/60/75/144/etc. Is this a bug?**
  * **A**: No, the `Simulation Rate` is clamped to the project's physics tick rate (`60` by default) as it's only being called in physics frame. So you cannot simulate the rope more frequently than the core physics engine updates. Usually a value of `0` is recommended to just match current value.

## Visuals
* **Q: How to properly configure rope material?**
  * **A**: Assign a simple tiling texture as `Albedo` and alter the `UV1` of the `Material Override` to fit the repeated material on the rope as you need.

* **Q: How to synchronize wind effect across multiple ropes?**
  * **A**: Create a single `FastNoiseLite` resource and reuse it across your ropes in `Wind Noise` property - it will synchronize the wind values of nearby ropes. The current wind force is determined using rope particle position and system time.
