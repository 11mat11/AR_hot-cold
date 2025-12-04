using UnityEngine;

/// <summary>
/// Defines the four elemental types used in the hot-cold AR game.
/// Each element can have unique visuals and audio characteristics.
/// </summary>
public enum ElementType
{
    Water,
    Fire,
    Earth,
    Air
}

/// <summary>
/// Serializable configuration for an elemental sphere prefab.
/// Allows easy management of element prefabs in the inspector.
/// </summary>
[System.Serializable]
public class ElementPrefabConfig
{
    [Tooltip("The type of element this prefab represents")]
    public ElementType elementType;

    [Tooltip("The prefab GameObject with SpatialSound component attached")]
    public GameObject prefab;

    [Tooltip("Optional: Override audio clip (if not set on prefab)")]
    public AudioClip audioClip;
}
