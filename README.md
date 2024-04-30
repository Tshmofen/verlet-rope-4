# Godot 4 Verlet Rope (.NET)

It is a port to Godot 4.0+ of [the fast implementation of verlet-integration-based rope physics](https://github.com/2nafish117/godot-verlet-rope), similar to the one seen in Half-Life 2. 

The port extensevily refactored and refined funcitonality of the original addon. 
* It adds a few just-convenient options to control initial state of the ropes.
* Adds settings to disable ropes in-editor.
* Adds previously not-working duplication support (`ctrl`+`d`).
* Removes need to manually assign `VisibleOnScreenNotifier3D` for each rope.
* Partially changes simulation rate implementation.
* And the most cool feature - adds integrated custom physics that allows ropes to slide against any physical bodies in a quite realistic way, with a perfomance of just one raycast per rope particle (for static mode!).
* All other changes involving rebasing to the Godot 4 API / .NET and realy heavy refactoring. The code should now be much more readable and adhere to C# guidelines, so feel free to read/modify it.

That's it, enjoy cool physics ropes! <sub>(C) 2023 Timofey Ivanov / tshmofen</sub>

<p align="center">
 <img height=295 src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/advanced_physics.gif"/>
 <img height=295 src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/advanced_physics_2.gif"/>
 <img height=295 src="https://github.com/Tshmofen/verlet-rope-4/blob/master/images/physics_ropes.gif"/>
</p>

# Hints
1. Try attaching to another rope node to chain the ropes.
2. To use textures enable tiling textures and set the `UV1` property to change the tiling.
3. Remember to set rope's material cull mode to disabled, otherwise rope will be rendered only on one side (It is mostly for editor clarity, as during runtime ropes will be always facing the camera).
4. Using one pre-saved noise resource allows for a global rope wind.

# Notes
1. Rotations on ropes are disabled as they are not working with the algorithm correctly. Anyways, they are not really needed, that is enough to just move the endpoints instead as ropes is always facing the camera.
2. If you see any errors piling up in the logs after any changes in the rope script, just close/reopen the scene in the editor.
3. Do not worry about unique instances of meshes for each rope, they are ensured to be unique automatically.
4. Look at `MovingRopeDemo.cs` to see how you can move ropes without jittering when simulation rate is less than physics tickrate. In such cases use `SimulationStep` signal in a way similar to the example.

# Features
1. [Verlet integration](https://en.wikipedia.org/wiki/Verlet_integration) based particle simulation.
2. Full rope simulation within the editor.
3. Adjustable number of particles, length, width and iterations for the rope.
4. Support of changeable simulation rate, always clamping to the physics rate.
5. Flat mesh generation, always faces the current camera in play mode.
6. Automatic tessellation using Catmull-Rom splines.
7. Integrated use of `VisibleOnScreenNotifier3D` for better performance.
8. Integration of different forces: gravity, wind and air damping.
9. Advanced performance-friendly slide collisions with adjustable parameters based on a single raycast per rope particle for static mode and `O(n)` (`n` - amount of dynamic bodies inside rope `AABB`) raycasts per particle on dynamic mode.

# FAQ
* Addon is throwing errors when I'm trying to enable it, what should I do?
  * It's likely the aftermath of using .NET version of Godot. First of all be sure that you've created a solution with `Project` -> `Tools` -> `C#` -> `Create C# Solution` and make sure that it is using `.NET 8.0`. After that just build the project and it will generate .NET binaries, now reload Godot and enable the addon - it should now read the scripts correctly.
* I see the recording above that shows how planks are being dragged by each other, but my rope just streches instead. What am I doing wrong?
  * You doing everything correctly, it just that `VerletRope` only provides visuals - it will not move the bodies on it's own. See the possible solution in the [[following thread]](https://github.com/Tshmofen/verlet-rope-4/issues/5).

# Export documentation

### Basics
| Export variable | How it works |
|--|--|
| Attach Start   | Determines if the start point is fixed in place. |
| Attach End     | A link to any Node3D, if it is set up, the end of the rope will be folliwing this node. |
| Rope Length    | Length. |
| Rope Width     | Width. Ropes are flat, but always look at the camera, so width effectively behaves as a diameter.|
| Simulation Particles | Number of particles to simulate the rope. Odd number (greater than 3) is recommended for ropes attached on both sides for a smoother rope at its lowest point. |
| Use Visible On Screen Notifier | If enabled, rope will create own notifier in `_Ready` to stop drawing itself when not visible. Shows warning when disabled. |

### Simulation
| Export variable | How it works |
|--|--|
| Simulation Rate| Amount of rope calculations per second. Cannot excede physics rate,  if physics rate is 60 and rope simulation rate is 100, it is still gonna be updated only 60 times per second. Should be decreased when rope is not moving much or is far away to save some perfomance. |
| Iterations     | Number of verlet constraint iterations per frame, higher value gives accurate rope simulation for lengthy ropes with many simulation particles. Increase if you find the rope is sagging or stretching too much. |
| Preprocess Iterations| Number of iterations to be precalculated in `Ready()` to set the rope in a rest position. Value of 20-30 should be enough. |
| Stiffness      | AKA elasticity - it is a fraction that controls how much the verlet constraint corrects the rope. value from 0.1 to 1.0 is recommended. |
| Start Simulation From Start Point | When enabled makes all rope particles start from a single point, instead of a straight line with a length of the rope.  |
| Simulate       | Enables the simulation. Rope is still being drawn every frame if this is off. |
| Draw           | Enables the mesh drawing, you will still see the rope because `SurfacesClear()` wasnt called, but the rope isnt being drawn every frame. Rope is still being simulated if this is off. |
| Start Draw Simulation On Start | Will enable `Simulate` and `Draw` on the start of the game. Useful to not have moving ropes in editor. |
| Subdivision Lod Distance | Sets max distance where Catmull-Rom spline smoothing is applied for required segments. |

### Gravity
| Export variable | How it works |
|--|--|
| Apply Gravity  | Enables gravity. |
| Gravity        | Gravity direction vector. |
| Gravity Scale  | A factor to uniformly scale the gravity vector. |

### Wind
| Export variable | How it works |
|--|--|
| Apply Wind     | Applies wind noise. |
| Wind Noise     | Noise as a base for wind, noise period controls the turbulence (kinda). Use saved resource across different ropes for a global wind setting. |
| Wind           | The wind direction vector.|
| Wind Scale     | A factor to scale the wind direction. |

### Damping
| Export variable | How it works |
|--|--|
| Apply Damping  | Enables drag/damping. May help when rope bugs out by bringing it back to rest. |
| Damping Factor | Amount of damping. |

### Collision
| Export variable | How it works |
|--|--|
| Rope Collision Type | There are 3 collision types:<br/> `StaticOnly` makes rope only collide with bodies from `Static Collision Mask` that it is coming into, so if you try to move any body inside the rope it will not react (the most performance friendly mode); <br/>`DynamicOnly` makes rope react only to bodies from `Dynamic Collision Mask` and it will react when they are stumbling into the rope (more performant heavy);<br/> `All` just makes rope react with bodies from both masks, though, only dynamic one will be affecting rope when entering it (same performance as `DynamicOnly`). |
| Rope Collision Behavior | You can choose from 3 behaviors:<br/>`None` that is just disabling all collisions;<br/> `StickyStretch` that makes rope stick to the collision body till it exceeds max length and ignores collision to restore it's original length;<br/> `SlideStrech` that is the most advanced collision type, in that mode rope will stretch on collision as well, but instead of ignoring collision when it exceeds max length it will slide against collision normal towards expected position, if this movement is not enough to restore original rope length it will eventually ignore collision. Settings and max lengthes for collisions are set up using variables from below. |
| Max Rope Stretch | Range `1`-`20`. When is used with `StickyStretch` collision this settings determines max length of the rope before it starts ignoring collisions (with value `1` collisions are effectively disabled). When is used with `SlideStrech` this setting determines min length of the rope for it to start sliding on the collision normal (with value '1' it will be constantly sliding in different directions). |
| Slide Ignore Collision Stretch | Range `1`-`20`. Only applicable for `SlideStrech`: determines max length of the rope before it starts ignoring collisions (with value `1` collisions are effectively disabled). |
| Max Dynamic Collisions | Sets max amount of different bodies that will be taken into account for dynamic collisions. |
| Static Collision Mask | The collision layers that will be affecting rope physics when it stumbles into them. |
| Dynamic Collision Mask | The collision layers that will be affecting rope physics independently of situation. |
| Hit From Inside | Proxy for `RayCast3D` setting - enables collisions from inside the body.  |
| Hit Back Faces | Proxy for `RayCast3D` setting - enables collisions with backfaces of the surfaces. |

### Notes: 
* For `StickyStretch` it is recommended to use 1.5+ `Max Rope Strech` value, the default values are recommended for `SlideStretch`.
* Dynamic collisions are casting rays towards the center of the physics body, so it means that the best results will be with simple shapes like spheres, cylinders or capsules that are equally centered on the rigid body.
* Dynamic collisions are expected to be reacting with moving objects and for that reason are using much longer casts (leading to more jittery), so do not expect smooth slides as with static objects; also, keep in mind that best results are expected with 1-2 dynamic bodies in the scope, bigger amounts or fast moving objects might lead to collision ignoring. 

###### That's it, thanks for reading! (c) Tshmofen / Timofey Ivanov
