using System.Collections;
using UnityEngine;

namespace EmberKnight.CameraSystem
{
    /// <summary>
    /// Segue o jogador horizontalmente com suavização e limites de sala
    /// (equivalente ao updateCamera()/camX do original). Se preferir,
    /// troque por uma Cinemachine Virtual Camera — o método Shake()
    /// continua útil como Impulse Source manual.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.15f;
        public Vector2 offset = new Vector2(0f, 1.2f);

        [Header("Limites da sala (em unidades de mundo)")]
        public float minX = 0f;
        public float maxX = 100f;
        public float halfViewWidth = 8f;

        Vector3 velocity;

        void LateUpdate()
        {
            if (target == null) return;

            float clampedX = Mathf.Clamp(target.position.x,
                minX + halfViewWidth, Mathf.Max(minX + halfViewWidth, maxX - halfViewWidth));

            Vector3 desired = new Vector3(clampedX + offset.x, offset.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }

        /// <summary>Define os limites quando uma nova sala é carregada.</summary>
        public void SetRoomBounds(float roomWidthWorldUnits)
        {
            minX = 0f;
            maxX = roomWidthWorldUnits;
        }

        public void Shake(float duration = 0.2f, float magnitude = 0.15f)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            Vector3 basePos = transform.position;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                Vector2 rnd = Random.insideUnitCircle * magnitude * (1f - t / duration);
                transform.position = basePos + (Vector3)rnd;
                yield return null;
            }
            transform.position = basePos;
        }
    }
}
