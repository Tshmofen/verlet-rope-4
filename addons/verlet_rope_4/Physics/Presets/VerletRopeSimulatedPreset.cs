using Godot;
using VerletRope4.Data;

namespace VerletRope4.Physics.Presets;

public static class VerletRopeSimulatedPreset
{
    public static void SetStandardValues(VerletRopeSimulated verletRope, EditorUndoRedoManager undoRedo, int actionId)
    {
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.SimulationParticles, 10);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.Stiffness, 0.9f);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.StiffnessIterations, 2);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.ApplyGravity, true);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.Gravity, Vector3.Down * 9.8f);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.GravityScale, 1.0f);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.ApplyWind, false);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.ApplyDamping, true);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.DampingFactor, 1.0f);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.RopeCollisionBehavior, (int) RopeCollisionBehavior.None);
        undoRedo.AddDoMethod(verletRope, VerletRopeSimulated.MethodName.CreateRope);
        
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.SimulationParticles, verletRope.SimulationParticles);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.Stiffness, verletRope.Stiffness);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.StiffnessIterations, verletRope.StiffnessIterations);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.ApplyGravity, verletRope.ApplyGravity);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.Gravity, verletRope.Gravity);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.GravityScale, verletRope.GravityScale);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.ApplyWind, verletRope.ApplyWind);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.ApplyDamping, verletRope.ApplyDamping);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.DampingFactor, verletRope.DampingFactor);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.RopeCollisionBehavior, (int) verletRope.RopeCollisionBehavior);
    }
}
