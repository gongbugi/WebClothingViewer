using UnityEngine;
using System.Runtime.InteropServices;

public class WebGLCommunicator : MonoBehaviour
{
    [Header("옷 모델 컨트롤러")]
    public ClothingModel clothingModel;
    
    [Header("옷 데이터 로더")]
    public OutfitDataLoader outfitDataLoader;
    
    [Header("카메라 설정")]
    public Camera mainCamera;
    public Transform cameraTarget; // 카메라가 바라볼 타겟
    
    public static WebGLCommunicator Instance { get; private set; }
    
    // WebGL에서 JavaScript로 메시지 전송을 위한 외부 함수
    [DllImport("__Internal")]
    private static extern void SendMessageToWeb(string message);
    
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
        // 카메라는 수동으로 조절하므로 자동 설정 비활성화
        // SetupCamera();
        
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
    /// 웹에서 호출할 모델 숨기기 메서드
    /// </summary>
    public void HideModelFromWeb()
    {
        if (clothingModel != null)
        {
            clothingModel.HideModel();
            SendToWeb("modelHidden");
        }
    }
    
    /// <summary>
    /// 웹에서 호출할 모델 보이기 메서드
    /// </summary>
    public void ShowModelFromWeb()
    {
        if (clothingModel != null)
        {
            clothingModel.ShowModel();
            SendToWeb("modelShown");
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
    /// 카메라 설정
    /// </summary>
    private void SetupCamera()
    {
        if (mainCamera != null && cameraTarget != null)
        {
            // 카메라가 타겟을 바라보도록 설정
            mainCamera.transform.LookAt(cameraTarget);
            
            // 적절한 거리로 카메라 위치 조정
            Vector3 direction = (mainCamera.transform.position - cameraTarget.position).normalized;
            mainCamera.transform.position = cameraTarget.position + direction * 3f;
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
            SendMessageToWeb(message);
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
    /// 카메라 각도 조정 (웹에서 호출 가능)
    /// </summary>
    public void RotateCamera(float x, float y)
    {
        if (mainCamera != null && cameraTarget != null)
        {
            // 타겟 주변으로 카메라 회전
            mainCamera.transform.RotateAround(cameraTarget.position, Vector3.up, x);
            mainCamera.transform.RotateAround(cameraTarget.position, mainCamera.transform.right, y);
            
            SendToWeb($"cameraRotated:{x},{y}");
        }
    }
    
    /// <summary>
    /// 카메라 줌 조정 (웹에서 호출 가능)
    /// </summary>
    public void ZoomCamera(float delta)
    {
        if (mainCamera != null && cameraTarget != null)
        {
            Vector3 direction = (mainCamera.transform.position - cameraTarget.position).normalized;
            float newDistance = Vector3.Distance(mainCamera.transform.position, cameraTarget.position) - delta;
            
            // 줌 범위 제한
            newDistance = Mathf.Clamp(newDistance, 1f, 10f);
            
            mainCamera.transform.position = cameraTarget.position + direction * newDistance;
            
            SendToWeb($"cameraZoomed:{newDistance}");
        }
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