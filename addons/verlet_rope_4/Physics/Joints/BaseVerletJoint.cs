using Godot;

namespace VerletRope4.Physics.Joints;

public abstract partial class BaseVerletJoint : Node, ISerializationListener
{
    /// <summary> A body that will be joined to the start of the Rope - by default <see cref="Node3D.GlobalPosition"/> is used as connection point. </summary>
    public abstract PhysicsBody3D StartBody { get; set; }

    /// <summary>
    /// A custom location for the start of the Rope. If <see cref="StartBody"/> is specified,
    /// used as custom joint location for physics calculations - otherwise behaves as simple start particle <see cref="Node3D.GlobalPosition"/> constraint.
    /// </summary>
    public abstract Node3D StartCustomLocation { get; set; }
    
    /// <summary> A body that will be joined to the end of the Rope - by default <see cref="Node3D.GlobalPosition"/> is used as connection point. </summary>
    public abstract PhysicsBody3D EndBody { get; set; }

    /// <summary>
    /// A custom location for the end of the Rope. If <see cref="EndBody"/> is specified,
    /// used as custom joint location for physics calculations - otherwise behaves as simple start particle <see cref="Node3D.GlobalPosition"/> constraint.
    /// </summary>
    public abstract Node3D EndCustomLocation{ get; set; }

    /// <summary> Resets the joined rope and all joint properties, have to be called after any property changes. It is being called when you press Reset Joint quick button. </summary>
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