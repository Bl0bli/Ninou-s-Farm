using DG.Tweening;
using UnityEngine;

public class CropInstance : Dropable
{
    private SpriteRenderer _spriteRenderer;
    private CropData _cropData;
    
    private Vector2Int _cell;
    private int _elapsedHours;
    private int _stage = -1;

    public bool IsGrown => _stage >= _cropData.GrowStages.Count - 1;

    public void Init(CropData cropData, Vector2Int cell)
    {
        _cropData = cropData;
        _cell = cell;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _elapsedHours = 0;
        _itemDropped = _cropData.Loot;
        SetStage(0);
        TimeManager.OnHourChanged += OnHourChanged;
    }

    private void OnDestroy()
    {
        TimeManager.OnHourChanged -= OnHourChanged;
        TileGridLocator.Current?.Release(_cell, this);
    }

    /// <summary>
    /// Arrache la culture : une plante mûre donne sa récolte, une plante encore en pousse rend sa graine.
    /// </summary>
    public void Harvest()
    {
        _itemDropped = IsGrown ? _cropData.Loot : _cropData.Seed;

        if (_itemDropped == null) Destroy(gameObject);
        else DropItem(true);
    }

    private void OnHourChanged(float _)
    {
        _elapsedHours++;
        SetStage(ComputeStage());
    }
    
    private int ComputeStage()
    {
        int lastStage = _cropData.GrowStages.Count - 1;
        if (_cropData.GrowTime <= 0) return lastStage;

        float progress = Mathf.Clamp01(_elapsedHours / (float)_cropData.GrowTime);
        return Mathf.FloorToInt(progress * lastStage);
    }

    private void SetStage(int stage)
    {
        if (stage <= _stage) return;

        _stage = stage;
        _spriteRenderer.sprite = _cropData.GrowStages[_stage];

        if (IsGrown)
        {
            TimeManager.OnHourChanged -= OnHourChanged;
            Debug.Log($"Growth Completed for {gameObject.name}");
        }
    }
}
