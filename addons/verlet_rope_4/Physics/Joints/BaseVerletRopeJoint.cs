using Godot;

namespace VerletRope4.Physics.Joints;

public abstract partial class BaseVerletRopeJoint : Node3D, ISerializationListener
{
    public abstract PhysicsBody3D StartBody { get; set; }
    public abstract Node3D StartJointCustomLocation{ get; set; }

    public abstract PhysicsBody3D EndBody { get; set; }
    public abstract Node3D EndJointCustomLocation{ get; set; }

    public abstract void ResetJoint();

    #region Script Reload

    public virtual void OnBeforeSerialize()
    {
        // Ignore unload
    }

    public virtual void OnAfterDeserialize()
    {
        // Recreate joint after script reload to make sure all physics related data (and updated logic) is reapplied.
        ResetJoint();
    }

    #endregion
}