using Photon.Pun;
using UnityEngine;

namespace PV.Multiplayer
{
    public class Weapon : MonoBehaviour
    {
        [Header("Weapon Properties")]
        [Tooltip("The amount of damage dealt by the weapon.")]
        public int damage = 10;
        [Tooltip("The rate of fire (times it can fire in 1 second).")]
        public float fireRate = 2;
        [Tooltip("The maximum distance the weapon's raycast can reach.")]
        public float hitDistance = 100;

        [Header("Weapon Components")]
        [Tooltip("The point from which the weapon will shoot (origin of the raycast).")]
        public Transform shootPoint;
        [Tooltip("Particle effect to display when firing.")]
        public GameObject shootParticle;
        [Tooltip("Particle effect to display when a target is hit.")]
        public GameObject hitParticle;
        [Tooltip("The layers that the weapon's raycast can hit.")]
        public LayerMask hitLayer;

        // Stores information about the object hit by the raycast.
        private RaycastHit _hit;
        // Reference to the PlayerController component of the hit target.
        private PlayerController _player;

        private Transform _cameraT;
        private Vector3 _shootDirection;
        private Vector3 _shootPosition;
        private PhotonView _photonView;

        private GameObject _shootParticleObject;
        private GameObject _hitParticleObject;

        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();
            _cameraT = Camera.main.transform;

            if (hitParticle != null)
            {
                // Detach hitParticle from its parent and deactivate it initially.
                hitParticle.transform.SetParent(null);
                hitParticle.SetActive(false);
            }
        }

        /// <summary>
        /// Fires the weapon, performing a raycast to detect and damage targets within range.
        /// </summary>
        public void Fire(int attackerID)
        {
            if (shootParticle != null)
            {
                // Show the shooting particle effect on all clients.
                _photonView.RPC(nameof(ShowShootParticle), RpcTarget.All, _photonView.ViewID);
            }

            // Determine the shooting direction and position based on aiming input.
            _shootDirection = InputManager.Instance.isAiming ? _cameraT.forward : shootPoint.forward;
            _shootPosition = InputManager.Instance.isAiming ? _cameraT.position : shootPoint.position;

            // Perform a raycast from the shoot point in the forward direction.
            if (Physics.Raycast(_shootPosition, _shootDirection, out _hit, hitDistance, hitLayer, QueryTriggerInteraction.Ignore))
            {
                // Check if the hit object has a PlayerController component.
                if (_hit.transform.TryGetComponent(out _player))
                {
                    // Apply damage to the hit player across all clients.
                    _player.photonView.RPC(nameof(_player.TakeDamage), RpcTarget.All, damage, attackerID);
                }

                if (hitParticle != null)
                {
                    // Show the hit particle effect on all clients at the hit location.
                    _photonView.RPC(nameof(ShowHitParticle), RpcTarget.All, _photonView.ViewID, _hit.point);
                }
            }
        }

        /// <summary>
        /// Displays the shooting particle effect on the weapon.
        /// </summary>
        /// <param name="viewID">The PhotonView ID of the weapon triggering the effect.</param>
        [PunRPC]
        private void ShowShootParticle(int viewID)
        {
            // Find the shootParticle object associated with the given PhotonView ID.
            _shootParticleObject = PhotonView.Find(viewID).GetComponent<Weapon>().shootParticle;

            if (_shootParticleObject != null)
            {
                // If particle is enabled then disable it to show particle when enabled again.
                if (_shootParticleObject.activeSelf)
                {
                    _shootParticleObject.SetActive(false);
                }
                _shootParticleObject.SetActive(true);
            }
        }

        /// <summary>
        /// Displays the hit particle effect at the given hit location.
        /// </summary>
        /// <param name="viewID">The PhotonView ID of the weapon triggering the effect.</param>
        /// <param name="hitPoint">The world position where the hit occurred.</param>
        [PunRPC]
        private void ShowHitParticle(int viewID, Vector3 hitPoint)
        {
            // Find the hitParticle object associated with the given PhotonView ID.
            _hitParticleObject = PhotonView.Find(viewID).GetComponent<Weapon>().hitParticle;

            if (_hitParticleObject != null)
            {
                // If particle is enabled then disable it to show particle when enabled again.
                if (_hitParticleObject.activeSelf)
                {
                    _hitParticleObject.SetActive(false);
                }
                _hitParticleObject.transform.position = hitPoint;
                _hitParticleObject.SetActive(true);
            }
        }
    }
}
