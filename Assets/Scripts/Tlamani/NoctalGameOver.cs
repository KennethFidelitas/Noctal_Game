
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Noctal
{
    public sealed class NoctalGameOver : MonoBehaviour
    {
        [SerializeField] private TlamaniVitals player;
        [SerializeField] private float delayBeforeGameOver = 2.5f;

        private bool transitionStarted;

        private void OnEnable()
        {
            if (player != null)
                player.Died += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            if (player != null)
                player.Died -= HandlePlayerDeath;
        }

        private void HandlePlayerDeath()
        {
            if (transitionStarted) return;

            transitionStarted = true;
            StartCoroutine(ShowGameOver());
        }

        private IEnumerator ShowGameOver()
        {
            yield return new WaitForSecondsRealtime(delayBeforeGameOver);

            Time.timeScale = 1f;
            SceneManager.LoadScene("GameOver");
        }
    }
}
