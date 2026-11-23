using Godot;

public interface IDamageable
{
    /// <summary>Applique un montant de dégâts à l'objet (port direct d'IDamageable).</summary>
    void TakeDamage(int amount, Attacker attacker);
}

public struct Attacker
{
    /// <summary>Direction du coup (Y vers le bas : un coup vers le bas vaut (0, 1)).</summary>
    public Vector2 AttackDirection;
}
