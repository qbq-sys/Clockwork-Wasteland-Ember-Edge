using UnityEngine;

public class AudioSourceTest : MonoBehaviour
{
    public AudioClip clip;

    private void Start()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = 1f;
        source.mute = false;
        source.spatialBlend = 0f; // 2D
        source.playOnAwake = false;
        source.loop = false;

        source.Play();

        Debug.Log($"AudioSource ²¥·Å×´Ì¬: clip={clip?.name}, isPlaying={source.isPlaying}, volume={source.volume}, listenerVolume={AudioListener.volume}, pause={AudioListener.pause}");
    }
}