using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Magnetable : MonoBehaviour
{
    private Rigidbody2D _rb;
    
    public Rigidbody2D Rigidbody2D => _rb;
    
    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
    }
}
