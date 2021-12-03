using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class WinCondition : MonoBehaviour
{
    private Collider[] collidersVictoria;
    private CarController controller;

    public GameObject VictoryPanel, speedoPanel;

    // Start is called before the first frame update
    void Start()
    {
        collidersVictoria = new Collider[transform.childCount];
        controller = GameObject.FindGameObjectWithTag("Player").GetComponent<CarController>();

        for(int i = 0; i < transform.childCount; i++)
        {
            collidersVictoria[i] = transform.GetChild(i).GetComponent<Collider>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(controller.Checkpoints > collidersVictoria.Length)
        {
            VictoryPanel.SetActive(true);
            speedoPanel.SetActive(false);
            controller.DisableControls = true;
        }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("Menu");
    }


}
