
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menucontroller : MonoBehaviour
{
    public void NuevaPartida()
    {
        Noctal.NoctalLevel.SavedCheckpointRoom = 0;
        SceneManager.LoadScene("Mecanicas_Noctal");
    }

    public void Salir()
    {
        Application.Quit();
        Debug.Log("Saliendo del juego...");
    }

    public void Reintentar()
    {
        Noctal.NoctalLevel level = Noctal.NoctalLevel.Instance;

        if (level != null)
        {
            level.Retry();
        }
        else
        {
            SceneManager.LoadScene("Mecanicas_Noctal");
        }
    }

    public void VolverAlMenu()
    {
        Noctal.NoctalLevel.SavedCheckpointRoom = 0;
        SceneManager.LoadScene("MainMenu");
    }
}
