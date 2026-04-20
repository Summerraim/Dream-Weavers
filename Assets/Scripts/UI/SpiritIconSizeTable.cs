using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpiritIconSizeEntry
{
    public string spiritName;
    public Vector2 iconSize;
}

[CreateAssetMenu(fileName = "Data", menuName = "Data/Spirit Icon Size Table")]
public class SpiritIconSizeTable : ScriptableObject
{
    [SerializeField]
    private List<SpiritIconSizeEntry> entries = new List<SpiritIconSizeEntry>();

    public bool TryGetSize(string spiritName, out Vector2 iconSize)
    {
        spiritName = string.IsNullOrWhiteSpace(spiritName) ? string.Empty : spiritName.Trim();
        if (string.IsNullOrEmpty(spiritName))
        {
            iconSize = Vector2.zero;
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            SpiritIconSizeEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.spiritName))
            {
                continue;
            }

            if (string.Equals(entry.spiritName.Trim(), spiritName, StringComparison.OrdinalIgnoreCase))
            {
                iconSize = entry.iconSize;
                return true;
            }
        }

        iconSize = Vector2.zero;
        return false;
    }
}
