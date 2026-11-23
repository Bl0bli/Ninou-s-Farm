using Godot;

/// <summary>
/// L'eau d'une direction pour l'arrosoir : ses images (cadre 48x48, comme le personnage) et le décalage en pixels
/// qui amène le filet d'eau sous le bec de l'arrosoir. Struct [Serializable] dans Unity → petite Resource ici,
/// seule façon d'avoir un groupe de champs imbriqué éditable dans l'Inspecteur Godot.
/// </summary>
[GlobalClass]
public partial class SplashSet : Resource
{
    [Export] public Texture2D[] Frames = System.Array.Empty<Texture2D>();

    /// <summary>Décalage en pixels depuis le sprite du personnage (x vers la droite, y vers le bas).</summary>
    [Export] public Vector2I PixelOffset;

    /// <summary>Dessiner l'eau derrière le personnage (arrosoir caché par le corps, de dos).</summary>
    [Export] public bool Behind;
}
