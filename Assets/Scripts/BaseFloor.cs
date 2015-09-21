using UnityEngine;
using System.Collections;

public class BaseFloor : MonoBehaviour
{
    private string k_BlockTag = "Block";

    private int m_BlocksTouchingTheFloor = 0;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnCollisionEnter2D(Collision2D i_Collision)
    {
        if (i_Collision.collider.CompareTag(k_BlockTag))
        {
            ++m_BlocksTouchingTheFloor;

            if (m_BlocksTouchingTheFloor > 1)
            {
                Debug.Log("Lost");
            }
        }
    }
    
    void OnCollisionExit2D(Collision2D i_Collision)
    {
        if (i_Collision.collider.CompareTag(k_BlockTag))
        {
            --m_BlocksTouchingTheFloor;
        }
    }
}
