using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Transform movePoint;

    public Tilemap pathingMap;
    public Tilemap collisionMap;

    SpriteRenderer spriteRenderer;
    public Sprite[] spriteArray;
    public int animLength;
    public float animSpeed;
    int animIndex;
    int animDirection;
    float animOffset;
    bool animRunning;
    bool animAttacking;

    // Start is called before the first frame update
    void Start()
    {
        movePoint.parent = null;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, movePoint.position, moveSpeed * Time.deltaTime);
        Vector3 moveCandidate;

        Tile curr_tile = (Tile)pathingMap.GetTile(Vector3Int.FloorToInt(movePoint.position));

        if (curr_tile == null)
        {
            curr_tile = (Tile)collisionMap.GetTile(Vector3Int.FloorToInt(movePoint.position));
        }


        // Debug.Log(curr_tile);
        if (Vector3.Distance(transform.position, movePoint.position) <= 0.05f)
        {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f)
            {
                animRunning = true;
                if (Input.GetAxisRaw("Horizontal") > 0) animDirection = 0;
                else animDirection = 3;
                moveCandidate = movePoint.position + new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f);
                if (!Physics2D.Linecast(transform.position, moveCandidate))
                {
                    movePoint.position = moveCandidate;
                }
            }

            if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f)
            {
                animRunning = true;
                if (Input.GetAxisRaw("Vertical") > 0) animDirection = 2;
                else animDirection = 1;
                moveCandidate = movePoint.position + new Vector3(0f, Input.GetAxisRaw("Vertical"), 0f);
                if (!Physics2D.Linecast(transform.position, moveCandidate))
                {
                    movePoint.position = moveCandidate;
                }
            }

            if (Input.GetAxisRaw("Vertical") == 0 && Input.GetAxisRaw("Horizontal") == 0)
            {

                animRunning = false;
            }
        }

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
