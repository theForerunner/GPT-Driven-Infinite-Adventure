using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharAnimation : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    public Sprite[] spriteArray;
    public int animLength;
    public float animSpeed;

    public int animDirection = 0;
    float animOffset;
    public bool animRunning = false;
    public bool animAttacking = false;

    void Start()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    void Update()
    {

        int animGroupStart = 4; // idle
        if (animAttacking)
        {
            animGroupStart = 0;
        } else if (animRunning)
        {
            animGroupStart = 8;
        }


        spriteRenderer.sprite = spriteArray[
            animGroupStart * animLength +
            animDirection * animLength +
            (int)animOffset
        ];
        animOffset = animOffset + animSpeed * Time.deltaTime;
        if (animOffset > animLength)
        {
            animOffset = animOffset % animLength;
            animAttacking = false;
        }
    }
}
