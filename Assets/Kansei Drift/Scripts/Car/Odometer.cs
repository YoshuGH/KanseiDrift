using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Odometer : MonoBehaviour
{
    private enum MeterType 
    {
        [InspectorName("Tacometer")] rpm, 
        [InspectorName("Speedometer")] velocity
    }

    private enum DisplayType
    {
        [InspectorName("Gears")] gears,
        [InspectorName("Speed")] speed,
        [InspectorName("RPM")] revolutions
    }

    [SerializeField] private MeterType meterType;
    [SerializeField] private DisplayType displayType;
    [SerializeField] private float startPosition, endPosition;
    private float desirePosition;

    private Transform needle;
    public Text display;

    private CarController carController;
    // Start is called before the first frame update
    void Start()
    {
        carController = GameObject.FindGameObjectWithTag("Player").GetComponent<CarController>();
        needle = transform.Find("Needle");
    }

    // Update is called once per frame
    void Update()
    {
        desirePosition = startPosition - endPosition;
        float tempSpeed = 0;

        switch (meterType)
        {
            case 0:
                tempSpeed = (carController.EngineRPM / 1000) / 6;
                break;
            case (MeterType)1:
                tempSpeed = carController.KMPH / 260;
                break;
        }
        
        switch(displayType)
        {
            case 0:
                if (display != null)
                {
                    if(carController.IsReverse)
                    {
                        display.text = "R";
                    }
                    else
                    {
                        display.text = (carController.Gear + 1).ToString();
                    }
                }   
                break;
            case (DisplayType)1:
                if (display != null)
                    display.text = carController.KMPH.ToString("f0");
                break;
            case (DisplayType)2:
                if (display != null)
                    display.text = carController.EngineRPM.ToString();
                break;
        }

        needle.eulerAngles = new Vector3(0,0, startPosition - tempSpeed * desirePosition);
    }
}
