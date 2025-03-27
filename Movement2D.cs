## Unity Code that handles the 2D movement and physics. This includes layer detection and state detection

using System.Collections;
using UnityEngine;

#nullable disable
public class Movement2D : MonoBehaviour
{
  [Header("Components")]
  private Rigidbody2D _rb;
  private Animator _anim;
  [Header("Layer Masks")]
  [SerializeField]
  private LayerMask _groundLayer;
  [SerializeField]
  private LayerMask _wallLayer;
  [Header("Movement Variables")]
  [SerializeField]
  private float _movementAcceleration = 70f;
  [SerializeField]
  private float _maxMoveSpeed = 12f;
  [SerializeField]
  private float _groundLinearDrag = 7f;
  private float _horizontalDirection;
  private float _verticalDirection;
  private bool _facingRight = true;
  private bool _isWalking;
  private bool _canFlip;
  private bool knockback;
  private float knockbackStartTime;
  [SerializeField]
  private float knockbackDuration;
  [SerializeField]
  private Vector2 knockbackSpeed;
  [Header("Jump Variables")]
  [SerializeField]
  private float _jumpForce = 12f;
  [SerializeField]
  private float _airLinearDrag = 2.5f;
  [SerializeField]
  private float _fallMultiplier = 8f;
  [SerializeField]
  private float _lowJumpFallMultiplier = 5f;
  [SerializeField]
  private float _downMultiplier = 12f;
  [SerializeField]
  private int _extraJumps = 1;
  [SerializeField]
  private float _hangTime = 0.1f;
  [SerializeField]
  private float _jumpBufferLength = 0.1f;
  private int _extraJumpsValue;
  private float _hangTimeCounter;
  private float _jumpBufferCounter;
  private bool _isJumping;
  [Header("Wall Movement Variables")]
  [SerializeField]
  private float _wallSlideModifier = 0.5f;
  [SerializeField]
  private float _wallRunModifier = 0.85f;
  [SerializeField]
  private float _wallJumpXVelocityHaltDelay = 0.2f;
  [Header("Dash Variables")]
  [SerializeField]
  private float _dashSpeed = 15f;
  [SerializeField]
  private float _dashLength = 0.3f;
  [SerializeField]
  private float _dashBufferLength = 0.1f;
  private float _dashBufferCounter;
  private bool _isDashing;
  private bool _hasDashed;
  [Header("Ground Collision Variables")]
  [SerializeField]
  private float _groundRaycastLength;
  [SerializeField]
  private Vector3 _groundRaycastOffset;
  private bool _onGround;
  [Header("Wall Collision Variables")]
  [SerializeField]
  private float _wallRaycastLength;
  private bool _onWall;
  private bool _onRightWall;
  [Header("Corner Correction Variables")]
  [SerializeField]
  private float _topRaycastLength;
  [SerializeField]
  private Vector3 _edgeRaycastOffset;
  [SerializeField]
  private Vector3 _innerRaycastOffset;
  private bool _canCornerCorrect;
  private PlayerCombatController _pcc;

  private bool _changingDirection
  {
    get
    {
      if ((double) this._rb.velocity.x > 0.0 && (double) this._horizontalDirection < 0.0)
        return true;
      return (double) this._rb.velocity.x < 0.0 && (double) this._horizontalDirection > 0.0;
    }
  }

  private bool _canMove => !this._wallGrab && this._pcc.canMove;

  public bool _canJump
  {
    get
    {
      return (double) this._jumpBufferCounter > 0.0 && ((double) this._hangTimeCounter > 0.0 || this._extraJumpsValue > 0 || this._onWall) && this._pcc.canJump;
    }
  }

  private bool _wallGrab
  {
    get => this._onWall && !this._onGround && Input.GetButton("WallGrab") && !this._wallRun;
  }

  private bool _wallSlide
  {
    get
    {
      return this._onWall && !this._onGround && !Input.GetButton("WallGrab") && (double) this._rb.velocity.y < 0.0 && !this._wallRun;
    }
  }

