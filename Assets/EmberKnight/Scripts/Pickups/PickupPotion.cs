using UnityEngine;
using EmberKnight.Player;

namespace EmberKnight.Pickups
{
    [RequireComponent(typeof(Collider2D))]
    public class PickupPotion : MonoBehaviour
    {
        public int healAmount = 30;
        public GameObject collectVfx;

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc == null) return;

            pc.Heal(healAmount);

            if (collectVfx != null)
                Instantiate(collectVfx, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
