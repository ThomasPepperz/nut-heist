namespace NutHeist.Performance
{
    /// <summary>In-editor reminder list for LOD, occlusion baking, primitive colliders, and profiling cadence.</summary>
    public static class ProfilingGuidelines
    {
        public const string LodPolicy =
            "Author LODGroups for hero props: LOD0 (<10 units), LOD1 (<30), LOD2 (<60), LOD3 billboard/cull.";
        public const string Occlusion =
            "Mark static meshes as Navigation/Occlusion Static, bake occlusion for basement + interior.";
        public const string Colliders =
            "Prefer box/sphere/capsule colliders; avoid mesh colliders on dynamic rigs.";
        public const string RayBudget =
            "Throttle IK probes (≤12 Hz) and consolidate ground checks to stay under raycast budgets.";
        public const string Builds =
            "Use Nut Heist → Builds after profiling Scenes/MainLevel standalone on target hardware.";
    }
}
