# Godot Verlet Rope

A fast implementation of verlet integration based rope pysics, similar to the one seen in half life 2.

# Quick Start
1. Add either rope node to the scene and set the AttachEndTo property in the inspector to any Spatial derived node (hint: use another rope node to chain the ropes or a position node to end the chain).
2. To use a custom rope texture you will have to set the Albedo of the material in the inpector. Use a tiling texture and set the UV0 x property to change the tiling.
3. The rope material has to set Cull Mode to disabled for the rope to render on both sides.
4. Play around with the rope parameters.
5. Rotations on rope have been disabled because rotating it doesn't correctly end the rope at the `attach_end_to` point. Rotations are also not really needed, just move the endpoints instead.
6. If you do make changes to GDVerletRope.gd or CSVerletRope.cs make sure to close and reopen the scene in the editor to stop some errors from piling up in the logs.

# Featues
1. Verlet integration based particle simulation
2. ImmediateGeometry to draw the rope
3. Full rope simulation within the editor, set Simulate to off to stop the simulation. 
4. Set number of particles, length, width and iterations for the rope.
5. PreprocessIterations processes the rope in _ready so it doesnt start out in a non rest position, a value of 20 is good enough, a higher value makes the scene load time longer.
6. Always faces the current camera in play mode
7. Automatically tesselates some sharp parts of the rope using Catmull rom splines to keep the rope looking smooth (use the SubdivLodDistance to change beyond what distance this tesselation stops).
8. Add a visibility notifier as a child to enable and disable drawing of the rope when the current camera is not viewing the rope. The rope automatically grows and shrinks the aabb, so you dont have to set the extents of the VisibilityNotifier yourself).
9. Enable/disable/configure forces like gravity, wind, air drag.
10. Rudimentary collisions using raycasts (only tests collisions if a collider enters its aabb), you need to enable this with ApplyCollisions.

# Rope export params and functions

| export variables | what it does |
|--|--|
| attach_start | attach/detach the start point |
| attach_end_to| attach end to any another `Spatial` by node path |
| rope_length  | length of the rope |
| rope_width   | width of the rope |
| simulation_particles | number of particles to simulate the rope. Odd number (greater than 3) is recommended for ropes attached on both sides for a smoother rope at its lowest point|
| iterations           | number of verlet constraint iterations per frame, higher value gives accurate rope simulation for lengthy and ropes with many simulation particles. Increase if you find the rope is sagging or stretching too much |
| preprocess_iterations| number of iterations to be precalculated in `_ready()` to set the rope in a rest position. Value of 20-30 should be enough. |
| simulation_rate| rate of simulation. lower the value if the rope is not going to move a lot or it is far away |
| stiffness      | should be named elasticity. it is a fraction that controls how much the verlet constraint corrects the rope. value from 0.1 to 1.0 is recommended |
| simulate       | on/off the simulation |
| draw           | on/off the drawing. you will still see the rope because `ImmediateGeometry.clear` wasnt called, but the rope isnt being drawn every frame. |
| subdiv_lod_distance | does catmull rom spline smoothing (for required segments) at distances less than this |
| apply_gravity  | on/off gravity |
| gravity        | gravity vector |
| gravity_scale  | a factor to scale the gravity vector |
| apply_wind     | on/off wind |
| wind_noise     | `OpenSimplexNoise` resource for the wind. noise period controls the turbulence(kinda). save resource to disk and share across ropes for a global wind setting. |
| wind           | the wind vector |
| wind_scale     | a factor to scale the wind vector |
| apply_damping  | on/off air drag/damping. sometimes helps when rope bugs out to bring it back to rest. |
| damping_factor | amount of damping |
| apply_collision| on/off collision with bodies. collisions work best on smooth surfaces without sharp edges. collisions are checked only when a body enters the ropes `AABB` (axis aligned bounding box)|
| collision_mask | the collision mask to be used for collisions |


| functions | what it does |
|--|--|
| get_end_location  | gets the coordinates of the end in global space |
| is_attached_start | is the start attached |
| is_attached_end   | is the end attached |


