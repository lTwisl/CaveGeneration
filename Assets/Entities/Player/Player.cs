using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private float _speed;

    private Rigidbody2D _rb;

    float inputHorizontal;
    float inputVertical;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    
    void Update()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal");
        inputVertical = Input.GetAxisRaw("Vertical");
    }

    private void FixedUpdate()
    {
        _rb.AddForce(new Vector2(inputHorizontal, inputVertical) * _speed * Time.fixedDeltaTime, ForceMode2D.Impulse);
    }

    public void Init(float speed)
    {
        _speed = speed;
    }
}