  private bool _wallRun => this._onWall && (double) this._verticalDirection > 0.0;

  private bool _canDash => (double) this._dashBufferCounter > 0.0 && !this._hasDashed;

  private void Start()
  {
    this._rb = this.GetComponent<Rigidbody2D>();
    this._anim = this.GetComponent<Animator>();
    this._pcc = this.GetComponent<PlayerCombatController>();
  }

  private void Update()
  {
    this._horizontalDirection = this.GetInput().x;
    this._verticalDirection = this.GetInput().y;
    this.CheckMovementDirection();
    this.CheckKnockback();
    if (Input.GetButtonDown("Jump"))
      this._jumpBufferCounter = this._jumpBufferLength;
    else
      this._jumpBufferCounter -= Time.deltaTime;
    if (Input.GetButtonDown("Dash"))
      this._dashBufferCounter = this._dashBufferLength;
    else
      this._dashBufferCounter -= Time.deltaTime;
    this.Animation();
  }

  private void FixedUpdate()
  {
    this.CheckCollisions();
    if (this._canDash && !this.knockback)
      this.StartCoroutine(this.Dash(this._horizontalDirection, this._verticalDirection));
    if (!this._isDashing)
    {
      if (this._canMove && !this.knockback)
        this.MoveCharacter();
      else
        this._rb.velocity = Vector2.Lerp(this._rb.velocity, new Vector2(this._horizontalDirection * this._maxMoveSpeed, this._rb.velocity.y), 0.5f * Time.deltaTime);
      if (this._onGround)
      {
        this.ApplyGroundLinearDrag();
        this._extraJumpsValue = this._extraJumps;
        this._hangTimeCounter = this._hangTime;
        this._hasDashed = false;
      }
      else
      {
        this.ApplyAirLinearDrag();
        this.FallMultiplier();
        this._hangTimeCounter -= Time.fixedDeltaTime;
        if (!this._onWall || (double) this._rb.velocity.y < 0.0 || this._wallRun)
          this._isJumping = false;
      }
      if (this._canJump && !this.knockback)
      {
        if (this._onWall && !this._onGround)
        {
          if (!this._wallRun && (this._onRightWall && (double) this._horizontalDirection > 0.0 || !this._onRightWall && (double) this._horizontalDirection < 0.0))
            this.StartCoroutine(this.NeutralWallJump());
          else
            this.WallJump();
          this.Flip();
        }
        else
          this.Jump(Vector2.up);
      }
      if (!this._isJumping)
      {
        if (this._wallSlide)
          this.WallSlide();
        if (this._wallGrab)
          this.WallGrab();
        if (this._wallRun)
          this.WallRun();
        if (this._onWall)
          this.StickToWall();
      }
    }
    if (!this._canCornerCorrect)
      return;
    this.CornerCorrect(this._rb.velocity.y);
  }

  private Vector2 GetInput()
  {
    return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
  }

  private void MoveCharacter()
  {
    this._rb.AddForce(new Vector2(this._horizontalDirection, 0.0f) * this._movementAcceleration);
    if ((double) Mathf.Abs(this._horizontalDirection) <= (double) this._maxMoveSpeed)
      return;
    this._rb.velocity = new Vector2(Mathf.Sign(this._rb.velocity.x) * this._maxMoveSpeed, this._rb.velocity.y);
  }

  private void ApplyGroundLinearDrag()
  {
    if ((double) Mathf.Abs(this._horizontalDirection) < 0.40000000596046448 || this._changingDirection)
      this._rb.drag = this._groundLinearDrag;
    else
      this._rb.drag = 0.0f;
  }

  private void ApplyAirLinearDrag() => this._rb.drag = this._airLinearDrag;

