using Godot;
using VerletRope4.Data;

namespace VerletRope4.Physics.Presets;

public static class VerletRopeSimulatedPreset
{
    #if TOOLS

    public static void SetBaseWindValues(VerletRopeSimulated verletRope, EditorUndoRedoManager undoRedo, int actionId)
    {
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.ApplyWind, true);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.WindDirection, Vector3.Back * 100.0f);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.WindNoiseMin, 0.05f);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.WindNoiseMax, 1.0f);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.WindNoise, new FastNoiseLite { Frequency = 0.03f });
        undoRedo.AddDoMethod(verletRope, VerletRopeSimulated.MethodName.CreateRope);
        
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.ApplyWind, verletRope.ApplyWind);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.WindDirection, verletRope.WindDirection);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.WindNoiseMin, verletRope.WindNoiseMin);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.WindNoiseMax, verletRope.WindNoiseMax);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.WindNoise, verletRope.WindNoise);
        undoRedo.AddUndoMethod(verletRope, VerletRopeSimulated.MethodName.CreateRope);
    }

    public static void SetFloatingValues(VerletRopeSimulated verletRope, EditorUndoRedoManager undoRedo, int actionId)
    {
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.Stiffness, 0.5f);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.StiffnessIterations, 2);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.ApplyDamping, true);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.DampingFactor, 2000.0f);
        undoRedo.AddDoMethod(verletRope, VerletRopeSimulated.MethodName.CreateRope);
        
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.Stiffness, verletRope.Stiffness);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.StiffnessIterations, verletRope.StiffnessIterations);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.ApplyDamping, verletRope.ApplyDamping);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.DampingFactor, verletRope.DampingFactor);
        undoRedo.AddUndoMethod(verletRope, VerletRopeSimulated.MethodName.CreateRope);
    }

    public static void SetBaseAllCollisionsValues(VerletRopeSimulated verletRope, EditorUndoRedoManager undoRedo, int actionId)
    {
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.RopeCollisionType, (int) RopeCollisionType.All);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.RopeCollisionBehavior, (int) RopeCollisionBehavior.SlideStretch);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.SlideCollisionStretch, 1.05f);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.IgnoreCollisionStretch, 5.0f);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.MaxDynamicCollisions, 4);
        undoRedo.AddDoProperty(verletRope, VerletRopeSimulated.PropertyName.DynamicCollisionTrackingMargin, 1.0f);
        undoRedo.AddDoMethod(verletRope, VerletRopeSimulated.MethodName.CreateRope);
        
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.RopeCollisionType, (int) verletRope.RopeCollisionType);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.RopeCollisionBehavior, (int) verletRope.RopeCollisionBehavior);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.SlideCollisionStretch, verletRope.SlideCollisionStretch);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.IgnoreCollisionStretch, verletRope.IgnoreCollisionStretch);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.MaxDynamicCollisions, verletRope.MaxDynamicCollisions);
        undoRedo.AddUndoProperty(verletRope, VerletRopeSimulated.PropertyName.DynamicCollisionTrackingMargin, verletRope.DynamicCollisionTrackingMargin);
        undoRedo.AddUndoMethod(verletRope, VerletRopeSimulated.MethodName.CreateRope);
    }

    #endif
}
