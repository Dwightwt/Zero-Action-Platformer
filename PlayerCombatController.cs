## Unity Code for player attacks, which checks for plaer input, detects for enemies in range, allows combo attacks, and has sound effects for every attack. If player is hit, they are granted invincibility frames. 

  // Decompiled with JetBrains decompiler
// Type: PlayerCombatController
// Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6B1F22E3-D286-40E2-A0DA-F3F5CE7B7B73
// Assembly location: C:\Users\Terron\Downloads\Zero Game\Zero Game\Zero_Data\Managed\Assembly-CSharp.dll

using System.Collections;
using UnityEngine;

#nullable disable
public class PlayerCombatController : MonoBehaviour
{
  [SerializeField]
  private bool combatEnabled;
  [SerializeField]
  private float inputTimer;
  [SerializeField]
  private float attack1Radius;
  [SerializeField]
  private float attack1Damage;
  [SerializeField]
  private Transform attack1HitBoxPos;
  [SerializeField]
  private LayerMask whatIsDamageable;
  public bool gotInput;
  public bool isAttacking;
  public bool isFirstAttack;
  public bool isAirAttacking;
  private float lastInputTime = float.NegativeInfinity;
  private AttackDetails attackDetails;
  private Animator _anim;
  public bool canJump;
  public bool canMove;
  public bool isInvincible;
  [SerializeField]
  private float invincibilityDurationSeconds;
  [SerializeField]
  private float stunDamageAmount = 1f;
  public int combo;
  public AudioSource audio_S;
  public AudioClip[] sound;
  private Movement2D PC;
  private PlayerStats PS;
  [SerializeField]
  private Healthbar _healthbar;

  private void Start()
  {
    this._anim = this.GetComponent<Animator>();
    this.audio_S = this.GetComponent<AudioSource>();
    this._anim.SetBool("canAttack", this.combatEnabled);
    this.PC = this.GetComponent<Movement2D>();
    this.PS = this.GetComponent<PlayerStats>();
  }

  private void Update() => this.CheckAttacks();

  private void CheckCombatInput()
  {
    if (!Input.GetButtonDown("Attack") || !this.combatEnabled)
      return;
    this.gotInput = true;
    this.lastInputTime = Time.time;
  }

  private void CheckAttacks()
  {
    if (!Input.GetButtonDown("Attack") || this.isAttacking)
      return;
    this.isAttacking = true;
    this._anim.SetTrigger(this.combo.ToString() ?? "");
    this.audio_S.clip = this.sound[this.combo];
    this.audio_S.Play();
  }

  public void StartCombo()
  {
    this.isAttacking = false;
    if (this.combo >= 3)
      return;
    ++this.combo;
  }

  private void CheckAttackHitBox()
  {
    Collider2D[] collider2DArray = Physics2D.OverlapCircleAll((Vector2) this.attack1HitBoxPos.position, this.attack1Radius, (int) this.whatIsDamageable);
    this.attackDetails.damageAmount = this.attack1Damage;
    this.attackDetails.position = (Vector2) this.transform.position;
    this.attackDetails.stunDamageAmount = this.stunDamageAmount;
    foreach (Component component in collider2DArray)
      component.transform.parent.SendMessage("Damage", (object) this.attackDetails);
  }

  private void FinishAttack1()
  {
    this.isAttacking = false;
    this.combo = 0;
    this._anim.SetBool("isAttacking", this.isAttacking);
    this._anim.SetBool("attack1", false);
  }

  private void Damage(AttackDetails attackDetails)
  {
    if (this.PC.GetDashStatus() || this.isInvincible)
      return;
    this.PS.DecreaseHealth(attackDetails.damageAmount);
    this._healthbar.SetHealth(this.PS.currentHealth);
    this.StartCoroutine(this.BecomeTemporarilyInvincible());
    int direction = (double) attackDetails.position.x >= (double) this.transform.position.x ? -1 : 1;
    this.isAttacking = false;
    this.combo = 0;
    this.PC.Knockback(direction);
  }

  private void OnDrawGizmos()
  {
    Gizmos.DrawWireSphere(this.attack1HitBoxPos.position, this.attack1Radius);
  }

  public void EnableJump() => this.canJump = true;

  public void DisableJump() => this.canJump = false;

  public void EnableMove() => this.canMove = true;

  public void DisableMove() => this.canMove = false;

  private IEnumerator BecomeTemporarilyInvincible()
  {
    Debug.Log((object) "Player turned invincible!");
    this.isInvincible = true;
    yield return (object) new WaitForSeconds(this.invincibilityDurationSeconds);
    this.isInvincible = false;
    Debug.Log((object) "Player is no longer invincible!");
  }
}
