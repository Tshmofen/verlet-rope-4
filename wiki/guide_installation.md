# Installation
**This page covers the installation process for the `Verlet Rope 4` addon in Godot 4.4 .NET**. You can install just the addon for use in your own projects or download the entire demo repository to see usage examples and verify that your system has all the prerequisites installed correctly.

## Prerequisites
Before installing the addon, ensure your system meets the following requirements:
- **Godot**: You must have `Godot 4.4 .NET` or a later version installed. You can download it from the official [Godot Engine download page](https://godotengine.org/download/windows/) for your operating system, refer to the `.NET` versions on that page.
- **.NET SDK**: You need the `.NET 8 SDK` or newer version installed on your system. It can be downloaded from the official [Microsoft .NET](https://dotnet.microsoft.com/en-us/download) page. The SDK is required to compile the addon code at least once for both debug and export versions.

> [!NOTE]
> Currently installed version of the Godot can be viewed via `Help` -> `About Godot...` menu - it should say `v[4.4+].stable.mono.official`.
>
> <img width="400" alt="Godot Version Path" src="https://github.com/user-attachments/assets/5e3e3d00-b386-4e2e-ad81-b9fd9d6db8e6" />
> <img width="400" alt="Godot Version Panel" src="https://github.com/user-attachments/assets/a8eaddea-cfdf-4327-9138-2b0077fd3f6e" />

## Addon
To install the Verlet Rope addon into your existing Godot project, follow those steps:

1. **Download**
   * Navigate to the [Releases page](https://github.com/Tshmofen/verlet-rope-4/releases).
   * Download the latest `Source Code (zip)` archive.

2. **Extract**
   * Extract the contents of the downloaded ZIP file into any directory.
   * Copy the `addons/verlet_rope_4` folder from the extracted archive into your Godot project's `addons/` directory (create if doesn't exists).
   * Make sure that plugin was installed correctly by checking existence of the `plugin.cfg` file in the following path: `[YourProject]\addons\verlet_rope_4\plugin.cfg`. 

3. **Build .NET project**
   * For addon to be accessible, first build the C# code via any of the following methods:
     - Open `MSBuild` tab on the bottom and click `Build` icon to the right.
     - Click the `Build` button on the top-right menu next to `Play` button.
     - Just use your pre-configured IDE to rebuild the binaries.
     - Press `alt + B` shortcut to trigger the build process.
       <br/><br/>
       <img width="400" alt="Build Example" src="https://github.com/user-attachments/assets/8492b177-7934-4a40-8650-58ae830317c4" />

4. **Enable**
   * Open your project in Godot.
   * Go to `Project -> Project Settings...`.
   * Navigate to the `Plugins` tab.
   * Find the **Verlet Rope 4** entry in the list and check the `Enable` checkbox next to it.
   * Reload the project.
     <br/><br/>
     <img width="400" alt="Plugin Enable Example" src="https://github.com/user-attachments/assets/4e61cf47-7611-4c90-a427-0eed3de1c525" />

After completing these steps, all the related rope nodes will be available in your project's `Create New Node` dialog.

## Demo
To see examples of all configured nodes, you can download and checkout the demo project:
1. **Download**
   * **Option 1: Clone via Git**:
     Open a terminal or Git client and run in the folder of your choice:
     ```bash
     git clone https://github.com/Tshmofen/verlet-rope-4.git
     ```
   * **Option 2: Download as ZIP**:
     - Visit the [main repository page](https://github.com/Tshmofen/verlet-rope-4)
     - Open`Code` section and click `Download ZIP` link.
     - Extract the contents of this ZIP file to any folder on your computer.

2. **Startup**
   * Navigate to the extracted folder.
   * Open `project.godot` file using `Godot 4.4 .NET` app.

3. **Build .NET project**:
   * For demo to be accessible, first build the C# code via any of the following methods:
     - Open `MSBuild` tab on the bottom and click `Build` icon to the right.
     - Click the `Build` button on the top-right menu next to `Play` button.
     - Just use your pre-configured IDE to rebuild the binaries.
     - Press `alt + B` shortcut to trigger the build process.
       <br/><br/>
       <img width="400" alt="Build Example" src="https://github.com/user-attachments/assets/8492b177-7934-4a40-8650-58ae830317c4" />

4. **Reload the project**

5. **Explore**:
   * After a successful build navigate to `demo/demo.tscn` scene within the `FileSystem` panel.
     - It showcases all the available nodes configured in one scene.
     - To see physical ropes and some moving parts in action, just hit the `Play` button.

<sup>That's it, enjoy cool ropes!</sup>