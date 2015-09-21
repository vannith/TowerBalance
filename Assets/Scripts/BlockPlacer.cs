using UnityEngine;
using System.Collections;
using System;
using System.Linq;

public class BlockPlacer : MonoBehaviour
{
    private const KeyCode k_PlacementButton = KeyCode.Space;
    private const string k_BlockTag = "Block";
    private const string k_CameraLimitLayer = "CameraLimits";
    private const int k_HeightOfBlockInPixels = 32;

    private Rigidbody2D m_HeldBlock = null;
    private bool m_WaitingOnNextBlock = true;
    private float m_HorizontalDropPosition;
    private float m_RightBorderHorizontalPosition;
    private float m_LeftBorderHorizontalPosition;
    private int m_LandedBlocks = 0;

    private Coroutine m_PlacerMover;
    
    [SerializeField]
    private float m_TimeBetweenBlocks;
    [SerializeField]
    private AnimationCurve m_SpeedPerLandedBlocks;


    // Use this for initialization
    void Start()
    {
        m_HeldBlock = ObjectPoolManager.PullObject(k_BlockTag).GetComponent<Rigidbody2D>();
        m_HeldBlock.transform.position = transform.position;

        //Checks if we have any camera limits set up correctly
        RaycastHit2D hit = Physics2D.RaycastAll(transform.position, Vector2.right).First((i_Hit) => i_Hit.transform.gameObject.layer == LayerMask.NameToLayer(k_CameraLimitLayer));
        if (!hit)
        {
            Debug.LogError("No camera limits found. Please set up the camera limits correctly(As childs of the camera and on the edges of it, giving them the CameraLimits layer).");
        }
        else
        {
            m_HorizontalDropPosition = m_RightBorderHorizontalPosition = hit.transform.position.x - m_HeldBlock.transform.localScale.x;
        }

        hit = Physics2D.RaycastAll(transform.position, Vector2.left).First((i_Hit) => i_Hit.transform.gameObject.layer == LayerMask.NameToLayer(k_CameraLimitLayer));
        if (!hit)
        {
            Debug.LogError("No camera limits found. Please set up the camera limits correctly(As childs of the camera and on the edges of it, giving them the CameraLimits layer).");
        }
        else
        {
            m_LeftBorderHorizontalPosition = hit.transform.position.x + m_HeldBlock.transform.localScale.x;
        }

        m_PlacerMover = StartCoroutine(movePlacerPosition());
    }

    // Update is called once per frame
    void Update()
    {
        if (m_HeldBlock)
        {
            m_HeldBlock.transform.position = new Vector2(m_HorizontalDropPosition, transform.position.y);

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(k_PlacementButton))
            {
                dropBlock();
            }
#elif UNITY_ANDROID || UNITY_IOS || UNITY_BLACKBERRY || UNITY_WP8 || UNITY_TIZEN
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                dropBlock();
            }
#endif
        }
        else
        {
            if (m_WaitingOnNextBlock)
            {
                StartCoroutine(readyNextBlock());
            }
        }
    }

    private void dropBlock()
    {
        ++m_LandedBlocks;

        m_HeldBlock.gravityScale = 1;
        m_HeldBlock = null;
        m_WaitingOnNextBlock = true;
        
        StopCoroutine(m_PlacerMover);
        
        StartCoroutine(scrollUp1Line());
    }
    
    private IEnumerator movePlacerPosition()
    {
        bool movingLeft = true;
        
        float timeAtStart = Time.time;
        float timeAtFinish = timeAtStart + m_SpeedPerLandedBlocks.Evaluate(m_LandedBlocks);
         
        while (true)
        {
            float percentComplete = (Time.time - timeAtStart) / (timeAtFinish - timeAtStart);
            
            m_HorizontalDropPosition = Mathf.Lerp(movingLeft ? m_RightBorderHorizontalPosition : m_LeftBorderHorizontalPosition, movingLeft ? m_LeftBorderHorizontalPosition : m_RightBorderHorizontalPosition, percentComplete);
            
            if (percentComplete >= 1)
            {
                timeAtStart = Time.time;
                timeAtFinish = timeAtStart + m_SpeedPerLandedBlocks.Evaluate(m_LandedBlocks);
                movingLeft = !movingLeft;
            }

            yield return null;
        }
    }

    private IEnumerator readyNextBlock()
    {
        m_WaitingOnNextBlock = false;

        yield return new WaitForSeconds(m_TimeBetweenBlocks);

        m_HeldBlock = ObjectPoolManager.PullObject(k_BlockTag).GetComponent<Rigidbody2D>();
        m_PlacerMover = StartCoroutine(movePlacerPosition());
    }

    private IEnumerator scrollUp1Line()
    {
        float timeAtStart = Time.time;
        float timeAtFinish = timeAtStart + 1;
        float percentComplete = 0;

        float startingVerticalPosition = transform.position.y;
        Vector3 currentScreenPosition = Camera.main.WorldToScreenPoint(transform.position);
        Vector3 targetWorldPosition = Camera.main.ScreenToWorldPoint(currentScreenPosition + Vector3.up * k_HeightOfBlockInPixels);
        float endingVerticalPosition = targetWorldPosition.y;

        do
        {
            percentComplete = (Time.time - timeAtStart) / (timeAtFinish - timeAtStart);

            float newYPosition = Mathf.Lerp(startingVerticalPosition, endingVerticalPosition, percentComplete);
            transform.position = new Vector3(transform.position.x, newYPosition, transform.position.z);

            yield return null;
        } while (percentComplete < 1);
    }
}
