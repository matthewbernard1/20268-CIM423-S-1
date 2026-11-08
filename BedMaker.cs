using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AirbnbTurnover
{
    /// <summary>
    /// Bed task: pillows must be placed on the bed (via a DropZone) before the Make Bed button works.
    /// Pressing it smooths the blanket into its neat pose.
    /// </summary>
    public class BedMaker : MonoBehaviour
    {
        public DropZone pillowZone;
        public int pillowsRequired = 2;
        public Transform blanket;
        public Vector3 neatLocalPosition;
        public Vector3 neatLocalEuler;
        public Button makeBedButton;
        public float smoothDuration = 0.8f;

        bool made;

        void Start()
        {
            makeBedButton.onClick.AddListener(MakeBed);
            pillowZone.ItemAccepted += (_, _) =>
            {
                if (pillowZone.AcceptedCount >= pillowsRequired && TaskManager.Instance != null)
                    TaskManager.Instance.ShowMessage("Pillows are back. Now press <b>Make Bed</b>.");
            };
        }

        public void MakeBed()
        {
            if (made)
                return;
            if (pillowZone.AcceptedCount < pillowsRequired)
            {
                if (TaskManager.Instance != null)
                    TaskManager.Instance.ShowMessage("Put the pillows back on the bed first. They're on the floor, obviously.");
                return;
            }
            made = true;
            makeBedButton.interactable = false;
            StartCoroutine(SmoothBlanket());
        }

        IEnumerator SmoothBlanket()
        {
            Vector3 fromPos = blanket.localPosition;
            Quaternion fromRot = blanket.localRotation;
            Quaternion toRot = Quaternion.Euler(neatLocalEuler);
            for (float t = 0f; t < 1f; t += Time.deltaTime / smoothDuration)
            {
                float e = Mathf.SmoothStep(0f, 1f, t);
                blanket.localPosition = Vector3.Lerp(fromPos, neatLocalPosition, e);
                blanket.localRotation = Quaternion.Slerp(fromRot, toRot, e);
                yield return null;
            }
            blanket.localPosition = neatLocalPosition;
            blanket.localRotation = toRot;
            if (TaskManager.Instance != null)
                TaskManager.Instance.AddProgress(TaskId.Bed);
        }
    }
}