  private void Jump(Vector2 direction)
  {
    if (!this._onGround && !this._onWall)
      --this._extraJumpsValue;
    this.ApplyAirLinearDrag();
    this._rb.velocity = new Vector2(this._rb.velocity.x, 0.0f);
    this._rb.AddForce(direction * this._jumpForce, ForceMode2D.Impulse);
    this._hangTimeCounter = 0.0f;
    this._jumpBufferCounter = 0.0f;
    this._isJumping = true;
  }

  private void WallJump()
  {
    this.Jump(Vector2.up + (this._onRightWall ? Vector2.left : Vector2.right));
  }

  private IEnumerator NeutralWallJump()
  {
    this.Jump(Vector2.up + (this._onRightWall ? Vector2.left : Vector2.right));
    yield return (object) new WaitForSeconds(this._wallJumpXVelocityHaltDelay);
    this._rb.velocity = new Vector2(0.0f, this._rb.velocity.y);
  }

  private void FallMultiplier()
  {
    if ((double) this._verticalDirection < 0.0)
      this._rb.gravityScale = this._downMultiplier;
    else if ((double) this._rb.velocity.y < 0.0)
      this._rb.gravityScale = this._fallMultiplier;
    else if ((double) this._rb.velocity.y > 0.0 && !Input.GetButton("Jump"))
      this._rb.gravityScale = this._lowJumpFallMultiplier;
    else
      this._rb.gravityScale = 1f;
  }

  private void WallGrab()
  {
    this._rb.gravityScale = 0.0f;
    this._rb.velocity = Vector2.zero;
  }

  private void WallSlide()
  {
    this._rb.velocity = new Vector2(this._rb.velocity.x, -this._maxMoveSpeed * this._wallSlideModifier);
  }

  private void WallRun()
  {
    this._rb.velocity = new Vector2(this._rb.velocity.x, this._verticalDirection * this._maxMoveSpeed * this._wallRunModifier);
  }

  private void StickToWall()
  {
    if (this._onRightWall && (double) this._horizontalDirection >= 0.0)
      this._rb.velocity = new Vector2(1f, this._rb.velocity.y);
    else if (!this._onRightWall && (double) this._horizontalDirection <= 0.0)
      this._rb.velocity = new Vector2(-1f, this._rb.velocity.y);
    if (this._onRightWall && !this._facingRight)
    {
      this.Flip();
    }
    else
    {
      if (this._onRightWall || !this._facingRight)
        return;
      this.Flip();
    }
  }

  public bool GetDashStatus() => this._isDashing;

  public void Knockback(int direction)
  {
    this.knockback = true;
    this.knockbackStartTime = Time.time;
    this._rb.velocity = new Vector2(this.knockbackSpeed.x * (float) direction, this.knockbackSpeed.y);
  }

  private void CheckKnockback()
  {
    if ((double) Time.time < (double) this.knockbackStartTime + (double) this.knockbackDuration || !this.knockback)
      return;
    this.knockback = false;
    this._rb.velocity = new Vector2(0.0f, this._rb.velocity.y);
  }

  public void EnableFlip() => this._canFlip = true;

  public void DisableFlip() => this._canFlip = false;

  private void Flip()
  {
    if (!this._canFlip || this.knockback)
      return;
    this._facingRight = !this._facingRight;
    this.transform.Rotate(0.0f, 180f, 0.0f);
  }

  private IEnumerator Dash(float x, float y)
  {
    float dashStartTime = Time.time;
    this._hasDashed = true;
    this._isDashing = true;
    this._onGround = false;
    this._isJumping = false;
    this._rb.velocity = Vector2.zero;
    this._rb.gravityScale = 0.0f;
    this._rb.drag = 0.0f;
    Vector2 dir = (double) x != 0.0 || (double) y != 0.0 ? new Vector2(x, y) : (!this._facingRight ? new Vector2(-1f, 0.0f) : new Vector2(1f, 0.0f));
    while ((double) Time.time < (double) dashStartTime + (double) this._dashLength)
    {
      this._rb.velocity = dir.normalized * this._dashSpeed;
      yield return (object) null;
    }
    this._isDashing = false;
  }

