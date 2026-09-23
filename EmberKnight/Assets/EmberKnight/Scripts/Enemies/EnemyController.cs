using System.Collections;
using UnityEngine;
using EmberKnight.Player;

namespace EmberKnight.Enemies
{
    public enum EnemyType { Wisp, Brute }

    /// <summary>
    /// IA simples de perseguição + telegraph + ataque, cobrindo os dois
    /// tipos de inimigo do protótipo original (wisp = rápido/fraco,
    /// brute = lento/forte).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Tipo")]
        public EnemyType type = EnemyType.Wisp;

        [Header("Stats (padrão do original: wisp hp30/spd1.6, brute hp60/spd1.1)")]
        public int maxHp = 30;
        public float moveSpeed = 3.2f;
        public int contactDamage = 10;

        [Header("IA")]
        public float detectRange = 5f;
        public float attackRange = 1.0f;
        public float telegraphTime = 0.5f;
        public float attackCooldown = 1.2f;

        [Header("Refs")]
        public Animator anim; // opcional, se o inimigo tiver animações próprias

        int currentHp;
        Rigidbody2D rb;
        SpriteRenderer sr;
        Transform player;
        Color baseColor;

        bool isDead;
        bool isTelegraphing;
        bool isAttackOnCooldown;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
            baseColor = sr.color;
            currentHp = maxHp;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        void Update()
        {
            if (isDead || player == null) return;

            float dist = Vector2.Distance(transform.position, player.position);
            bool facingRight = transform.position.x < player.position.x;
            SetFacing(facingRight);

            if (dist <= attackRange && !isTelegraphing && !isAttackOnCooldown)
            {
                StartCoroutine(TelegraphAndAttack());
            }
            else if (dist <= detectRange && !isTelegraphing)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
            }
            else if (!isTelegraphing)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        void SetFacing(bool right)
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (right ? 1f : -1f);
            transform.localScale = s;
        }

        IEnumerator TelegraphAndAttack()
        {
            isTelegraphing = true;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // telegraph visual: pisca uma cor de aviso antes do golpe
            sr.color = Color.yellow;
            yield return new WaitForSeconds(telegraphTime);
            sr.color = baseColor;

            if (!isDead && player != null &&
                Vector2.Distance(transform.position, player.position) <= attackRange * 1.4f)
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage(contactDamage);
            }

            isTelegraphing = false;
            isAttackOnCooldown = true;
            yield return new WaitForSeconds(attackCooldown);
            isAttackOnCooldown = false;
        }

        public void TakeDamage(int amount, Vector2 knockDir)
        {
            if (isDead) return;

            currentHp -= amount;
            StartCoroutine(HitFlash());
            rb.linearVelocity = new Vector2(knockDir.x * 3f, rb.linearVelocity.y);

            if (currentHp <= 0) Die();
        }

        IEnumerator HitFlash()
        {
            sr.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (!isDead) sr.color = baseColor;
        }

        void Die()
        {
            isDead = true;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;

            if (anim != null) anim.SetTrigger("Death");

            Rooms.RoomManager.Instance?.NotifyEnemyDefeated(this);

            // deixa a animação de morte tocar antes de remover
            Destroy(gameObject, 1.0f);
        }

        /// <summary>HP normalizado (0..1), útil pra desenhar a barrinha acima do inimigo.</summary>
        public float HpPercent => Mathf.Clamp01((float)currentHp / maxHp);
        public bool IsDead => isDead;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = new Color(1f, 1f, 0.3f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectRange);
        }
    }
}
