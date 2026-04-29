using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ARFps.Features.HandTracking
{
    /// <summary>
    /// Interfaces directly with Unity's XR Hands subsystem.
    /// Extracts physical joint positions without calculating gesture logic.
    /// </summary>
    public class ARHandTracker
    {
        private XRHandSubsystem _handSubsystem;
        private static readonly List<XRHandSubsystem> s_subsystemsReuse = new List<XRHandSubsystem>();

        /// <summary>
        /// Attempts to hook into the active XR Hand Subsystem.
        /// </summary>
        public void TryInitializeSubsystem()
        {
            if (_handSubsystem != null && _handSubsystem.running) return;

            SubsystemManager.GetSubsystems(s_subsystemsReuse);
            if (s_subsystemsReuse.Count > 0)
            {
                _handSubsystem = s_subsystemsReuse[0];
            }
        }

        /// <summary>
        /// Queries the XR tracking system for the current positions of the specific pinch joints.
        /// </summary>
        /// <param name="thumbTipPos">Outputs the Thumb Tip position if tracked.</param>
        /// <param name="indexProximalPos">Outputs the Index Proximal position if tracked.</param>
        /// <returns>True if the hand is actively tracked and joint poses are valid.</returns>
        public bool TryGetPinchJoints(out Vector3 thumbTipPos, out Vector3 indexProximalPos)
        {
            thumbTipPos = Vector3.zero;
            indexProximalPos = Vector3.zero;

            if (_handSubsystem == null || !_handSubsystem.running)
            {
                TryInitializeSubsystem();
                return false;
            }

            // We assume the Right Hand is the gun grip for now.
            XRHand rightHand = _handSubsystem.rightHand;
            if (!rightHand.isTracked) return false;

            XRHandJoint thumbTip = rightHand.GetJoint(XRHandJointID.ThumbTip);
            XRHandJoint indexProximal = rightHand.GetJoint(XRHandJointID.IndexProximal);

            if (thumbTip.TryGetPose(out Pose thumbPose) && indexProximal.TryGetPose(out Pose indexPose))
            {
                thumbTipPos = thumbPose.position;
                indexProximalPos = indexPose.position;
                return true;
            }
            return false;
        }
    }
}