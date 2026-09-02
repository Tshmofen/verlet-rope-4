namespace VerletRope4.Data;

/// <summary> Determines how the rope's rendering is updated. </summary>
public enum RopeInterpolationType
{
    /// <summary>
    /// The rope is only drawn during physics ticks. Moving the rope node in `_Process` may cause a frame flickers
    /// because the start particle's position lags behind the new transform until the next physics step.
    /// </summary>
    None,

    /// <summary>
    /// The rope is drawn during physics frame, but if the node's global position changes, the mesh is immediately
    /// redrawn in the process frame. This eliminates flicker while leaving the rest of the rope behavior and performance intact.
    /// </summary>
    GlobalMovement,

    /// <summary>
    /// The rope is being drawn every process frame instead of physics frame. This is the smoothest mode,
    /// but may incur a small performance overhead compared to the other options.
    /// </summary>
    Always
}
