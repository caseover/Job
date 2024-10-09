using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CamCon{
    public class CameraControl : MonoBehaviour
    {
        public Camera camera;
        void Start()
        {
            if (camera == null)
                camera = Camera.main;  // 메인 카메라 자동 할당
        }

        public void ZoomIn(float zoomFactor, float duration)
        {
            StartCoroutine(ZoomCamera(zoomFactor, duration));
        }

        public void RotateView(float angle, float duration)
        {
            StartCoroutine(RotateCamera(angle, duration));
        }

        IEnumerator ZoomCamera(float zoomFactor, float duration)
        {
            float startTime = Time.time;
            float startSize = camera.orthographicSize;
            float endSize = startSize / zoomFactor;

            while (Time.time < startTime + duration)
            {
                camera.orthographicSize = Mathf.Lerp(startSize, endSize, (Time.time - startTime) / duration);
                yield return null;
            }

            camera.orthographicSize = endSize;
        }

        IEnumerator RotateCamera(float angle, float duration)
        {
            float startTime = Time.time;
            Quaternion startRotation = camera.transform.rotation;
            Quaternion endRotation = startRotation * Quaternion.Euler(0, 0, angle);

            while (Time.time < startTime + duration)
            {
                camera.transform.rotation = Quaternion.Lerp(startRotation, endRotation, (Time.time - startTime) / duration);
                yield return null;
            }

            camera.transform.rotation = endRotation;
        }
    }
}