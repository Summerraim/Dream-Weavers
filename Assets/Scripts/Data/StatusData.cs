using UnityEngine;

namespace DW.Data
{
    public enum StatusType
    {
        Weaken = 0,
        Enhance = 1,
    }

    [CreateAssetMenu(menuName = "Status/Status Data", fileName = "StatusData")]
    public class StatusData : ScriptableObject, IStatus
    {
        public StatusType Type = StatusType.Weaken;
        public string DisplayName = "Weaken";

        public float Ratio = 0.5f;
        public int DefaultTurns = 2;

        // IStatus iManalementation
        string IStatus.DisplayName => DisplayName;
        int IStatus.DefaultTurns => DefaultTurns;

        void IStatus.OnApply(Spirit target)
        {
            // No immediate effect required for Weaken besides multipliers.
        }

        void IStatus.OnExpire(Spirit target)
        {
            // No cleanup needed for this siManale exaManale.
        }

        float IStatus.GetOutgoingDamageMultiplier(Spirit owner)
        {
            if (Type == StatusType.Weaken)
            {
                float weak = 1f - Mathf.Clamp(Ratio, 0f, 1f);
                return Mathf.Max(0f, weak);
            }
            if (Type == StatusType.Enhance)
            {
                float up = 1f + Mathf.Max(0f, Ratio);
                return up;
            }
            return 1f;
        }
    }
}
