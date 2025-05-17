using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private AudioClip menuMusic; // Assign this in the Inspector
    [SerializeField] private float fadeOutDuration = 1.0f; // How long the fade out should take
    
    private AudioSource audioSource;
    
    private void Start()
    {
        // Create an AudioSource component if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure the audio source
        audioSource.clip = menuMusic;
        audioSource.loop = true;
        audioSource.Play();
    }
    
    public void PlayGame()
    {
        // Start coroutine to fade out music before scene transition
        StartCoroutine(FadeOutAndLoadScene(1));
    }
    
    public void QuitGame()
    {
        // Start coroutine to fade out music before quitting
        StartCoroutine(FadeOutAndQuit());
    }
    
    private IEnumerator FadeOutAndLoadScene(int sceneIndex)
    {
        // Only proceed with fade if we have an audio source with a clip playing
        if (audioSource != null && audioSource.isPlaying)
        {
            float startVolume = audioSource.volume;
            
            // Gradually reduce the volume
            for (float t = 0; t < fadeOutDuration; t += Time.deltaTime)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeOutDuration);
                yield return null;
            }
            
            // Ensure volume is set to 0 at the end
            audioSource.volume = 0;
            audioSource.Stop();
        }
        
        // Load the next scene
        SceneManager.LoadSceneAsync(sceneIndex);
    }
    
    private IEnumerator FadeOutAndQuit()
    {
        // Only proceed with fade if we have an audio source with a clip playing
        if (audioSource != null && audioSource.isPlaying)
        {
            float startVolume = audioSource.volume;
            
            // Gradually reduce the volume
            for (float t = 0; t < fadeOutDuration; t += Time.deltaTime)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeOutDuration);
                yield return null;
            }
            
            // Ensure volume is set to 0 at the end
            audioSource.volume = 0;
            audioSource.Stop();
        }
        
        // Quit the application
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}