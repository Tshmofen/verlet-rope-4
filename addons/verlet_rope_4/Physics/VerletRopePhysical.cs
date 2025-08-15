using Godot;
using System.Collections.Generic;
using VerletRope.addons.verlet_rope_4;
using VerletRope4.Rendering;

namespace VerletRope.Physics;

[Tool]
public abstract partial class VerletRopePhysical : Node3D, ISerializationListener
{
    protected readonly List<Rid> CollisionExceptions = [];

    private VerletRopeMesh _verletRopeMesh;
    protected VerletRopeMesh VerletRopeMesh => _verletRopeMesh ??= this.FindOrCreateChild<VerletRopeMesh>();
    
    // Properties have the same default values as on `VerletRopeMesh`
    [ExportGroup("Visuals")]
    [Export] public float RopeLength { get; set; } = 3.0f;
    [Export] public float RopeWidth { get; set; } = 0.07f;
    [Export] public float SubdivisionLodDistance { get; set; } = 15.0f;
    [Export] public bool UseVisibleOnScreenNotifier { get; set; } = true;
    [Export] public Material MaterialOverride { get; set; }

    public virtual void ClearExceptions()
    {
        CollisionExceptions.Clear();
    }

    public virtual void RegisterExceptionRid(Rid rid, bool toInclude)
    {
        if (toInclude)
        {
            CollisionExceptions.Add(rid);
        }
        else
        {
            CollisionExceptions.Remove(rid);
        }
    }

    public virtual void CreateRope()
    {
        VerletRopeMesh.RopeLength = RopeLength;
        VerletRopeMesh.RopeWidth = RopeWidth;
        VerletRopeMesh.SubdivisionLodDistance = SubdivisionLodDistance;
        VerletRopeMesh.UseVisibleOnScreenNotifier = UseVisibleOnScreenNotifier;
        VerletRopeMesh.MaterialOverride = MaterialOverride;
    }

    public virtual void DestroyRope() { }

    #region Script Reload

    public void OnBeforeSerialize()
    {
        // Ignore unload
    }

    public void OnAfterDeserialize()
    {
        CreateRope();
    }

    #endregion
}