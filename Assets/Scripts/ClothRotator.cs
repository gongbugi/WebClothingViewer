using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 의류 모델 전용 회전 컨트롤러
/// 마우스 클릭 및 드래그로 의류 모델을 회전시킬 수 있습니다.
/// </summary>
public class ClothRotator : MonoBehaviour
{
    [Header("회전 설정")]
    [Tooltip("회전 속도")]
    public float rotationSpeed = 100f;
    
    [Tooltip("Y축 회전만 허용 (좌우 회전)")]
    public bool yAxisOnly = true;
    
    [Tooltip("X축 회전 허용 (상하 회전)")]
    public bool enableXRotation = false;
    
    [Tooltip("회전 범위 제한 (X축, 0이면 제한 없음)")]
    public float maxXRotation = 30f;
    
    [Header("입력 설정")]
    [Tooltip("마우스 감도")]
    public float mouseSensitivity = 1f;
    
    [Tooltip("관성 활성화")]
    public bool enableInertia = false;
    
    [Tooltip("관성 감쇠 속도")]
    public float inertiaDamping = 5f;
    
    private bool isRotating = false;
    private Camera mainCamera;
    private Vector3 lastMousePosition;
    private Vector3 rotationVelocity = Vector3.zero;
    private float currentXRotation = 0f;
    
    void Start()
    {
        InitializeCamera();
    }
    
    private void InitializeCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("ClothRotator: 카메라를 찾을 수 없습니다!");
            enabled = false;
        }
        else
        {
            Debug.Log($"ClothRotator: 카메라 초기화 완료 - {mainCamera.name}");
        }
    }

    void Update()
    {
        // 카메라 null 체크 및 재초기화 시도
        if (mainCamera == null)
        {
            InitializeCamera();
            if (mainCamera == null) return;
        }
        
        HandleMouseInput();
        
        // 관성 처리
        if (enableInertia && !isRotating && rotationVelocity.magnitude > 0.01f)
        {
            ApplyInertia();
        }
    }
    
    private void HandleMouseInput()
    {
        // Mouse.current null 체크
        if (Mouse.current == null) return;
        
        // 마우스 왼쪽 버튼 입력 확인
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // UI 요소 위에서 클릭이 시작되었는지 확인
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return; // UI 위라면 회전을 시작하지 않음
            }

            // 마우스 클릭 위치에서 Ray 검사
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            // 이 게임 오브젝트의 Collider와 충돌했는지 확인
            if (Physics.Raycast(ray, out hit) && hit.transform == transform)
            {
                StartRotation();
            }
        }

        // 마우스 왼쪽 버튼을 떼면 회전 멈춤
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            StopRotation();
        }

        // 회전 중일 때 마우스 움직임에 따라 회전
        if (isRotating)
        {
            PerformRotation();
        }
    }
    
    private void StartRotation()
    {
        isRotating = true;
        
        if (Mouse.current != null)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
        }
        
        rotationVelocity = Vector3.zero;
        
        Debug.Log("의류 회전 시작");
    }
    
    private void StopRotation()
    {
        isRotating = false;
        
        Debug.Log("의류 회전 종료");
    }
    
    private void PerformRotation()
    {
        if (Mouse.current == null) return;
        
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
        
        // Y축 회전 (좌우)
        if (yAxisOnly || mouseDelta.x != 0)
        {
            float yRotation = -mouseDelta.x * rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, yRotation, Space.World);
            
            if (enableInertia)
            {
                rotationVelocity.y = yRotation;
            }
        }
        
        // X축 회전 (상하) - 옵션
        if (enableXRotation && mouseDelta.y != 0)
        {
            float xRotation = mouseDelta.y * rotationSpeed * Time.deltaTime;
            
            // 회전 범위 제한
            if (maxXRotation > 0)
            {
                float newXRotation = currentXRotation + xRotation;
                newXRotation = Mathf.Clamp(newXRotation, -maxXRotation, maxXRotation);
                xRotation = newXRotation - currentXRotation;
                currentXRotation = newXRotation;
            }
            
            transform.Rotate(Vector3.right, xRotation, Space.Self);
            
            if (enableInertia)
            {
                rotationVelocity.x = xRotation;
            }
        }
    }
    
    private void ApplyInertia()
    {
        // Y축 관성
        if (Mathf.Abs(rotationVelocity.y) > 0.01f)
        {
            transform.Rotate(Vector3.up, rotationVelocity.y, Space.World);
            rotationVelocity.y = Mathf.Lerp(rotationVelocity.y, 0, inertiaDamping * Time.deltaTime);
        }
        
        // X축 관성
        if (enableXRotation && Mathf.Abs(rotationVelocity.x) > 0.01f)
        {
            float xRotation = rotationVelocity.x;
            
            // 회전 범위 제한
            if (maxXRotation > 0)
            {
                float newXRotation = currentXRotation + xRotation;
                newXRotation = Mathf.Clamp(newXRotation, -maxXRotation, maxXRotation);
                xRotation = newXRotation - currentXRotation;
                currentXRotation = newXRotation;
            }
            
            transform.Rotate(Vector3.right, xRotation, Space.Self);
            rotationVelocity.x = Mathf.Lerp(rotationVelocity.x, 0, inertiaDamping * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// 회전 상태 초기화
    /// </summary>
    public void ResetRotation()
    {
        isRotating = false;
        rotationVelocity = Vector3.zero;
        currentXRotation = 0f;
        
        Debug.Log("ClothRotator 상태 초기화");
    }
    
    /// <summary>
    /// 현재 회전 중인지 확인
    /// </summary>
    public bool IsRotating()
    {
        return isRotating;
    }
}