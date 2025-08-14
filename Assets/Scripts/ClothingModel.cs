using UnityEngine;

public class ClothingModel : MonoBehaviour
{
    [Header("현재 표시중인 옷 정보")]
    public OutfitData currentOutfit;
    
    [Header("3D 모델 설정")]
    public Transform modelParent; // 3D 모델이 생성될 부모 Transform
    public Vector3 modelRotation = Vector3.zero;
    public Vector3 modelScale = Vector3.one;
    
    private GameObject currentModelObject;
    private Renderer currentRenderer;
    
    // 텍스처 캐싱을 위한 딕셔너리
    private static System.Collections.Generic.Dictionary<string, Texture2D> textureCache = 
        new System.Collections.Generic.Dictionary<string, Texture2D>();
    
    void Start()
    {
        // 초기 상태에서는 아무 옷도 표시하지 않음
        HideModel();
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
        
        // 기존 모델 제거
        if (currentModelObject != null)
        {
            DestroyImmediate(currentModelObject);
        }
        
        currentOutfit = outfitData;
        
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
        
        // 텍스처 적용
        ApplyTexture();
        
        Debug.Log($"옷 모델 표시 완료: {currentOutfit.outfitName}");
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
    /// 로컬 Resources에서 텍스처 로드
    /// </summary>
    private void LoadTextureFromResources(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            currentRenderer.material.mainTexture = texture;
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
            currentRenderer.material.mainTexture = textureCache[url];
            
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
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
                
                if (currentRenderer != null && downloadedTexture != null)
                {
                    // 캐시에 저장
                    textureCache[url] = downloadedTexture;
                    
                    currentRenderer.material.mainTexture = downloadedTexture;
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
    /// 모델 숨기기
    /// </summary>
    public void HideModel()
    {
        if (currentModelObject != null)
        {
            currentModelObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 모델 보이기
    /// </summary>
    public void ShowModel()
    {
        if (currentModelObject != null)
        {
            currentModelObject.SetActive(true);
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
    
}