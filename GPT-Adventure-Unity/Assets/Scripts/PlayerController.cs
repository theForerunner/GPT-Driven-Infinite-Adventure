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

    // Start is called before the first frame update
    void Start()
    {
        movePoint.parent = null;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, movePoint.position, moveSpeed * Time.deltaTime);
        Vector3 moveCandidate;

        // Tile curr_tile = (Tile)pathingMap.GetTile(Vector3Int.FloorToInt(movePoint.position));

        // Debug.Log(curr_tile);

        if (Vector3.Distance(transform.position, movePoint.position) <= 0.05f)
        {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f)
            {
                moveCandidate = movePoint.position + new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f);
                if (!Physics2D.Linecast(transform.position, moveCandidate))
                {
                    movePoint.position = moveCandidate;
                }
            }

            if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f)
            {
                moveCandidate = movePoint.position + new Vector3(0f, Input.GetAxisRaw("Vertical"), 0f);
                if (!Physics2D.Linecast(transform.position, moveCandidate))
                {
                    movePoint.position = moveCandidate;
                }
            }

        }
    }
}
