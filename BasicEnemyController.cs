## Unity Code for Enemy Functions and Controls. This includes layer detection, state switches, attack, and touch damage.

using UnityEngine;

#nullable disable
public class BasicEnemyController : MonoBehaviour
{
  private BasicEnemyController.State currentState;
  [SerializeField]
  private float knockbackSpeedX;
  [SerializeField]
  private float knockbackSpeedY;
  [SerializeField]
  private float knockbackDeathSpeedX;
  [SerializeField]
  private float knockbackDeathSpeedY;
  [SerializeField]
  private float deathTorque;
  [SerializeField]
  private Transform groundCheck;
  [SerializeField]
  private Transform wallCheck;
  [SerializeField]
  private Transform touchDamageCheck;
  [SerializeField]
  private float groundCheckDistance;
  [SerializeField]
  private float wallCheckDistance;
  [SerializeField]
  private float movementSpeed;
  [SerializeField]
  private float maxHealth;
  [SerializeField]
  private float knockbackDuration;
  [SerializeField]
  private float lastTouchDamageTime;
  [SerializeField]
  private float touchDamageCooldown;
  [SerializeField]
  private float touchDamage;
  [SerializeField]
  private float touchDamageWidth;
  [SerializeField]
  private float touchDamageHeight;
  [SerializeField]
  private LayerMask whatIsGround;
  [SerializeField]
  private LayerMask whatIsWall;
  [SerializeField]
  private LayerMask whatIsPlayer;
  [SerializeField]
  private Vector2 knockbackSpeed;
  [SerializeField]
  private GameObject hitParticle;
  [SerializeField]
  private GameObject deathParticle;
  [SerializeField]
  private GameObject extraParticles;
  private bool groundDetected;
  private bool wallDetected;
  private float currentHealth;
  private float knockbackStartTime;
  private int facingDirection;
  private int damageDirection;
  private float animTime;
  public float delay;
  private AttackDetails attackDetails;
  private Vector2 movement;
  private Vector2 touchDamageBotLeft;
  private Vector2 touchDamageTopRight;
  private GameObject alive;
  private Rigidbody2D aliveRb;
  private Animator aliveAnim;

  private void Start()
  {
    this.alive = this.transform.Find("Alive").gameObject;
    this.aliveRb = this.alive.GetComponent<Rigidbody2D>();
    this.aliveAnim = this.alive.GetComponent<Animator>();
    this.currentHealth = this.maxHealth;
    this.facingDirection = 1;
    this.animTime = this.alive.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
  }

  private void Update()
  {
    switch (this.currentState)
    {
      case BasicEnemyController.State.Moving:
        this.UpdateMovingState();
        break;
      case BasicEnemyController.State.Knockback:
        this.UpdateKnockbackState();
        break;
      case BasicEnemyController.State.Dead:
        this.UpdateDeadState();
        break;
    }
  }

  private void EnterMovingState()
  {
  }

  private void UpdateMovingState()
  {
    this.groundDetected = (bool) Physics2D.Raycast((Vector2) this.groundCheck.position, Vector2.down, this.groundCheckDistance, (int) this.whatIsGround);
    this.wallDetected = (bool) Physics2D.Raycast((Vector2) this.wallCheck.position, (Vector2) this.transform.right, this.wallCheckDistance, (int) this.whatIsWall);
    this.CheckTouchDamage();
    if (!this.groundDetected || this.wallDetected)
    {
      this.Flip();
    }
    else
    {
      this.movement.Set(this.movementSpeed * (float) this.facingDirection, this.aliveRb.velocity.y);
      this.aliveRb.velocity = this.movement;
    }
  }

  private void ExitMovingState()
  {
  }

  private void EnterKnockbackState()
  {
    this.knockbackStartTime = Time.time;
    this.movement.Set(this.knockbackSpeed.x * (float) this.damageDirection, this.knockbackSpeed.y);
    this.aliveRb.velocity = this.movement;
    this.aliveAnim.SetBool("Knockback", true);
  }

  private void UpdateKnockbackState()
  {
    if ((double) Time.time < (double) this.knockbackStartTime + (double) this.knockbackDuration)
      return;
    this.SwitchState(BasicEnemyController.State.Moving);
  }

  private void ExitKnockbackState() => this.aliveAnim.SetBool("Knockback", false);

  private void EnterDeadState()
  {
    this.aliveAnim.SetBool("Death", true);
    this.aliveAnim.Play("Enemy1_Death");
    Object.Destroy((Object) this.gameObject, this.animTime - 0.7f);
  }

