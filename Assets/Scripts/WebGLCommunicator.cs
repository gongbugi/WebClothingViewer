using UnityEngine;

public class WebGLCommunicator : MonoBehaviour
{
    [Header("옷 모델 컨트롤러")]
    public ClothingModel clothingModel;
    
    [Header("옷 데이터 로더")]
    public OutfitDataLoader outfitDataLoader;
    
    
    public static WebGLCommunicator Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 웹에 Unity 준비 완료 신호 먼저 전송
        SendToWeb("unityReady");
        
        // 옷 데이터 로드 시작
        StartCoroutine(InitializeOutfitData());
    }
    
    private System.Collections.IEnumerator InitializeOutfitData()
    {
        // OutfitDataLoader가 설정되어 있다면 데이터 로드
        if (outfitDataLoader != null)
        {
            Debug.Log("옷 데이터 로드 시작...");
            outfitDataLoader.LoadOutfits();
            
            // 데이터 로드 완료까지 대기 (최대 30초)
            float timeout = 30f;
            float elapsed = 0f;
            
            while (!outfitDataLoader.IsDataLoaded() && elapsed < timeout)
            {
                yield return new UnityEngine.WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (outfitDataLoader.IsDataLoaded())
            {
                int outfitCount = outfitDataLoader.GetLoadedOutfits().Count;
                Debug.Log($"옷 데이터 로드 완료: {outfitCount}개");
                SendToWeb($"outfitsLoaded:{outfitCount}");
            }
            else
            {
                Debug.LogError("옷 데이터 로드 시간 초과!");
                SendToWeb("error:Outfit data load timeout");
            }
        }
        else
        {
            Debug.LogWarning("OutfitDataLoader가 설정되지 않았습니다.");
            SendToWeb("warning:No OutfitDataLoader configured");
        }
        
        // 웹에 Unity 준비 완료 신호 전송
        SendToWeb("unityReady");
    }
    
    /// <summary>
    /// 웹에서 호출할 옷 변경 메서드 (JavaScript에서 직접 호출)
    /// </summary>
    [ContextMenu("Test Change Tshirt")]
    private void TestChangeTshirt() => ChangeClothingFromWeb("tshirt_001");
    
    [ContextMenu("Test Change Shirt")]  
    private void TestChangeShirt() => ChangeClothingFromWeb("shirt_001");
    
    [ContextMenu("Test Change Pants")]
    private void TestChangePants() => ChangeClothingFromWeb("pants_001");
    
    [ContextMenu("Test Change Shorts")]
    private void TestChangeShorts() => ChangeClothingFromWeb("shorts_001");
    
    [ContextMenu("Test Get Current Info")]
    private void TestGetCurrentInfo() => GetCurrentClothingInfo();
    
    [ContextMenu("Test Get Available Outfits")]
    private void TestGetAvailableOutfits() => GetAvailableOutfits();
    
    public void ChangeClothingFromWeb(string outfitId)
    {
        Debug.Log($"웹에서 옷 변경 요청 받음: {outfitId}");
        
        // 기본 유효성 검사
        if (clothingModel == null)
        {
            Debug.LogError("ClothingModel이 설정되지 않았습니다!");
            SendToWeb("error:ClothingModel not found");
            return;
        }
        
        if (outfitDataLoader == null || !outfitDataLoader.IsDataLoaded())
        {
            Debug.LogError("OutfitDataLoader가 초기화되지 않았거나 데이터가 로드되지 않았습니다!");
            SendToWeb("error:OutfitData not loaded");
            return;
        }
        
        // 옷 데이터 찾기
        OutfitData outfit = outfitDataLoader.GetOutfitById(outfitId);
        if (outfit == null)
        {
            Debug.LogWarning($"ID '{outfitId}'에 해당하는 옷을 찾을 수 없습니다.");
            SendToWeb($"error:Outfit not found - {outfitId}");
            return;
        }
        
        // 옷 변경 실행
        try
        {
            clothingModel.ChangeClothing(outfit);
            SendToWeb($"clothingChanged:{outfitId}");
            Debug.Log($"옷 변경 성공: {outfit.outfitName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"옷 변경 중 오류 발생: {e.Message}");
            SendToWeb($"error:Failed to change clothing - {e.Message}");
        }
    }
    
    
    /// <summary>
    /// 웹에서 호출할 현재 옷 정보 요청 메서드
    /// </summary>
    public void GetCurrentClothingInfo()
    {
        if (clothingModel != null)
        {
            string info = clothingModel.GetCurrentClothingInfo();
            SendToWeb($"currentClothingInfo:{info}");
        }
    }
    
    
    /// <summary>
    /// 웹으로 메시지 전송 (외부에서도 호출 가능)
    /// </summary>
    public void SendToWeb(string message)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            // Unity 6의 WebGL에서 JavaScript 함수 호출
            string jsCode = $"if(window.receiveUnityMessage) window.receiveUnityMessage('{message}');";
            Application.ExternalEval(jsCode);
            Debug.Log($"웹으로 메시지 전송: {message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"웹 메시지 전송 실패: {e.Message}");
        }
        #else
        Debug.Log($"[에디터 모드] 웹으로 전송할 메시지: {message}");
        #endif
    }
    
    
    
    /// <summary>
    /// 웹에서 사용 가능한 옷 목록 요청 (JavaScript에서 호출 가능)
    /// </summary>
    public void GetAvailableOutfits()
    {
        if (outfitDataLoader == null || !outfitDataLoader.IsDataLoaded())
        {
            SendToWeb("error:Outfit data not available");
            return;
        }
        
        var outfits = outfitDataLoader.GetLoadedOutfits();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("availableOutfits:");
        
        for (int i = 0; i < outfits.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($"{outfits[i].name}|{outfits[i].outfitName}|{outfits[i].type}");
        }
        
        SendToWeb(sb.ToString());
    }
    
    /// <summary>
    /// 서버 URL 변경 (런타임에 서버 주소 변경 가능)
    /// </summary>
    public void SetServerUrl(string url)
    {
        if (outfitDataLoader != null)
        {
            outfitDataLoader.serverUrl = url;
            Debug.Log($"서버 URL 변경됨: {url}");
            SendToWeb($"serverUrlChanged:{url}");
        }
        else
        {
            SendToWeb("error:OutfitDataLoader not found");
        }
    }
    
    /// <summary>
    /// 서버에서 옷 데이터 재로드
    /// </summary>
    public void ReloadOutfitsFromServer()
    {
        if (outfitDataLoader != null)
        {
            outfitDataLoader.useLocalFile = false; // 서버 모드로 변경
            StartCoroutine(ReloadOutfitsCoroutine());
        }
        else
        {
            SendToWeb("error:OutfitDataLoader not found");
        }
    }
    
    private System.Collections.IEnumerator ReloadOutfitsCoroutine()
    {
        Debug.Log("서버에서 옷 데이터 재로드 시작...");
        SendToWeb("outfitsReloading");
        
        outfitDataLoader.LoadOutfits();
        
        // 로드 완료 대기
        float timeout = 30f;
        float elapsed = 0f;
        
        while (!outfitDataLoader.IsDataLoaded() && elapsed < timeout)
        {
            yield return new UnityEngine.WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (outfitDataLoader.IsDataLoaded())
        {
            int outfitCount = outfitDataLoader.GetLoadedOutfits().Count;
            Debug.Log($"서버에서 옷 데이터 재로드 완료: {outfitCount}개");
            SendToWeb($"outfitsReloaded:{outfitCount}");
        }
        else
        {
            Debug.LogError("서버 옷 데이터 재로드 실패!");
            SendToWeb("error:Server reload failed");
        }
    }
}