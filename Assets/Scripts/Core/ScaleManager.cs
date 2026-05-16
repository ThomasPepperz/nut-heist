using UnityEngine;

// ScaleManager.cs
// Central authority for world scale calculations.
// SQUIRREL HEIGHT = 0.25 Unity units = ~25 cm real-world feel.
namespace NutHeist.Core
{
    public static class ScaleManager
    {
        public const float SQUIRREL_HEIGHT = 0.25f;
        public const float SQUIRREL_WIDTH = 0.15f;
        public const float REAL_METER_TO_UNIT = 1.0f;

        public const float BRICK_HEIGHT = 0.057f;
        public const float DOOR_HANDLE_HEIGHT = 1.0f;
        public const float COUNTER_HEIGHT = 0.9f;
        public const float CEILING_HEIGHT_FLOOR1 = 2.4f;
        public const float TREE_HEIGHT_OAK = 15.0f;
        public const float FENCE_HEIGHT = 1.8f;
        public const float VENT_DIAMETER = 0.35f;

        /*
         Scale checklist / blockout QA (abbrev.):
          - Terrain/world props should cite real-meter targets first, then multiply by REAL_METER_TO_UNIT.
          - Verify fence ≈ SquirrelHeights(FENCE_HEIGHT) squirrel-heights (~7×) for readability.
          - Oak tree height should read like a skyline (TREE_HEIGHT_OAK).
          - Vents/air volumes should honour VENT_DIAMETER (~1.4× squirrel height corridor).
          - Keep CharacterController/skin width tighter at squirrel scale (≈ 0.01f) elsewhere.
        */
        public static float ToUnits(float realWorldMeters) => realWorldMeters * REAL_METER_TO_UNIT;

        public static float SquirrelHeights(float realWorldMeters) => realWorldMeters / SQUIRREL_HEIGHT;
    }
}

