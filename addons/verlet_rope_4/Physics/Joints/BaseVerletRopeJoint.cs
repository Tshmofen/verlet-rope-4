using Godot;

namespace VerletRope4.Physics.Joints;

public abstract partial class BaseVerletRopeJoint : Node, ISerializationListener
{
    public abstract PhysicsBody3D StartBody { get; set; }
    public abstract Node3D StartCustomLocation{ get; set; }

    public abstract PhysicsBody3D EndBody { get; set; }
    public abstract Node3D EndCustomLocation{ get; set; }

    public virtual void ResetJoint()
    {
        UpdateConfigurationWarnings();
    }

    public override string[] _GetConfigurationWarnings()
    {
        if (StartBody == null && StartCustomLocation == null && EndBody == null && EndCustomLocation == null)
        {
            return ["No custom bodies specified, joint is doing nothing - consider resetting or removing it."];
        }

        return [];
    }

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