
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menucontroller : MonoBehaviour
{
    public void NuevaPartida()
    {
        SceneManager.LoadScene("Mecanicas_Noctal");
    }

    public void Salir()
    {
        Application.Quit();
        Debug.Log("Saliendo del juego...");
    }

    public void Reintentar()
    {
        SceneManager.LoadScene("Mecanicas_Noctal");
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
