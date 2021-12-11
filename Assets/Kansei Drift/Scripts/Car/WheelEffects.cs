using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelEffects : MonoBehaviour
{
    public GameObject smokePrefab;
    public AudioSource tireAudioSource;
    public AudioClip skidSound;
    [SerializeField] private CarController controller;
    private Transform[] wheelTranforms = new Transform[4];
    private WheelCollider[] wheelColliders = new WheelCollider[4];
    private GameObject[] smokes;
    private ParticleSystem[] smokeParticles;
    private TrailRenderer[] skidMarks;
    [SerializeField] private bool[] tireLossesTraction = {false};
    private bool playSkid,once;
    [SerializeField]private float[] tireSlip = {0f};
    public float slipLimit;

    private void Awake()
    {
        controller = transform.GetComponent<CarController>();
        wheelTranforms = new Transform[controller.WheelTransforms.Length];
        wheelColliders = new WheelCollider[controller.WheelColliders.Length];
        tireAudioSource = transform.GetComponent<AudioSource>();
        smokes = new GameObject[wheelTranforms.Length];
        smokeParticles = new ParticleSystem[wheelTranforms.Length];
        skidMarks = new TrailRenderer[wheelTranforms.Length];
        tireLossesTraction = new bool[wheelTranforms.Length];
        tireSlip = new float[wheelTranforms.Length];
    }

    private void Start()
    {
        for(int i = 0; i < wheelColliders.Length; i++)
        {
            wheelColliders[i] = controller.WheelColliders[i];
        }

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            wheelTranforms[i] = controller.WheelTransforms[i];
        }

        for (int i = 0; i < 4; i++)
        {
            Vector3 pos;
            Quaternion quat;
            wheelColliders[i].GetWorldPose(out pos, out quat);

            quat.y = 180;

            smokes[i] = Instantiate(smokePrefab, pos, quat, wheelTranforms[i]);
            smokeParticles[i] = smokes[i].GetComponent<ParticleSystem>();
            skidMarks[i] = smokeParticles[i].GetComponent<TrailRenderer>();
        }
    }

    private void Update()
    {
        if(!once)
        {
            tireAudioSource.clip = skidSound;
            tireAudioSource.loop = true;
            tireAudioSource.Play();
            once = true;
        }
    }

    private void FixedUpdate()
    {
        PlaySmoke();
        EmitteSkidMarks();
    }

    private void PlaySmoke()
    {
        WheelHit hit;
        for (int i = 0; i < wheelTranforms.Length; i++)
        {
            if (wheelColliders[i].GetGroundHit(out hit))
            {
                if (hit.forwardSlip > slipLimit || hit.sidewaysSlip > slipLimit)
                {
                    if (!smokeParticles[i].isPlaying)
                    {
                        smokeParticles[i].Play();
                    }
                }
                else
                {
                    if (!smokeParticles[i].isStopped)
                    {
                        smokeParticles[i].Stop();
                    }
                }
            }
        }
    }

    private void EmitteSkidMarks()
    {
        WheelHit hit;

        for (int i = 0; i < wheelTranforms.Length; i++)
        {
            if(wheelColliders[i].GetGroundHit(out hit))
            {
                if (hit.forwardSlip > slipLimit || hit.sidewaysSlip > slipLimit)
                {
                    if (tireLossesTraction[i])
                    {
                        return;
                    }
                    else
                    {
                        tireLossesTraction[i] = true;
                    }
                }
                else
                {
                    if (!tireLossesTraction[i])
                    {
                        return;
                    }
                    else
                    {
                        tireLossesTraction[i] = false;
                    }
                }
                tireSlip[i] = hit.forwardSlip;
            }
        }

        for (int i = 0; i < tireLossesTraction.Length; i++)
        {
            if (tireLossesTraction[i])
            {
                playSkid = true;
                skidMarks[i].emitting = true;
            }
            else
            {
                playSkid = false;
                skidMarks[i].emitting = false;
            }
        }

        if (playSkid)
        {
            tireAudioSource.mute = false;
        }
        else
        {
            tireAudioSource.mute = true;
        }
    }
}