using System.Collections;
using UnityEngine;

namespace EmberKnight.Player
{
    /// <summary>
    /// Controlador principal do Ember Knight. Porta a lógica de movimento,
    /// combo de ataque e golpe especial do protótipo original em HTML5 Canvas.
    /// Requer: Rigidbody2D, BoxCollider2D, Animator, SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        public float moveSpeed = 6f;
        public float jumpForce = 14f;
        public float groundFriction = 12f;

        [Header("Chão")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.15f;
        public LayerMask groundLayer;

        [Header("Vida")]
        public int maxHp = 100;
        public int currentHp;
        public float invulnDuration = 0.9f;

        [Header("Especial")]
        public float maxSpecial = 100f;
        public float currentSpecial;
        public float specialRegenPerSecond = 4f;
        public int specialDamage = 40;
        public float specialRange = 1.6f;

        [Header("Combate")]
        public int[] comboDamage = { 10, 12, 18 };
        public float comboWindow = 0.45f;   // tempo p/ encadear o próximo golpe
        public float attackRange = 1.1f;
        public LayerMask enemyLayer;

        [Header("Eventos (arraste UI aqui)")]
        public UnityEngine.Events.UnityEvent<float> onHealthChanged;   // 0..1
        public UnityEngine.Events.UnityEvent<float> onSpecialChanged;  // 0..1
        public UnityEngine.Events.UnityEvent onDeath;

        Rigidbody2D rb;
        Animator anim;
        SpriteRenderer sr;

        float moveInput;
        bool facingRight = true;
        bool isGrounded;
        bool isDead;
        bool isAttacking;
        bool isSpecialCasting;

        int comboStep;
        float comboTimer;
        float invulnTimer;

        static readonly int HashSpeed = Animator.StringToHash("Speed");
        static readonly int HashGrounded = Animator.StringToHash("Grounded");
        static readonly int HashVSpeed = Animator.StringToHash("VSpeed");
        static readonly int HashAttack1 = Animator.StringToHash("Attack1");
        static readonly int HashAttack2 = Animator.StringToHash("Attack2");
        static readonly int HashAttack3 = Animator.StringToHash("Attack3");
        static readonly int HashSpecial = Animator.StringToHash("Special");
        static readonly int HashDamage = Animator.StringToHash("Damage");
        static readonly int HashDeath = Animator.StringToHash("Death");

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            currentHp = maxHp;
            currentSpecial = 0f;
        }

        void Update()
        {
            if (isDead) return;

            HandleInput();
            HandleCombo();
            RegenSpecial();
            TickInvuln();
            UpdateAnimatorParams();
        }

        void FixedUpdate()
        {
            if (isDead) return;

            isGrounded = groundCheck != null &&
                Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (!isAttacking && !isSpecialCasting)
            {
                float targetVx = moveInput * moveSpeed;
                float vx = Mathf.Lerp(rb.linearVelocity.x, targetVx, groundFriction * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
            }
            else
            {
                // durante ataque/especial o personagem trava horizontalmente, como no original
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        void HandleInput()
        {
            moveInput = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) moveInput -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) moveInput += 1f;

            if (moveInput != 0f && !isAttacking && !isSpecialCasting)
            {
                bool wantsRight = moveInput > 0f;
                if (wantsRight != facingRight) Flip();
            }

            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                && isGrounded && !isAttacking && !isSpecialCasting)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            if (Input.GetKeyDown(KeyCode.J) && !isSpecialCasting)
            {
                TryAttack();
            }

            if (Input.GetKeyDown(KeyCode.K) && !isAttacking && !isSpecialCasting
                && currentSpecial >= maxSpecial)
            {
                StartCoroutine(DoSpecial());
            }
        }

        void HandleCombo()
        {
            if (comboTimer > 0f)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f) comboStep = 0; // combo expirou, volta ao golpe 1
            }
        }

        void TryAttack()
        {
            if (isAttacking) return;
            isAttacking = true;
            comboTimer = comboWindow;

            int step = comboStep % comboDamage.Length;
            switch (step)
            {
                case 0: anim.SetTrigger(HashAttack1); break;
                case 1: anim.SetTrigger(HashAttack2); break;
                default: anim.SetTrigger(HashAttack3); break;
            }

            comboStep++;
        }

        /// <summary>
        /// Chamado por um Animation Event no frame de impacto de cada clipe de ataque.
        /// </summary>
        public void Anim_ApplyAttackDamage()
        {
            int step = (comboStep - 1 + comboDamage.Length) % comboDamage.Length;
            int dmg = comboDamage[step];
            DealDamageInFront(attackRange, dmg);
        }

        /// <summary>Chamado por Animation Event no fim do clipe de ataque.</summary>
        public void Anim_AttackEnd()
        {
            isAttacking = false;
        }

        IEnumerator DoSpecial()
        {
            isSpecialCasting = true;
            anim.SetTrigger(HashSpecial);
            currentSpecial = 0f;
            onSpecialChanged?.Invoke(0f);

            // aplica o dano no meio da animação (ajuste conforme a duração do clipe)
            yield return new WaitForSeconds(0.35f);
            DealDamageInFront(specialRange, specialDamage);

            yield return new WaitForSeconds(0.35f);
            isSpecialCasting = false;
        }

        void DealDamageInFront(float range, int dmg)
        {
            Vector2 origin = transform.position;
            Vector2 dir = facingRight ? Vector2.right : Vector2.left;
            var hits = Physics2D.OverlapCircleAll(origin + dir * (range * 0.5f), range * 0.5f, enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<Enemies.EnemyController>();
                if (enemy != null) enemy.TakeDamage(dmg, dir);
            }
        }

        void RegenSpecial()
        {
            if (currentSpecial < maxSpecial)
            {
                currentSpecial = Mathf.Min(maxSpecial, currentSpecial + specialRegenPerSecond * Time.deltaTime);
                onSpecialChanged?.Invoke(currentSpecial / maxSpecial);
            }
        }

        void TickInvuln()
        {
            if (invulnTimer > 0f)
            {
                invulnTimer -= Time.deltaTime;
                // pisca o sprite enquanto invulnerável, como o flashHit do original
                sr.enabled = Mathf.FloorToInt(invulnTimer * 12f) % 2 == 0;
            }
            else
            {
                sr.enabled = true;
            }
        }

        public void TakeDamage(int amount)
        {
            if (isDead || invulnTimer > 0f) return;

            currentHp = Mathf.Max(0, currentHp - amount);
            invulnTimer = invulnDuration;
            onHealthChanged?.Invoke((float)currentHp / maxHp);
            anim.SetTrigger(HashDamage);

            if (currentHp <= 0) Die();
        }

        public void Heal(int amount)
        {
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            onHealthChanged?.Invoke((float)currentHp / maxHp);
        }

        void Die()
        {
            isDead = true;
            anim.SetTrigger(HashDeath);
            rb.linearVelocity = Vector2.zero;
            onDeath?.Invoke();
        }

        void Flip()
        {
            facingRight = !facingRight;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
            transform.localScale = s;
        }

        void UpdateAnimatorParams()
        {
            anim.SetFloat(HashSpeed, Mathf.Abs(rb.linearVelocity.x));
            anim.SetBool(HashGrounded, isGrounded);
            anim.SetFloat(HashVSpeed, rb.linearVelocity.y);
        }

        void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
    }
}