  private void Animation()
  {
    Shadows.me.SombrasSkill();
    if (this._isDashing)
    {
      this._anim.SetBool("isDashing", true);
      this._anim.SetBool("isGrounded", true);
      this._anim.SetBool("isFalling", false);
      this._anim.SetBool("WallGrab", false);
      this._anim.SetBool("isJumping", false);
      this._anim.SetFloat("horizontalDirection", 0.0f);
      this._anim.SetFloat("verticalDirection", 0.0f);
    }
    else
    {
      this._anim.SetBool("isDashing", false);
      if (((double) this._horizontalDirection < 0.0 && this._facingRight || (double) this._horizontalDirection > 0.0 && !this._facingRight) && !this._wallGrab && !this._wallSlide)
        this.Flip();
      if (this._onGround)
      {
        this._anim.SetBool("isGrounded", true);
        this._anim.SetBool("isFalling", false);
        this._anim.SetBool("WallGrab", false);
        this._anim.SetFloat("horizontalDirection", Mathf.Abs(this._horizontalDirection));
      }
      else
        this._anim.SetBool("isGrounded", false);
      if (this._isJumping)
      {
        this._anim.SetBool("isJumping", true);
        this._anim.SetBool("isFalling", false);
        this._anim.SetBool("WallGrab", false);
        this._anim.SetFloat("verticalDirection", 0.0f);
      }
      else
      {
        this._anim.SetBool("isJumping", false);
        if (this._wallGrab || this._wallSlide)
        {
          this._anim.SetBool("WallGrab", true);
          this._anim.SetBool("isFalling", false);
          this._anim.SetFloat("verticalDirection", 0.0f);
        }
        else if ((double) this._rb.velocity.y < 0.0)
        {
          this._anim.SetBool("isFalling", true);
          this._anim.SetBool("WallGrab", false);
          this._anim.SetFloat("verticalDirection", 0.0f);
        }
        if (!this._wallRun)
          return;
        this._anim.SetBool("isFalling", false);
        this._anim.SetBool("WallGrab", false);
        this._anim.SetFloat("verticalDirection", Mathf.Abs(this._verticalDirection));
      }
    }
  }

  public bool GetFacingDirection() => this._facingRight;

  private void CornerCorrect(float Yvelocity)
  {
    RaycastHit2D raycastHit2D1 = Physics2D.Raycast((Vector2) (this.transform.position - this._innerRaycastOffset + Vector3.up * this._topRaycastLength), (Vector2) Vector3.left, this._topRaycastLength, (int) this._groundLayer);
    if ((Object) raycastHit2D1.collider != (Object) null)
    {
      this.transform.position = new Vector3(this.transform.position.x + Vector3.Distance(new Vector3(raycastHit2D1.point.x, this.transform.position.y, 0.0f) + Vector3.up * this._topRaycastLength, this.transform.position - this._edgeRaycastOffset + Vector3.up * this._topRaycastLength), this.transform.position.y, this.transform.position.z);
      this._rb.velocity = new Vector2(this._rb.velocity.x, Yvelocity);
    }
    else
    {
      RaycastHit2D raycastHit2D2 = Physics2D.Raycast((Vector2) (this.transform.position + this._innerRaycastOffset + Vector3.up * this._topRaycastLength), (Vector2) Vector3.right, this._topRaycastLength, (int) this._groundLayer);
      if (!((Object) raycastHit2D2.collider != (Object) null))
        return;
      this.transform.position = new Vector3(this.transform.position.x - Vector3.Distance(new Vector3(raycastHit2D2.point.x, this.transform.position.y, 0.0f) + Vector3.up * this._topRaycastLength, this.transform.position + this._edgeRaycastOffset + Vector3.up * this._topRaycastLength), this.transform.position.y, this.transform.position.z);
      this._rb.velocity = new Vector2(this._rb.velocity.x, Yvelocity);
    }
  }

