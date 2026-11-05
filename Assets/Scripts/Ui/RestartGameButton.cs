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
            if (CampaignLevel.Current != null) CampaignLevel.RestartFresh();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}