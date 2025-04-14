using UnityEngine;

namespace DealerSelfSupplySystem.UI
{
    public static class Styling
    {
        // Divide RGB values by 255 to get the 0-1 range that Unity expects
        public static Color PRIMARY_COLOR = new Color(69f / 255f, 170f / 255f, 229f / 255f, 1f);
        public static Color SECONDARY_COLOR = new Color(239f / 255f, 251f / 255f, 255f / 255f, 1f);
        public static Color DESTRUCTIVE_COLOR = new Color(255f / 255f, 120f / 255f, 107f / 255f, 1f);
    }

    public enum EButtonType
    {
        Primary,
        Secondary,
        Destructive
    }
}