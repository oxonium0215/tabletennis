using UnityEngine;
using StepUpTableTennis.TableTennisEngine.Collisions.Events;
using StepUpTableTennis.TableTennisEngine.Collisions.System;

namespace StepUpTableTennis.Training
{
    public class CollisionSoundManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource paddleAudioSource;
        [SerializeField] private AudioSource tableAudioSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip paddleHitSound;
        [SerializeField] private AudioClip tableHitSound;

        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float paddleHitVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float tableHitVolume = 1f;

        [Header("Pitch Variation")]
        [SerializeField] private bool usePitchVariation = true;
        [SerializeField, Range(0f, 0.2f)] private float pitchVariationRange = 0.1f;

        private void Start()
        {
            var sessionManager = GetComponent<TrainingSessionManager>();
            if (sessionManager == null)
            {
                Debug.LogError("CollisionSoundManager requires TrainingSessionManager component!");
                return;
            }

            // AudioSourceが設定されていない場合は自動的に作成
            if (paddleAudioSource == null)
            {
                paddleAudioSource = gameObject.AddComponent<AudioSource>();
                paddleAudioSource.playOnAwake = false;
                paddleAudioSource.spatialBlend = 1f; // 3Dサウンドとして設定
            }

            if (tableAudioSource == null)
            {
                tableAudioSource = gameObject.AddComponent<AudioSource>();
                tableAudioSource.playOnAwake = false;
                tableAudioSource.spatialBlend = 1f;
            }

            // TrainingSessionManager の OnCollision イベントに登録
            sessionManager.OnCollision += HandleCollision;
        }

        private void HandleCollision(CollisionEventArgs args)
        {
            // 衝突の種類に応じて適切な音を再生
            switch (args.CollisionInfo.Type)
            {
                case CollisionInfo.CollisionType.BallPaddle:
                    if (paddleHitSound != null)
                    {
                        PlaySound(paddleAudioSource, paddleHitSound, args.CollisionInfo.Point, paddleHitVolume);
                        // ボールを消す(下方向に吹き飛ばす)
                        // args.CollisionInfo.Ball.AddForce(Vector3.down * 1000f);
                    }
                    break;

                case CollisionInfo.CollisionType.BallBox:
                    if (tableHitSound != null)
                    {
                        PlaySound(tableAudioSource, tableHitSound, args.CollisionInfo.Point, tableHitVolume);
                    }
                    break;
            }
        }

        private void PlaySound(AudioSource source, AudioClip clip, Vector3 position, float volume)
        {
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume;

            if (usePitchVariation)
            {
                source.pitch = 1f + Random.Range(-pitchVariationRange, pitchVariationRange);
            }
            else
            {
                source.pitch = 1f;
            }

            source.Play();
        }

        // エディタ上でのデバッグ用
        public void TestPaddleSound()
        {
            if (paddleHitSound != null && paddleAudioSource != null)
            {
                PlaySound(paddleAudioSource, paddleHitSound, transform.position, paddleHitVolume);
            }
        }

        public void TestTableSound()
        {
            if (tableHitSound != null && tableAudioSource != null)
            {
                PlaySound(tableAudioSource, tableHitSound, transform.position, tableHitVolume);
            }
        }

        private void OnDestroy()
        {
            // イベントリスナーの解除
            if (TryGetComponent<TrainingSessionManager>(out var sessionManager))
            {
                sessionManager.OnCollision -= HandleCollision;
            }
        }
    }
}
