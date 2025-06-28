using UnityEngine;
using Photon.Pun;

namespace PV.Multiplayer
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkMovement : MonoBehaviourPun, IPunObservable
    {
        // Smoothing settings
        public float positionLerpRate = 15f;
        public float rotationLerpRate = 15f;
        public float compensateFactor = 0.5f;
        public float maxOffset = 0.2f;

        private Vector3 _desiredPosition;
        private Quaternion _desiredRotation;
        private Vector3 _networkPosition;
        private Quaternion _networkRotation;
        private Vector3 _networkVelocity;

        // Reference to local Rigidbody for physics data
        private Rigidbody _rb;

        private float _lag;
        private Vector3 _predictedOffset;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _networkPosition = transform.position;
            _networkRotation = transform.rotation;
        }

        void FixedUpdate()
        {
            // If this is not our local player, interpolate towards the network values
            if (!photonView.IsMine)
            {
                _desiredPosition = Vector3.Lerp(transform.position, _networkPosition, Time.fixedDeltaTime * positionLerpRate);
                _desiredRotation = Quaternion.Lerp(transform.rotation, _networkRotation, Time.fixedDeltaTime * rotationLerpRate);
                transform.SetPositionAndRotation(_desiredPosition, _desiredRotation);
                
                // Optionally, you can also update the Rigidbody's velocity if needed:
                _rb.velocity = Vector3.Lerp(_rb.velocity, _networkVelocity, Time.fixedDeltaTime * positionLerpRate);
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Local player: send current transform and velocity
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(_rb.velocity);
            }
            else
            {
                // Remote player: receive and store transform and velocity values
                _networkPosition = (Vector3)stream.ReceiveNext();
                _networkRotation = (Quaternion)stream.ReceiveNext();
                _networkVelocity = (Vector3)stream.ReceiveNext();

                // Client-side prediction: compensate for network lag
                _lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                _predictedOffset = _lag * compensateFactor * _networkVelocity;

                // If predicted offset exceeds the maximum allowed offset / distance.
                if (_predictedOffset.magnitude > maxOffset)
                {
                    // Clamp the predicted offset to a fixed maximum length while preserving its direction.
                    _predictedOffset = _predictedOffset.normalized * maxOffset;
                }

                _networkPosition += _predictedOffset;
            }
        }
    }
}
