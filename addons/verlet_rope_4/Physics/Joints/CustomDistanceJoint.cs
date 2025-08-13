using Godot;

namespace VerletRope.Physics.Joints;

[Tool]
public partial class CustomDistanceJoint : Generic6DofJoint3D
{
    [ExportToolButton("Reset Joint")] public Callable ResetJointButton => Callable.From(ResetJoint);

    [ExportCategory("Distance Joint Settings")]
    [Export] public PhysicsBody3D BodyA { get; set; }
    [Export] public Node3D CustomLocationA { get; set; }

    [Export] public PhysicsBody3D BodyB { get; set; }
    [Export] public Node3D CustomLocationB { get; set; }

    [Export(PropertyHint.Range,"0,10000")] public float MaxDistance { get; set; }
    [Export(PropertyHint.Range,"0.00,16")] public float UniformDamping { get; set; } = 0;
    [Export(PropertyHint.Range,"0.00,16")] public float UniformSoftness { get; set; } = 0;
    [Export(PropertyHint.Range,"0.00,16")] public float UniformRestitution { get; set; } = 0;

    public override void _Ready()
    {
        SetFlagX(Flag.EnableAngularLimit, false);
        SetFlagY(Flag.EnableAngularLimit, false);
        SetFlagZ(Flag.EnableAngularLimit, false);
        ResetJoint();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (MaxDistance == 0 || BodyA == null || BodyB == null)
        {
            return;
        } 

        var a = CustomLocationA?.GlobalPosition ?? BodyA.GlobalPosition;
        var b = CustomLocationB?.GlobalPosition ?? BodyB.GlobalPosition;
        GlobalPosition = a + (b - a) / 2f;
    }

    public void ResetJoint()
    {
        if (MaxDistance != 0)
        {
            var lowerLimit = -MaxDistance / 2f;
            var upperLimit = MaxDistance / 2f;
            SetParamX(Param.LinearLowerLimit, lowerLimit);
            SetParamX(Param.LinearUpperLimit, upperLimit);
            SetParamY(Param.LinearLowerLimit, lowerLimit);
            SetParamY(Param.LinearUpperLimit, upperLimit);
            SetParamZ(Param.LinearLowerLimit, lowerLimit);
            SetParamZ(Param.LinearUpperLimit, upperLimit);
        }

        if (UniformSoftness != 0)
        {
            SetParamX(Param.LinearLimitSoftness, UniformSoftness);
            SetParamY(Param.LinearLimitSoftness, UniformSoftness);
            SetParamZ(Param.LinearLimitSoftness, UniformSoftness);
        }

        if (UniformRestitution != 0)
        {
            
            SetParamX(Param.LinearRestitution, UniformRestitution);
            SetParamY(Param.LinearRestitution, UniformRestitution);
            SetParamZ(Param.LinearRestitution, UniformRestitution);
        }

        if (UniformDamping != 0)
        {
            SetParamX(Param.LinearDamping, UniformDamping);
            SetParamY(Param.LinearDamping, UniformDamping);
            SetParamZ(Param.LinearDamping, UniformDamping);
        }

        base.NodeA = BodyA?.GetPath();
        base.NodeB = BodyB?.GetPath();
    }
} 