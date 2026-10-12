using System;
using UnityEngine;

/// <summary>
/// Logique pure de la jauge de pêche : un curseur fait des allers-retours entre 0 (bas) et 1 (haut),
/// le joueur doit valider quand il est dans la zone cible. Aucune dépendance à Unity à part Mathf :
/// la jauge est pilotée par Tick(dt), ce qui la rend testable sans lancer le jeu.
/// </summary>
public class FishingGauge
{
    private readonly float _speed;
    private int _direction = 1;

    /// <summary>Position du curseur, de 0 (bas de la jauge) à 1 (haut).</summary>
    public float Cursor { get; private set; }

    /// <summary>Bornes de la zone cible, de 0 à 1.</summary>
    public float ZoneMin { get; }
    public float ZoneMax { get; }

    public bool IsRunning { get; private set; }

    public event Action<float> OnCursorMoved;

    /// <param name="zoneCenter">Centre de la zone cible (0 à 1).</param>
    /// <param name="zoneWidth">Largeur de la zone cible (0 à 1).</param>
    /// <param name="cursorSpeed">Vitesse du curseur, en longueurs de jauge par seconde.</param>
    public FishingGauge(float zoneCenter, float zoneWidth, float cursorSpeed)
    {
        float half = Mathf.Clamp(zoneWidth, 0.01f, 1f) / 2f;
        zoneCenter = Mathf.Clamp(zoneCenter, half, 1f - half);

        ZoneMin = zoneCenter - half;
        ZoneMax = zoneCenter + half;
        _speed = Mathf.Max(0f, cursorSpeed);
        IsRunning = true;
    }

    /// <summary>
    /// Fait avancer le curseur ; il rebondit sur les extrémités sans perdre de distance.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!IsRunning) return;

        float next = Cursor + _direction * _speed * deltaTime;

        // Plusieurs rebonds possibles si le pas est très grand (gros dt ou curseur rapide).
        while (next > 1f || next < 0f)
        {
            if (next > 1f) { next = 2f - next; _direction = -1; }
            else { next = -next; _direction = 1; }
        }

        Cursor = next;
        OnCursorMoved?.Invoke(Cursor);
    }

    /// <summary>
    /// Valide le clic du joueur : arrête le curseur et indique s'il est dans la zone.
    /// </summary>
    public bool Confirm()
    {
        if (!IsRunning) return false;

        IsRunning = false;
        return Cursor >= ZoneMin && Cursor <= ZoneMax;
    }

    /// <summary>
    /// Arrête la jauge sans la valider (délai écoulé, pêche annulée).
    /// </summary>
    public void Stop() => IsRunning = false;
}
