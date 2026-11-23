using Godot;

/// <summary>
/// Arbre (Tree.cs dans Unity ; renommé car Godot a déjà une classe Tree, un contrôle d'interface) : encaisse les coups de hache, tombe au dernier coup puis lâche son bois (port de Tree.cs).
///
/// Animator (trigger Hit, booléen Cut) + coroutine qui attend la fin de « Cut » → AnimatedSprite2D et
/// le signal AnimationFinished : la fin de « hit » enchaîne sur « cut » ou « idle », la fin de « cut » lâche l'objet.
/// </summary>
public partial class WorldTree : Dropable, IDamageable
{
    [Export] private int _maxHealth = 3;
    [Export] private AnimatedSprite2D _sprite;
    [Export] private string _idleAnimName = "idle";
    [Export] private string _hitAnimName = "hit";
    [Export] private string _cutAnimName = "cut";

    private int _currentHealth;
    private bool _isCut;
    private bool _hasDropped;

    public override void _Ready()
    {
        _currentHealth = _maxHealth;
        _sprite.AnimationFinished += OnAnimationFinished;
        _sprite.Play(_idleAnimName);
    }

    public void TakeDamage(int amount, Attacker attacker)
    {
        if (_isCut) return;

        _currentHealth -= amount;
        GD.Print($"[Tree] Took {amount} damage. Current health: {_currentHealth}");

        _sprite.Play(_hitAnimName);
        _sprite.Frame = 0;

        if (_currentHealth > 0) return;

        _isCut = true;
        // L'arbre tombe du côté opposé au coup. Échelle X négative (et pas FlipH) : le miroir se fait autour
        // de l'origine du nœud, c'est-à-dire du pied du tronc, comme flipX autour du pivot dans Unity.
        if (attacker.AttackDirection == Vector2.Down || attacker.AttackDirection == Vector2.Right)
            _sprite.Scale = new Vector2(-Mathf.Abs(_sprite.Scale.X), _sprite.Scale.Y);
    }

    private void OnAnimationFinished()
    {
        string current = _sprite.Animation; // StringName -> string : comparaison de texte, sans ambiguïté
        if (current == _hitAnimName)
        {
            _sprite.Play(_isCut ? _cutAnimName : _idleAnimName);
        }
        else if (current == _cutAnimName && !_hasDropped)
        {
            _hasDropped = true;
            DropItem(true);
        }
    }
}
