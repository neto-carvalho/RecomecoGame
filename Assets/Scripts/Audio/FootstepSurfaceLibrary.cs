using System;
using UnityEngine;

/// <summary>Biblioteca de clips por superfície (andar / correr).</summary>
[CreateAssetMenu(fileName = "FootstepSurfaceLibrary", menuName = "Recomeco/Audio/Footstep Surface Library")]
public sealed class FootstepSurfaceLibrary : ScriptableObject
{
    [Serializable]
    public struct SurfaceClips
    {
        public FootstepSurfaceType surface;
        public AudioClip[] walk;
        public AudioClip[] run;

        public bool HasWalk => walk != null && walk.Length > 0;
        public bool HasAny => HasWalk || (run != null && run.Length > 0);
    }

    [SerializeField] FootstepSurfaceType defaultSurface = FootstepSurfaceType.Tile;
    [SerializeField] SurfaceClips[] surfaces = Array.Empty<SurfaceClips>();

    public FootstepSurfaceType DefaultSurface => defaultSurface;

    public bool TryGetClips(FootstepSurfaceType surface, bool running, out AudioClip[] clips)
    {
        clips = null;
        if (TryGetEntry(surface, out var entry) && entry.HasWalk)
        {
            clips = running ? entry.run : entry.walk;
            if (clips != null && clips.Length > 0)
                return true;
        }

        if (surface != defaultSurface && TryGetEntry(defaultSurface, out entry) && entry.HasWalk)
        {
            clips = running ? entry.run : entry.walk;
            return clips != null && clips.Length > 0;
        }

        if (surfaces.Length > 0 && surfaces[0].HasWalk)
        {
            clips = running ? surfaces[0].run : surfaces[0].walk;
            return clips != null && clips.Length > 0;
        }

        return false;
    }

    bool TryGetEntry(FootstepSurfaceType surface, out SurfaceClips entry)
    {
        foreach (var s in surfaces)
        {
            if (s.surface != surface)
                continue;
            entry = s;
            return true;
        }

        entry = default;
        return false;
    }

#if UNITY_EDITOR
    public void EditorSetSurfaces(SurfaceClips[] newSurfaces, FootstepSurfaceType newDefault)
    {
        surfaces = newSurfaces;
        defaultSurface = newDefault;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
