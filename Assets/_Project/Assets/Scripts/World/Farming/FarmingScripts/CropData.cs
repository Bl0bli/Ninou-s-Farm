using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "CropData", menuName = "Scriptable Objects/CropData")]
public class CropData : ScriptableObject
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private string _cropName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private int _growTime;
    [SerializeField] private List<Sprite> _growStages;
    
    public string CropName => _cropName;
    public Sprite Icon => _icon;
    public int GrowTime => _growTime;
    public List<Sprite> GrowStages => _growStages;
    public GameObject Prefab => _prefab;
}
