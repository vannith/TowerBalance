using UnityEngine;
using System.Collections;
using System;
using System.Linq;

public class BlockPlacer : MonoBehaviour
{
    private const KeyCode k_PlacementButton = KeyCode.Space;
    private const string k_BlockTag = "Block";
    private const string k_CameraLimitLayer = "CameraLimits";

    private Rigidbody2D m_HeldBlock = null;
    private bool m_WaitingOnNextBlock = true;
    private float m_HorizontalDropPosition;
    private float m_RightBorderHorizontalPosition;
    private float m_LeftBorderHorizontalPosition;
    private int m_LandedBlocks = 0;
    private bool m_ReadyToPlay = false;
    private int m_HeightOfBlockInPixels;
    private int m_WidthOfBlockInPixels;

    private Coroutine m_PlacerMover;
    
    [SerializeField]
    private float m_TimeBetweenBlocks;
    [SerializeField]
    private AnimationCurve m_SideToSideTimePerLandedBlocks;
    
    // Use this for initialization
    void Start()
    {
        m_HeldBlock = ObjectPoolManager.PullObject(k_BlockTag).GetComponent<Rigidbody2D>();
        m_HeldBlock.GetComponent<Collider2D>().enabled = false;

        SpriteRenderer sprite = m_HeldBlock.GetComponent<SpriteRenderer>();
        m_HeightOfBlockInPixels = sprite.sprite.texture.height;
        m_WidthOfBlockInPixels = sprite.sprite.texture.width;

        Camera.main.orthographicSize = Camera.main.pixelHeight / sprite.sprite.pixelsPerUnit / 2;
        StartCoroutine(scrollUpForGameStart());
        
        m_HorizontalDropPosition = m_RightBorderHorizontalPosition = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width - m_WidthOfBlockInPixels / 2, Screen.height / 2, transform.position.z)).x;
        m_LeftBorderHorizontalPosition = Camera.main.ScreenToWorldPoint(new Vector3(0  + m_WidthOfBlockInPixels / 2, Screen.height / 2, transform.position.z)).x;
       
        m_PlacerMover = StartCoroutine(movePlacerPosition());
    }

    // Update is called once per frame
    void Update()
    {
        if (m_ReadyToPlay)
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
        while (!m_ReadyToPlay)
        {
            yield return null;
        }

        bool movingLeft = true;
        
        float timeAtStart = Time.time;
        float timeAtFinish = timeAtStart + m_SideToSideTimePerLandedBlocks.Evaluate(m_LandedBlocks);
         
        while (true)
        {
            float percentComplete = (Time.time - timeAtStart) / (timeAtFinish - timeAtStart);
            
            m_HorizontalDropPosition = Mathf.Lerp(movingLeft ? m_RightBorderHorizontalPosition : m_LeftBorderHorizontalPosition, movingLeft ? m_LeftBorderHorizontalPosition : m_RightBorderHorizontalPosition, percentComplete);
            
            if (percentComplete >= 1)
            {
                timeAtStart = Time.time;
                timeAtFinish = timeAtStart + m_SideToSideTimePerLandedBlocks.Evaluate(m_LandedBlocks);
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

    private IEnumerator scrollUpForGameStart()
    {
        for (int i = 0; i < Mathf.RoundToInt(Camera.main.orthographicSize) - 1; ++i)
        {
            yield return StartCoroutine(scrollUp1Line(0.5f));

            yield return null;
        }

        m_ReadyToPlay = true;
        m_HeldBlock.GetComponent<Collider2D>().enabled = true;
    }

    private IEnumerator scrollUp1Line(float i_TimePerScroll = 1)
    {
        float timeAtStart = Time.time;
        float timeAtFinish = timeAtStart + i_TimePerScroll;
        float percentComplete = 0;

        float startingVerticalPosition = transform.position.y;
        Vector3 currentScreenPosition = Camera.main.WorldToScreenPoint(transform.position);
        Vector3 targetWorldPosition = Camera.main.ScreenToWorldPoint(currentScreenPosition + Vector3.up * m_HeightOfBlockInPixels);
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