  private void UpdateDeadState()
  {
  }

  private void ExitDeadState()
  {
  }

  private void Damage(AttackDetails attackDetails)
  {
    this.currentHealth -= attackDetails.damageAmount;
    Object.Instantiate<GameObject>(this.hitParticle, this.alive.transform.position, Quaternion.Euler(0.0f, 0.0f, Random.Range(0.0f, 360f)));
    this.damageDirection = (double) attackDetails.position.x <= (double) this.alive.transform.position.x ? 1 : -1;
    if ((double) this.currentHealth > 0.0)
    {
      this.SwitchState(BasicEnemyController.State.Knockback);
    }
    else
    {
      if ((double) this.currentHealth > 0.0)
        return;
      this.SwitchState(BasicEnemyController.State.Dead);
    }
  }

  private void CheckTouchDamage()
  {
    if ((double) Time.time < (double) this.lastTouchDamageTime + (double) this.touchDamageCooldown)
      return;
    this.touchDamageBotLeft.Set(this.touchDamageCheck.position.x - this.touchDamageWidth / 2f, this.touchDamageCheck.position.y - this.touchDamageHeight / 2f);
    this.touchDamageTopRight.Set(this.touchDamageCheck.position.x + this.touchDamageWidth / 2f, this.touchDamageCheck.position.y + this.touchDamageHeight / 2f);
    Collider2D collider2D = Physics2D.OverlapArea(this.touchDamageBotLeft, this.touchDamageTopRight, (int) this.whatIsPlayer);
    if (!((Object) collider2D != (Object) null))
      return;
    this.lastTouchDamageTime = Time.time;
    this.attackDetails.damageAmount = this.touchDamage;
    this.attackDetails.position.x = this.alive.transform.position.x;
    collider2D.SendMessage("Damage", (object) this.attackDetails);
  }

  private void Flip()
  {
    this.facingDirection *= -1;
    this.alive.transform.Rotate(0.0f, 180f, 0.0f);
  }

  private void SwitchState(BasicEnemyController.State state)
  {
    switch (this.currentState)
    {
      case BasicEnemyController.State.Moving:
        this.ExitMovingState();
        break;
      case BasicEnemyController.State.Knockback:
        this.ExitKnockbackState();
        break;
      case BasicEnemyController.State.Dead:
        this.ExitDeadState();
        break;
    }
    switch (state)
    {
      case BasicEnemyController.State.Moving:
        this.EnterMovingState();
        break;
      case BasicEnemyController.State.Knockback:
        this.EnterKnockbackState();
        break;
      case BasicEnemyController.State.Dead:
        this.EnterDeadState();
        break;
    }
    this.currentState = state;
  }

  private void OnDrawGizmos()
  {
    Gizmos.DrawLine(this.groundCheck.position, (Vector3) new Vector2(this.groundCheck.position.x, this.groundCheck.position.y - this.groundCheckDistance));
    Gizmos.DrawLine(this.wallCheck.position, (Vector3) new Vector2(this.wallCheck.position.x + this.wallCheckDistance, this.wallCheck.position.y));
    Vector2 vector2_1 = new Vector2(this.touchDamageCheck.position.x - this.touchDamageWidth / 2f, this.touchDamageCheck.position.y - this.touchDamageHeight / 2f);
    Vector2 vector2_2 = new Vector2(this.touchDamageCheck.position.x + this.touchDamageWidth / 2f, this.touchDamageCheck.position.y - this.touchDamageHeight / 2f);
    Vector2 vector2_3 = new Vector2(this.touchDamageCheck.position.x + this.touchDamageWidth / 2f, this.touchDamageCheck.position.y + this.touchDamageHeight / 2f);
    Vector2 vector2_4 = new Vector2(this.touchDamageCheck.position.x - this.touchDamageWidth / 2f, this.touchDamageCheck.position.y + this.touchDamageHeight / 2f);
    Gizmos.DrawLine((Vector3) vector2_1, (Vector3) vector2_2);
    Gizmos.DrawLine((Vector3) vector2_2, (Vector3) vector2_3);
    Gizmos.DrawLine((Vector3) vector2_3, (Vector3) vector2_4);
    Gizmos.DrawLine((Vector3) vector2_4, (Vector3) vector2_1);
  }

  private enum State
  {
    Moving,
    Knockback,
    Dead,
  }
}
