using UnityEngine;
using System.Collections;
using System;

public class StreamingMic : MonoBehaviour {
    private int m_nRecordingRoutine = 0;
    private string m_sMicrophoneID = null;
    private AudioClip m_acRecording = null;
    private int m_nRecordingBufferSize = 1;
    public int m_nRecordingHZ = 16000;

    [SerializeField]
    //private WebSocketClient m_webSocketClient;
    public float m_level;

    void Start() {
        StartRecording();
    }

    private void StartRecording() {
        if (m_nRecordingRoutine == 0) {
            //UnityObjectUtil.StartDestroyQueue();
            m_nRecordingRoutine = Runnable.Run(RecordingHandler());
        }
    }

    private void StopRecording() {
        if (m_nRecordingRoutine != 0) {
            Microphone.End(m_sMicrophoneID);
            Runnable.Stop(m_nRecordingRoutine);
            m_nRecordingRoutine = 0;
        }
    }

    private void OnError(string error) {
        Debug.Log("StreamingMic Error! " + error);
    }

    public void ToggleMicrophone() {
    }

    private IEnumerator RecordingHandler() {
        Debug.Log("****StreamingMic devices: " + Microphone.devices);
        m_acRecording = Microphone.Start(m_sMicrophoneID, true, m_nRecordingBufferSize, m_nRecordingHZ);
        while (!(Microphone.GetPosition(null) > 0)) {
        }
        yield return null;

        if (m_acRecording == null) {
            StopRecording();
            yield break;
        }

        float[] samples = null;
        int lastSample = 0;

        while (m_nRecordingRoutine != 0 && m_acRecording != null) {
            int pos = Microphone.GetPosition(m_sMicrophoneID);
            if (pos > m_acRecording.samples || !Microphone.IsRecording(m_sMicrophoneID)) {
                Debug.Log("MicrophoneWidget Microphone disconnected.");
                StopRecording();
                yield break;
            }

            int diff = pos - lastSample;
            //Debug.Log("pos=" + pos + ", lastSample=" + lastSample + ", diff=" + diff);

            if (diff > 0) {
                int nsamplesarray = diff * m_acRecording.channels;
                samples = new float[nsamplesarray];
                m_acRecording.GetData(samples, lastSample);
                m_level = Mathf.Max(samples);
                //m_webSocketClient.OnListen(samples, 0, samples.Length, m_acRecording.channels);
            } else {
                samples = new float[m_acRecording.samples];
                m_acRecording.GetData(samples, 0);
                //m_webSocketClient.OnListen(samples, lastSample, samples.Length, m_acRecording.channels);
                //m_webSocketClient.OnListen(samples, 0, pos, m_acRecording.channels);
            }
            lastSample = pos;
            yield return new WaitForSeconds(0.1f);
        }

        yield break;
    }

}

