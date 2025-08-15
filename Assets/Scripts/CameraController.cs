using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Positions")]
    [Tooltip("카메라 위치 - 상의 표시용")]
    public Vector3 topClothingPosition = new Vector3(0, 1.41f, 1.22f);
    
    [Tooltip("카메라 위치 - 하의 표시용")]
    public Vector3 bottomClothingPosition = new Vector3(0, 0.89f, 1.61f);
    
    [Header("Animation Settings")]
    [Tooltip("카메라 이동 애니메이션 속도")]
    public float moveSpeed = 2.0f;
    
    [Tooltip("카메라 이동 애니메이션을 사용할지 여부")]
    public bool useAnimation = true;
    
    private Camera targetCamera;
    private bool isMoving = false;
    
    void Start()
    {
        // Main Camera 찾기
        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindFirstObjectByType<Camera>();
        }
        
        if (targetCamera == null)
        {
            Debug.LogError("CameraController: 카메라를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 의류 타입에 따라 카메라 위치 설정
    /// </summary>
    public void SetCameraForClothingType(ClothingType clothingType)
    {
        if (targetCamera == null) return;
        
        Vector3 targetPosition = clothingType == ClothingType.Top ? topClothingPosition : bottomClothingPosition;
        
        if (useAnimation && Application.isPlaying)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCameraToPosition(targetPosition));
        }
        else
        {
            targetCamera.transform.position = targetPosition;
        }
        
        Debug.Log($"카메라 위치 변경: {clothingType} -> {targetPosition}");
    }
    
    /// <summary>
    /// 카메라를 부드럽게 이동시키는 코루틴
    /// </summary>
    private System.Collections.IEnumerator MoveCameraToPosition(Vector3 targetPosition)
    {
        isMoving = true;
        Vector3 startPosition = targetCamera.transform.position;
        float elapsedTime = 0f;
        float duration = 1f / moveSpeed;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // Smooth interpolation
            progress = Mathf.SmoothStep(0f, 1f, progress);
            
            targetCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }
        
        targetCamera.transform.position = targetPosition;
        isMoving = false;
    }
    
    /// <summary>
    /// 카메라가 현재 이동 중인지 확인
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
    
    /// <summary>
    /// 즉시 카메라 이동 (애니메이션 없이)
    /// </summary>
    public void SetCameraPositionImmediate(ClothingType clothingType)
    {
        if (targetCamera == null) return;
        
        StopAllCoroutines();
        isMoving = false;
        
        Vector3 targetPosition = clothingType == ClothingType.Top ? topClothingPosition : bottomClothingPosition;
        targetCamera.transform.position = targetPosition;
        
        Debug.Log($"카메라 위치 즉시 변경: {clothingType} -> {targetPosition}");
    }
}