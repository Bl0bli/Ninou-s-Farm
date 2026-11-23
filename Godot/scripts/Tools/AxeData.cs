using Godot;

/// <summary>Hache : joue l'animation puis fait apparaître une hitbox devant le joueur (port d'AxeData.cs).</summary>
[GlobalClass]
public partial class AxeData : ToolData
{
    [ExportGroup("Specific Settings")]
    /// <summary>Scène de la hitbox (le prefab Axe_HITBOX dans Unity).</summary>
    [Export] private PackedScene _actionScene;
    /// <summary>Calques touchés (le LayerMask Unity) : par défaut « damageable ».</summary>
    [Export(PropertyHint.Layers2DPhysics)] private uint _targetLayers = 32;
    [Export] private int _damage = 1;
    [Export] private float _actionDuration = 0.3f;
    /// <summary>Distance de la hitbox devant le joueur, en cases.</summary>
    [Export] private float _spawnOffset = 0.5f;

    public override bool Use(PlayerController player, Vector2 targetPosition)
    {
        if (player.Actions == null || _actionScene == null) return false;

        if (!string.IsNullOrEmpty(_animationName))
            player.TriggerAnimation(_animationName);

        player.Actions.SpawnAction(_actionScene, _damage, _targetLayers, _actionDuration, _spawnOffset);
        return true;
    }
}
