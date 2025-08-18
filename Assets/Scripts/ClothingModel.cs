using UnityEngine;

public class ClothingModel : MonoBehaviour
{
    [Header("현재 표시중인 옷 정보")]
    public OutfitData currentOutfit;
    
    [Header("3D 모델 설정")]
    public Transform modelParent; // 3D 모델이 생성될 부모 Transform
    public Vector3 modelRotation = Vector3.zero;
    public Vector3 modelScale = Vector3.one;
    
    [Header("카메라 제어")]
    public CameraController cameraController; // 카메라 컨트롤러 참조
    
    [Header("회전 제어")]
    [Tooltip("마우스 클릭으로 의류 모델 회전 기능 활성화")]
    public bool enableRotation = true;
    
    [Tooltip("회전 속도 (기본값: 100)")]
    public float rotationSpeed = 100f;
    
    [Tooltip("Fallback Collider 자동 추가 (메쉬에 Collider가 없을 때만)")]
    public bool autoAddCollider = false;
    
    [Header("Material 설정")]
    [Tooltip("의류 텍스처에 사용할 공통 Material (Base Map 변경)")]
    public Material clothMaterial;
    
    private GameObject currentModelObject;
    private Renderer currentRenderer;
    
    // 텍스처 캐싱을 위한 딕셔너리
    private static System.Collections.Generic.Dictionary<string, Texture2D> textureCache = 
        new System.Collections.Generic.Dictionary<string, Texture2D>();
    
