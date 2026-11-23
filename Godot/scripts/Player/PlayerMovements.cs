using System.Linq;
using Godot;

/// <summary>
/// Déplacement et animations du joueur : port de PlayerMovements.cs + de l'Animator Unity.
///
/// Correspondances Unity → Godot :
///  - Rigidbody2D.MovePosition dans FixedUpdate → CharacterBody2D.MoveAndSlide dans _PhysicsProcess ;
///  - Input System (callback OnPlayerMove) → Input.GetVector sur les actions de project.godot ;
///  - Animator (blend tree Dir_x / Dir_y / IsMoving) → AnimatedSprite2D, l'animation est choisie en code ;
///  - LockMovementBehaviour (StateMachineBehaviour) → PlayAction() bloque le mouvement jusqu'à la fin de l'animation.
///
/// Unités : Unity travaille en cases (16 px), Godot en pixels. Une vitesse de 3 cases/s devient 48 px/s.
/// </summary>
public partial class PlayerMovements : CharacterBody2D
{
    public const float PixelsPerUnit = 16f;

    [Export] private AnimatedSprite2D _sprite;

    /// <summary>Vitesse de marche, en cases par seconde (comme _speedScaler dans Unity).</summary>
    [Export] private float _speedScaler = 3f;

    /// <summary>Vitesse de lecture des animations au repos (Animator.speed dans Unity).</summary>
    [Export] private float _speedIdle = 1.25f;

    private Vector2 _currentInput;
    private Vector2 _lastDirection = Vector2.Down;
    private bool _isMoving;
    private bool _isActing;

    public bool CanMove = true;

    /// <summary>Une animation d'action (houe, arrosoir...) est en cours.</summary>
    public bool IsActing => _isActing;

    /// <summary>Dernière direction regardée, en convention Godot (Y vers le BAS : « haut » vaut (0,-1)).</summary>
    public Vector2 LastDirection => _lastDirection;

    public override void _Ready()
    {
        _sprite.AnimationFinished += OnAnimationFinished;
        UpdateAnimation();
    }

    public override void _PhysicsProcess(double delta)
    {
        ReadInput();

        Velocity = CanMove ? _currentInput * _speedScaler * PixelsPerUnit : Vector2.Zero;
        MoveAndSlide();
    }

    private void ReadInput()
    {
        Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool wasMoving = _isMoving;
        Vector2 previous = _currentInput;

        _currentInput = input;
        _isMoving = input.LengthSquared() > 0.01f;
        if (_isMoving) _lastDirection = input;

        if (wasMoving != _isMoving || previous != input) UpdateAnimation();
    }

    /// <summary>
    /// Joue une animation d'action (chop, hoe, water...) dans la direction regardée, et bloque le déplacement
    /// jusqu'à sa fin. Remplace le SetTrigger + LockMovementBehaviour de l'Animator Unity.
    /// </summary>
    /// <returns>False si l'animation n'existe pas (pas encore portée).</returns>
    public bool PlayAction(string action)
    {
        string anim = $"{action}_{DirectionSuffix(_lastDirection)}";
        if (!_sprite.SpriteFrames.HasAnimation(anim))
        {
            GD.PushWarning($"[PlayerMovements] Animation '{anim}' absente de SpriteFrames.");
            return false;
        }

        _isActing = true;
        CanMove = false;
        _sprite.SpeedScale = 1f;
        PlayAnimation(anim);
        return true;
    }

    // ------------------------------------------------------------------ Contrôle externe (pêche)
    // La pêche enchaîne plusieurs animations (lancer, attente, combat, fin) : PlayerFishing prend la main
    // sur le sprite jusqu'à ce qu'il la rende. Équivalent des états « Fishing » de l'Animator Unity.

    private bool _externalControl;

    /// <summary>Déclenché à la fin de chaque animation non bouclée, pendant un contrôle externe.</summary>
    public event System.Action<string> ExternalAnimationFinished;

    /// <summary>Bloque le déplacement et les animations automatiques ; c'est l'appelant qui joue les animations.</summary>
    public void BeginExternalControl()
    {
        _externalControl = true;
        _isActing = true;
        CanMove = false;
        _sprite.SpeedScale = 1f;
    }

    /// <summary>Joue « action_direction » (ex : fishing_idle_left) pendant un contrôle externe.</summary>
    public void PlayDirectional(string action)
    {
        string anim = $"{action}_{DirectionSuffix(_lastDirection)}";
        if (_sprite.SpriteFrames.HasAnimation(anim)) PlayAnimation(anim);
        else GD.PushWarning($"[PlayerMovements] Animation '{anim}' absente de SpriteFrames.");
    }

    /// <summary>Rend la main : le joueur peut de nouveau bouger.</summary>
    public void EndExternalControl()
    {
        _externalControl = false;
        _isActing = false;
        CanMove = true;
        UpdateAnimation();
    }

    private void OnAnimationFinished()
    {
        if (_externalControl)
        {
            ExternalAnimationFinished?.Invoke(_sprite.Animation);
            return;
        }
        if (!_isActing) return;
        _isActing = false;
        CanMove = true;
        UpdateAnimation();
    }

    /// <summary>
    /// Joue une animation en appliquant le retournement horizontal de son clip Unity (m_FlipX) :
    /// les clips « droite » de la pêche réutilisent la feuille de côté retournée (métadonnée flip_h).
    /// </summary>
    private void PlayAnimation(string anim)
    {
        _sprite.FlipH = _sprite.SpriteFrames.HasMeta("flip_h") &&
                        _sprite.SpriteFrames.GetMeta("flip_h").AsStringArray().Contains(anim);
        _sprite.Play(anim);
    }

    private void UpdateAnimation()
    {
        if (_sprite == null || _isActing) return;

        bool moving = _isMoving && CanMove;
        Vector2 dir = moving ? _currentInput : _lastDirection;
        _sprite.SpeedScale = moving ? _speedScaler / 2f : _speedIdle;
        PlayAnimation(moving ? MovingAnimation(dir) : $"idle_{DirectionSuffix(dir)}");
    }

    /// <summary>
    /// Animation de marche sur 8 directions (le blend tree 2D d'Unity avait 8 clips). Chaque diagonale occupe
    /// un octant de 45° : elle est choisie quand le plus petit des deux axes dépasse environ 41 % du plus grand
    /// (tan 22,5°). Une direction de clavier en diagonale vaut (0,71 ; 0,71) : elle tombe donc en plein dedans.
    /// </summary>
    private string MovingAnimation(Vector2 dir)
    {
        float ax = Mathf.Abs(dir.X), ay = Mathf.Abs(dir.Y);
        if (Mathf.Min(ax, ay) > Mathf.Max(ax, ay) * 0.414f)
        {
            string diagonal = $"moving_{(dir.Y < 0 ? "up" : "down")}{(dir.X < 0 ? "left" : "right")}";
            if (_sprite.SpriteFrames.HasAnimation(diagonal)) return diagonal;
        }
        return $"moving_{DirectionSuffix(dir)}";
    }

    /// <summary>Direction dominante, comme le blend tree 2D de l'Animator (diagonales → côté le plus fort).</summary>
    public static string DirectionSuffix(Vector2 dir)
    {
        if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y)) return dir.X > 0 ? "right" : "left";
        return dir.Y < 0 ? "up" : "down";
    }
}
