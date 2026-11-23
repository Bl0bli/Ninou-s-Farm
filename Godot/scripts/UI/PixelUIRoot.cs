using Godot;

/// <summary>
/// Racine de l'interface en « pixels du sprite » : ses enfants sont mis en page dans un écran virtuel
/// (1280x720 / _scale), puis tout est agrandi d'un facteur entier. Les sprites d'UI restent nets,
/// et on règle les tailles directement en pixels de la feuille (une case = 28x30).
///
/// Unity faisait l'inverse : Canvas en pixels d'écran et « Pixels Per Unit Multiplier » sur chaque Image.
/// </summary>
public partial class PixelUIRoot : Control
{
    [Export(PropertyHint.Range, "1,8")] private int _scale = 2;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        GetViewport().SizeChanged += Fit;
        Fit();
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= Fit;

    private void Fit()
    {
        SetAnchorsPreset(LayoutPreset.TopLeft);
        Position = Vector2.Zero;
        Scale = new Vector2(_scale, _scale);
        Size = GetViewportRect().Size / _scale;
    }
}
