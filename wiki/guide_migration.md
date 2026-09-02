# Migration
This page describes the steps required to update from specific versions either by specifying what you need to adjust in the node settings to restore previous behavior, mentioning if something is unavailable or what settings you need to save and copy after update.

## Versioning
The addon uses the semantic-like versioning system, let's see what it means. We have `x.y.z` versions (e.g. `2.3.2`), each part change means the following: 
* `x` changes are big compatibility breaking refactoring or reworks, a lot of functionality was changed in non-compatible way.
* `y` changes are some functional changes that partially change the way addon works or have introduced some new functionality.
* `z` changes are just minor fixes, no actual breaking changes introduced.  

So this page covers migrations for any breaking changes, `x` or `y` ones.

## Migration Guide: From `v1.2.0` to `v2.0.0`

This guide outlines the update changes including: significant namespace updates, node renames, property adjustments and general improvements of structure and clarity.

### Summary

| v1.2.0 | v2.0.0 | Important Change Type |
| :--- | :--- | :--- |
| `VerletRope`       | `VerletRopeSimulated` | Node rename. |
| `Iterations`       | `StiffnessIterations` | Property rename. |
| `VerletRope4`      | `VerletRope4.[SubNamespaces]` | Namespaces split. |
| `Simulate`, `Draw` | `Simulation Behavior` (Enum)  | Property consolidation into single ENUM. |
| `Attach Start` / `Attach End` | `VerletRopeSimulatedJoint` Node | Paradigm shift from properties to separate node. |

### Steps

> [!NOTE]
> The guide expects you to already have new version installed, make sure to do that following [Installation Guide](https://github.com/Tshmofen/verlet-rope-4/wiki/Guide-%E2%80%90-Installation).

> [!IMPORTANT]
> Always use GIT versioning (or at very least backups) when you do any extensive version migrations for any addons.

#### Rope Naming
The most breaking change is the main node rename. `VerletRope` is now `VerletRopeSimulated`.

*For low amount of references it can be quickly migrated using built-in Godot tool:*
1. Open the scene with the rope in your Godot project, it will show the `Missing dependencies` error.
   <br/><br/>
   <img width="400" alt="Missing Dependencies Example" src="https://github.com/user-attachments/assets/bfb7ec63-d9de-4f88-a374-74d39cc0ead9" />
2. Click `Fix Dependencies` button, it will open the `Dependencies` window.
   <br/><br/>
   <img width="400" alt="Dependencies Example" src="https://github.com/user-attachments/assets/f327bffc-8693-4495-b620-23ccf818951b" />
3. Here navigate to `VerletRope.cs` row and click on `Folder` icon, make the script point to `./Physics/VerletRopeSimulated.cs` and click `Ok`.
4. Now that dependency was resolved you can click on `Open Anyway` button - all ropes now should not be giving any errors. (There might be a few console errors on the first opening)

*In case you need to update a lot of scenes the same can be done using any text editor (e.g. `Notepad++`)*
1. Open your text editor's search function (`ctrl + shift + f` for `Notepad++`)
2. Select `Find in files` or similar function and specify path to your project as search folder/directory.
3. Specify find argument as `res://addons/verlet_rope_4/VerletRope.cs` and replace as `res://addons/verlet_rope_4/Physics/VerletRopeSimulated.cs`.
4. Run the `Replace in files` function - it will fix the references in all the scenes.

#### Properties Naming
A bunch of properties have been renamed or re-categorized for better organization. They have to be readjusted again as now will be having default values.

> [!TIP]
> Alternatively the naming can be updated using the same text editor approach as in `Rope Node Rename` section.

* `Iterations` was renamed to`StiffnessIterations` to properly reflect the logic behind.
* `Simulate` and `Draw` booleans were replaced by `SimulationBehavior` ENUM.
* `Attach Start` and `Attach End` have been removed, attachments are now handled by dedicated child nodes. For `VerletRopeSimulated` it is `VerletJointSimulated`.

#### Namespace Changes (C#)
If you referenced any rope classes from your own C# scripts, the namespaces have changed and needs to be referenced again. To fix that just let your IDE to import any missing namespaces and remove erroring old ones.

The corresponding nodes and properties names were also updated as in the previous sections, they have to be fixed manually in the code.

> [!NOTE]
> The update also have introduced a bunch of new features, you can explore them all on the [Wiki Home](https://github.com/Tshmofen/verlet-rope-4/wiki) page or specific changes in the [Addon Releases](https://github.com/Tshmofen/verlet-rope-4/releases).