    void Start()
    {
        // CameraController가 할당되지 않은 경우 자동으로 찾기
        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("CameraController를 찾을 수 없습니다. 카메라 자동 조정이 비활성화됩니다.");
            }
        }
        
        // Cloth_Texture Material 자동 로드
        if (clothMaterial == null)
        {
            clothMaterial = Resources.Load<Material>("Cloth_Texture");
            if (clothMaterial == null)
            {
                Debug.LogWarning("Cloth_Texture.mat을 Resources 폴더에서 찾을 수 없습니다!");
            }
            else
            {
                Debug.Log("Cloth_Texture Material 로드 완료");
            }
        }
        
        // 초기 상태에서는 아무 옷도 표시하지 않음
    }
    
    /// <summary>
    /// 웹에서 호출될 옷 변경 메서드 (ID 기반)
    /// </summary>
    public void ChangeClothing(string outfitId)
    {
        Debug.Log($"옷 변경 요청: {outfitId}");
        
        // OutfitDataLoader에서 옷 데이터 찾기
        if (OutfitDataLoader.Instance == null || !OutfitDataLoader.Instance.IsDataLoaded())
        {
            Debug.LogError("OutfitDataLoader가 초기화되지 않았거나 데이터가 로드되지 않았습니다!");
            return;
        }
        
        OutfitData outfitData = OutfitDataLoader.Instance.GetOutfitById(outfitId);
        if (outfitData == null)
        {
            Debug.LogError($"옷 데이터를 찾을 수 없습니다: {outfitId}");
            return;
        }
        
        ChangeClothing(outfitData);
    }

    /// <summary>
    /// OutfitData로 직접 옷 변경
    /// </summary>
    public void ChangeClothing(OutfitData outfitData)
    {
        Debug.Log($"옷 변경: {outfitData.outfitName}");
        
        // 기존 모델 제거 (회전 상태 초기화 포함)
        if (currentModelObject != null)
        {
            CleanupCurrentModel();
        }
        
        currentOutfit = outfitData;
        
        // 의류 타입에 따른 카메라 위치 조정
        if (cameraController != null)
        {
            cameraController.SetCameraForClothingType(outfitData.type);
        }
        
        // 3D 모델 로드 및 표시
        LoadAndDisplayModel();
    }
    
    
    /// <summary>
    /// 3D 모델 로드 및 표시
    /// </summary>
    private void LoadAndDisplayModel()
    {
        if (currentOutfit == null) return;
        
        // 카테고리 기반으로 모델 경로 생성
        string modelPath = currentOutfit.GetModelPath();
        
        // 3D 모델 로드
        GameObject modelPrefab = Resources.Load<GameObject>(modelPath);
        if (modelPrefab == null)
        {
            Debug.LogError($"3D 모델을 찾을 수 없습니다: {modelPath} (카테고리: {currentOutfit.type}.{(currentOutfit.type == ClothingType.Top ? currentOutfit.topCategory.ToString() : currentOutfit.bottomCategory.ToString())})");
            return;
        }
        
        // 모델 인스턴스 생성
        currentModelObject = Instantiate(modelPrefab, modelParent);
        currentModelObject.transform.localPosition = Vector3.zero;
        currentModelObject.transform.localRotation = Quaternion.Euler(modelRotation);
        currentModelObject.transform.localScale = modelScale;
        
        // 렌더러 설정
        currentRenderer = currentModelObject.GetComponentInChildren<Renderer>();
        
        // Cloth_Texture Material 적용
        ApplyClothMaterial();
        
        // 회전 기능 설정
        SetupRotationFeature();
        
        // 텍스처 적용
        ApplyTexture();
        
        Debug.Log($"옷 모델 표시 완료: {currentOutfit.outfitName}");
    }
    
    /// <summary>
    /// Cloth_Texture Material을 모델에 적용
    /// </summary>
    private void ApplyClothMaterial()
    {
        if (currentRenderer == null) return;
        
        if (clothMaterial != null)
        {
            // Material 적용
            currentRenderer.material = clothMaterial;
            Debug.Log("Cloth_Texture Material 적용 완료");
        }
        else
        {
            Debug.LogWarning("Cloth_Texture Material이 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 현재 모델 정리 (회전 상태 초기화 포함)
    /// </summary>
    private void CleanupCurrentModel()
    {
        if (currentModelObject == null) return;
        
        // ClothRotator 컴포넌트 정리 (회전 상태 초기화)
        ClothRotator rotator = currentModelObject.GetComponent<ClothRotator>();
        if (rotator != null)
        {
            rotator.ResetRotation();
        }

        
        // 모델 오브젝트 제거
        DestroyImmediate(currentModelObject);
        currentModelObject = null;
        currentRenderer = null;
        
        Debug.Log("기존 모델 정리 완료");
    }
    
    /// <summary>
    /// 회전 기능 설정 (AvatarRotator 컴포넌트 및 Collider 추가)
    /// </summary>
    private void SetupRotationFeature()
    {
        if (!enableRotation || currentModelObject == null) return;
        
        ClothRotator rotator = currentModelObject.GetComponent<ClothRotator>();
        if (rotator == null)
        {
            rotator = currentModelObject.AddComponent<ClothRotator>();
        }
        
        // 회전 속도 설정
        rotator.rotationSpeed = rotationSpeed;
        
        // Collider 확인 (메쉬에 미리 설정된 Collider 사용)
        VerifyCollider();
        
        Debug.Log($"회전 기능 설정 완료: {currentOutfit.outfitName}");
    }
    
    /// <summary>
    /// 메쉬에 미리 설정된 Collider 확인
    /// </summary>
    private void VerifyCollider()
    {
        if (currentModelObject == null) return;
        
        // 프리팹에 미리 설정된 Collider 확인
        Collider[] colliders = currentModelObject.GetComponentsInChildren<Collider>();
        
        if (colliders.Length > 0)
        {
            Debug.Log($"Collider 확인 완료: {colliders.Length}개 발견 ({string.Join(", ", System.Array.ConvertAll(colliders, c => c.GetType().Name))})");
            
            // 회전 기능이 활성화된 경우에만 확인 메시지
            if (enableRotation)
            {
                Debug.Log("마우스 클릭으로 회전 가능합니다.");
            }
        }
        else
        {
            Debug.LogWarning($"Collider가 설정되지 않았습니다: {currentOutfit.outfitName}. 메쉬 프리팹에 Collider를 추가해주세요.");
            
            // 자동 추가 옵션이 활성화된 경우에만 런타임 추가
            if (autoAddCollider)
            {
                Debug.Log("자동 Collider 추가를 시도합니다...");
                AddFallbackCollider();
            }
        }
    }
    
    /// <summary>
    /// Fallback용 간단한 BoxCollider 추가 (메쉬에 Collider가 없을 때만 사용)
    /// </summary>
    private void AddFallbackCollider()
    {
        if (currentModelObject == null) return;
        
        BoxCollider boxCollider = currentModelObject.AddComponent<BoxCollider>();
        
        // 렌더러 기반으로 기본 크기 설정
        if (currentRenderer != null)
        {
            Bounds bounds = currentRenderer.bounds;
            Vector3 center = currentModelObject.transform.InverseTransformPoint(bounds.center);
            Vector3 size = bounds.size;
            
            // Transform 스케일 고려
            Vector3 scale = currentModelObject.transform.lossyScale;
            if (scale.x != 0) size.x /= scale.x;
            if (scale.y != 0) size.y /= scale.y;
            if (scale.z != 0) size.z /= scale.z;
            
            boxCollider.center = center;
            boxCollider.size = size;
        }
        
        Debug.Log("Fallback BoxCollider 추가 완료");
    }
    
    /// <summary>
    /// 텍스처 적용 (URL 또는 로컬 Resources 지원)
    /// </summary>
    private void ApplyTexture()
    {
        if (currentRenderer == null || string.IsNullOrEmpty(currentOutfit.texturePath)) return;
        
        // URL인지 로컬 경로인지 판단
        if (currentOutfit.texturePath.StartsWith("http://") || currentOutfit.texturePath.StartsWith("https://"))
        {
            // 서버에서 텍스처 다운로드
            StartCoroutine(LoadTextureFromUrl(currentOutfit.texturePath));
        }
        else
        {
            // 로컬 Resources에서 텍스처 로드
            LoadTextureFromResources(currentOutfit.texturePath);
        }
    }
    
    /// <summary>
    /// Cloth_Texture Material의 Base Map에 텍스처 적용
    /// </summary>
    private void ApplyTextureToMaterial(Texture2D texture)
    {
        if (texture == null || currentRenderer == null) return;
        
        // Cloth_Texture Material의 Base Map 설정
        if (clothMaterial != null)
        {
            // Material의 Base Map (mainTexture) 변경
            clothMaterial.mainTexture = texture;
            
            // URP의 경우 "_BaseMap" 프로퍼티도 설정
            if (clothMaterial.HasProperty("_BaseMap"))
            {
                clothMaterial.SetTexture("_BaseMap", texture);
            }
            
            Debug.Log($"Cloth_Texture Material Base Map 적용 완료: {texture.name}");
        }
        else
        {
            // Fallback: 렌더러의 material에 직접 적용
            currentRenderer.material.mainTexture = texture;
            Debug.Log($"렌더러에 직접 텍스처 적용: {texture.name}");
        }
    }
    
    /// <summary>
    /// 로컬 Resources에서 텍스처 로드
    /// </summary>
    private void LoadTextureFromResources(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            ApplyTextureToMaterial(texture);
            Debug.Log($"로컬 텍스처 적용 완료: {resourcePath}");
        }
        else
        {
            Debug.LogWarning($"로컬 텍스처를 찾을 수 없습니다: {resourcePath}");
        }
    }
    
    /// <summary>
    /// 서버 URL에서 텍스처 다운로드 및 적용 (캐싱 지원)
    /// </summary>
    private System.Collections.IEnumerator LoadTextureFromUrl(string url)
    {
        // 캐시에 이미 있는지 확인
        if (textureCache.ContainsKey(url))
        {
            Debug.Log($"캐시된 텍스처 사용: {url}");
            ApplyTextureToMaterial(textureCache[url]);
            
            // 웹에 텍스처 로드 완료 알림
            if (WebGLCommunicator.Instance != null)
            {
                WebGLCommunicator.Instance.SendToWeb($"textureLoaded:{currentOutfit.name}");
            }
            yield break;
        }
        
        Debug.Log($"서버에서 텍스처 다운로드 시작: {url}");
        
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            // OutfitDataLoader에서 토큰 가져와서 헤더에 추가
            if (OutfitDataLoader.Instance != null)
            {
                string token = OutfitDataLoader.Instance.GetCurrentToken();
                if (!string.IsNullOrEmpty(token))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + token);
                    Debug.Log("텍스처 요청에 Authorization 헤더 추가됨");
                }
            }
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
                
                if (currentRenderer != null && downloadedTexture != null)
                {
                    // 캐시에 저장
                    textureCache[url] = downloadedTexture;
                    
                    ApplyTextureToMaterial(downloadedTexture);
                    Debug.Log($"서버 텍스처 적용 및 캐시 저장 완료: {url}");
                    
                    // 웹에 텍스처 로드 완료 알림
                    if (WebGLCommunicator.Instance != null)
                    {
                        WebGLCommunicator.Instance.SendToWeb($"textureLoaded:{currentOutfit.name}");
                    }
                }
                else
                {
                    Debug.LogError("렌더러 또는 텍스처가 null입니다.");
                    
                    if (WebGLCommunicator.Instance != null)
                    {
                        WebGLCommunicator.Instance.SendToWeb($"error:Renderer or texture is null");
                    }
                }
            }
            else
            {
                Debug.LogError($"텍스처 다운로드 실패: {url} - {request.error}");
                
                // 웹에 에러 알림
                if (WebGLCommunicator.Instance != null)
                {
                    WebGLCommunicator.Instance.SendToWeb($"error:Texture download failed - {request.error}");
                }
            }
        }
    }
    
    
    /// <summary>
    /// 현재 표시중인 옷 정보 반환
    /// </summary>
    public string GetCurrentClothingInfo()
    {
        if (currentOutfit != null)
        {
            return $"{currentOutfit.outfitName} ({currentOutfit.name})";
        }
        return "표시중인 옷이 없습니다.";
    }
    
    /// <summary>
    /// 현재 의류 모델의 회전 기능 활성화/비활성화
    /// </summary>
    public void SetRotationEnabled(bool enabled)
    {
        enableRotation = enabled;
        
        if (currentModelObject != null)
        {
            ClothRotator rotator = currentModelObject.GetComponent<ClothRotator>();
            if (rotator != null)
            {
                rotator.enabled = enabled;
            }
        }
        
        Debug.Log($"회전 기능 {(enabled ? "활성화" : "비활성화")}");
    }
    
    /// <summary>
    /// 현재 의류 모델의 회전 속도 변경
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
        
        if (currentModelObject != null)
        {
            ClothRotator rotator = currentModelObject.GetComponent<ClothRotator>();
            if (rotator != null)
            {
                rotator.rotationSpeed = speed;
            }
        }
        
        Debug.Log($"회전 속도 변경: {speed}");
    }
    
    /// <summary>
    /// 현재 의류 모델의 회전 상태 초기화 (원래 회전값으로 복원)
    /// </summary>
    public void ResetRotation()
    {
        if (currentModelObject != null)
        {
            currentModelObject.transform.localRotation = Quaternion.Euler(modelRotation);
            Debug.Log("회전 상태 초기화 완료");
        }
    }
    
}