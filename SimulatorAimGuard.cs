#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace AirbnbTurnover
{
    /// <summary>
    /// Editor-only fix for the XR Interaction Simulator. In point-and-click mode it aims each controller at whatever the
    /// mouse ray hits. When the player stands closer to an object than the controller (about 30 cm in front of the head),
    /// that point is behind the controller and it spins around to face the player. This keeps the last sensible aim instead.
    /// Runs right after the simulator and re-applies the corrected controller state before anything reads it.
    /// </summary>
    [DefaultExecutionOrder(-29990)]
    public class SimulatorAimGuard : MonoBehaviour
    {
        // Controllers may point at most this far away from where the head is looking (dot of forward vectors).
        const float MinForwardDot = 0.2f;

        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo HmdField = typeof(XRInteractionSimulator).GetField("m_HMDState", Private);
        static readonly FieldInfo LeftField = typeof(XRInteractionSimulator).GetField("m_LeftControllerState", Private);
        static readonly FieldInfo RightField = typeof(XRInteractionSimulator).GetField("m_RightControllerState", Private);
        static readonly MethodInfo ApplyMethod = typeof(SimulatedDeviceLifecycleManager).GetMethod("ApplyControllerState", Private);

        XRInteractionSimulator simulator;
        Quaternion lastLeft = Quaternion.identity;
        Quaternion lastRight = Quaternion.identity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (HmdField == null || LeftField == null || RightField == null || ApplyMethod == null)
            {
                Debug.LogWarning("SimulatorAimGuard: XR Interaction Simulator internals changed; controller aim fix is off.");
                return;
            }
            var go = new GameObject("Simulator Aim Guard") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            go.AddComponent<SimulatorAimGuard>();
        }

        void Update()
        {
            if (simulator == null)
            {
                simulator = FindAnyObjectByType<XRInteractionSimulator>();
                if (simulator == null)
                    return;
            }
            if (!simulator.isActiveAndEnabled || !simulator.pointAndClickActive || simulator.deviceLifecycleManager == null)
                return;

            var hmd = (XRSimulatedHMDState)HmdField.GetValue(simulator);
            Vector3 headForward = hmd.deviceRotation * Vector3.forward;
            var left = (XRSimulatedControllerState)LeftField.GetValue(simulator);
            var right = (XRSimulatedControllerState)RightField.GetValue(simulator);

            bool changed = Guard(ref left.deviceRotation, ref lastLeft, headForward, hmd.deviceRotation);
            changed |= Guard(ref right.deviceRotation, ref lastRight, headForward, hmd.deviceRotation);
            if (!changed)
                return;

            LeftField.SetValue(simulator, left);
            RightField.SetValue(simulator, right);
            ApplyMethod.Invoke(simulator.deviceLifecycleManager, new object[] { left, right });
        }

        static bool Guard(ref Quaternion rotation, ref Quaternion lastGood, Vector3 headForward, Quaternion headRotation)
        {
            if (Vector3.Dot(rotation * Vector3.forward, headForward) >= MinForwardDot)
            {
                lastGood = rotation;
                return false;
            }
            bool lastIsValid = Vector3.Dot(lastGood * Vector3.forward, headForward) >= MinForwardDot;
            rotation = lastIsValid ? lastGood : headRotation;
            return true;
        }
    }
}
#endif
