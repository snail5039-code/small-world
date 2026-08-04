using System;
using UnityEngine;

namespace SmallWorld.Player
{
    public sealed class PlayerFootstepEmitter : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField, Min(0.1f)] private float walkStepDistance = 2.2f;
        [SerializeField, Min(0.1f)] private float sprintStepDistance = 2.3f;
        private float distanceUntilStep;
        private AudioClip generatedClip;

        public event Action<Vector3> Step;

        public AudioSource AudioSource => audioSource;

        public void Configure(AudioSource source) => audioSource = source;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource != null && audioSource.clip == null)
            {
                generatedClip = CreatePlaceholderFootstep();
                audioSource.clip = generatedClip;
            }
        }

        public void Tick(float horizontalDistance, bool grounded, bool sprinting)
        {
            if (!grounded || horizontalDistance <= 0f) return;
            distanceUntilStep -= horizontalDistance;
            if (distanceUntilStep > 0f) return;

            distanceUntilStep = sprinting ? sprintStepDistance : walkStepDistance;
            Step?.Invoke(transform.position);
            if (audioSource != null && audioSource.clip != null) audioSource.PlayOneShot(audioSource.clip);
        }

        public void ResetCadence()
        {
            distanceUntilStep = 0f;
        }

        private void OnDestroy()
        {
            if (generatedClip != null) Destroy(generatedClip);
        }

        private static AudioClip CreatePlaceholderFootstep()
        {
            const int sampleRate = 22050;
            const int sampleCount = 1323;
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Exp(-time * 55f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 95f * time) * envelope * 0.18f;
            }

            AudioClip clip = AudioClip.Create("Generated Footstep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