  private void CheckMovementDirection()
  {
    if ((double) Mathf.Abs(this._rb.velocity.x) >= 0.0099999997764825821)
      this._isWalking = true;
    else
      this._isWalking = false;
  }

  private void CheckCollisions()
  {
    this._onGround = (bool) Physics2D.Raycast((Vector2) (this.transform.position * this._groundRaycastLength), Vector2.down, this._groundRaycastLength, (int) this._groundLayer) || (bool) Physics2D.Raycast((Vector2) (this.transform.position - this._groundRaycastOffset), Vector2.down, this._groundRaycastLength, (int) this._groundLayer);
    this._canCornerCorrect = (bool) Physics2D.Raycast((Vector2) (this.transform.position + this._edgeRaycastOffset), Vector2.up, this._topRaycastLength, (int) this._groundLayer) && !(bool) Physics2D.Raycast((Vector2) (this.transform.position + this._innerRaycastOffset), Vector2.up, this._topRaycastLength, (int) this._groundLayer) || (bool) Physics2D.Raycast((Vector2) (this.transform.position - this._edgeRaycastOffset), Vector2.up, this._topRaycastLength, (int) this._groundLayer) && !(bool) Physics2D.Raycast((Vector2) (this.transform.position - this._innerRaycastOffset), Vector2.up, this._topRaycastLength, (int) this._groundLayer);
    this._onWall = (bool) Physics2D.Raycast((Vector2) this.transform.position, Vector2.right, this._wallRaycastLength, (int) this._wallLayer) || (bool) Physics2D.Raycast((Vector2) this.transform.position, Vector2.left, this._wallRaycastLength, (int) this._wallLayer);
    this._onRightWall = (bool) Physics2D.Raycast((Vector2) this.transform.position, Vector2.right, this._wallRaycastLength, (int) this._wallLayer);
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.green;
    Gizmos.DrawLine(this.transform.position, this.transform.position + Vector3.down * this._groundRaycastLength);
    Gizmos.DrawLine(this.transform.position + this._groundRaycastOffset, this.transform.position + this._groundRaycastOffset + Vector3.down * this._groundRaycastLength);
    Gizmos.DrawLine(this.transform.position - this._groundRaycastOffset, this.transform.position - this._groundRaycastOffset + Vector3.down * this._groundRaycastLength);
    Gizmos.DrawLine(this.transform.position + this._edgeRaycastOffset, this.transform.position + this._edgeRaycastOffset + Vector3.up * this._topRaycastLength);
    Gizmos.DrawLine(this.transform.position - this._edgeRaycastOffset, this.transform.position - this._edgeRaycastOffset + Vector3.up * this._topRaycastLength);
    Gizmos.DrawLine(this.transform.position + this._innerRaycastOffset, this.transform.position + this._innerRaycastOffset + Vector3.up * this._topRaycastLength);
    Gizmos.DrawLine(this.transform.position - this._innerRaycastOffset, this.transform.position - this._innerRaycastOffset + Vector3.up * this._topRaycastLength);
    Gizmos.DrawLine(this.transform.position - this._innerRaycastOffset + Vector3.up * this._topRaycastLength, this.transform.position - this._innerRaycastOffset + Vector3.up * this._topRaycastLength + Vector3.left * this._topRaycastLength);
    Gizmos.DrawLine(this.transform.position + this._innerRaycastOffset + Vector3.up * this._topRaycastLength, this.transform.position + this._innerRaycastOffset + Vector3.up * this._topRaycastLength + Vector3.right * this._topRaycastLength);
    Gizmos.DrawLine(this.transform.position, this.transform.position + Vector3.right * this._wallRaycastLength);
    Gizmos.DrawLine(this.transform.position, this.transform.position + Vector3.left * this._wallRaycastLength);
  }
}
