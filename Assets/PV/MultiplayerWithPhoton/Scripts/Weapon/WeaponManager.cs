using Photon.Pun;
using UnityEngine;

namespace PV.Multiplayer
{
    public class WeaponManager : MonoBehaviour
    {
        public Weapon primaryWeapon;

        // Tracks the time remaining before the player can fire the weapon again.
        private float _attackDelay = 0f;

        private InputManager Input => InputManager.Instance;

        private int _playerViewID;

        private void Start()
        {
            _playerViewID = GetComponent<PhotonView>().ViewID;
        }

        /// <summary>
        /// Updates weapon-related behavior.
        /// </summary>
        public void DoUpdate()
        {
            if (Input == null)
            {
                return;
            }

            // Check if the player is pressing the attack button.
            if (Input.attack)
            {
                // If the attack delay has elapsed, fire the weapon.
                if (_attackDelay <= 0f)
                {
                    // Reset the attack delay to the weapon's fire rate in 1 second.
                    _attackDelay = 1 / primaryWeapon.fireRate;

                    // Trigger the weapon fire.
                    primaryWeapon.Fire(_playerViewID);
                }

                // Reduce the attack delay timer.
                _attackDelay -= Time.deltaTime;
            }
            else if (_attackDelay > 0f)
            {
                // If the attack button is not being pressed, reset the attack delay.
                _attackDelay = 0f;
            }
        }
    }
}
