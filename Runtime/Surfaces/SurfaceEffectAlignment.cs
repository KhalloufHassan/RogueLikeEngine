namespace RogueLikeEngine.Surfaces
{
    /// <summary>Which way a surface effect's VFX faces.</summary>
    public enum SurfaceEffectAlignment
    {
        /// <summary>Out of the surface, along its normal.</summary>
        SurfaceNormal,
        /// <summary>Back toward where the hit came from, against its direction.</summary>
        AgainstDirection
    }
}
