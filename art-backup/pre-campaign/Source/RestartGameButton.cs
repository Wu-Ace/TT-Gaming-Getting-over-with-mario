using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ui
{
    public class RestartGameButton : MonoBehaviour
    {
        [UsedImplicitly]
        public void Perform()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}