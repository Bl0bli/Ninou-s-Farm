using Godot;

/// <summary>
/// Une ligne de la table de pêche (la struct FishLootTable.Entry d'Unity) : un poisson et son poids.
/// Petite Resource, comme SplashSet : c'est ainsi qu'on édite une liste de couples dans l'Inspecteur Godot.
/// </summary>
[GlobalClass]
public partial class FishLootEntry : Resource
{
    [Export] public FishData Fish;
    [Export(PropertyHint.Range, "0,1000,or_greater")] public float Weight = 1f;
}
