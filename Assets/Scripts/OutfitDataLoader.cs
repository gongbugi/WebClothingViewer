using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class OutfitJsonData
{
    public string id;
    public string outfitName;
    public string type; // "Top" or "Bottom"
    public string topCategory; // "Tshirt" or "Shirt"
    public string bottomCategory; // "Pants" or "Shorts"
    public string texturePath; // 텍스처 경로
    public string thumbnailUrl;
}

[Serializable]
public class OutfitListWrapper
{
    public List<OutfitJsonData> outfits;
}

public class OutfitDataLoader : MonoBehaviour
{
    [Header("설정")]
    public string jsonFileName = "outfits.json";
    public bool useLocalFile = false; // 서버에서 데이터 가져오기
    public string serverUrl = "http://localhost:3000/api/outfits"; // 서버 URL

    public static OutfitDataLoader Instance { get; private set; }
    
    private List<OutfitData> loadedOutfits = new List<OutfitData>();
    private Dictionary<string, Sprite> thumbnailCache = new Dictionary<string, Sprite>();

    public event Action<List<OutfitData>> OnOutfitsLoaded;

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

    [ContextMenu("Test Load Outfits")]
    public void LoadOutfits()
    {
        if (useLocalFile)
        {
            StartCoroutine(LoadOutfitsFromLocalFile());
        }
        else
        {
            StartCoroutine(LoadOutfitsFromServer());
        }
    }

    private IEnumerator LoadOutfitsFromLocalFile()
    {
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, jsonFileName);
        
        UnityWebRequest request = UnityWebRequest.Get(filePath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonContent = request.downloadHandler.text;
            ProcessJsonData(jsonContent);
        }
        else
        {
            Debug.LogError($"로컬 JSON 파일 로드 실패: {request.error}");
            Debug.LogError($"파일 경로: {filePath}");
        }

        request.Dispose();
    }

    private IEnumerator LoadOutfitsFromServer()
    {
        UnityWebRequest request = UnityWebRequest.Get(serverUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonContent = request.downloadHandler.text;
            ProcessJsonData(jsonContent);
        }
        else
        {
            Debug.LogError($"서버에서 JSON 로드 실패: {request.error}");
        }

        request.Dispose();
    }

    private void ProcessJsonData(string jsonContent)
    {
        try
        {
            OutfitListWrapper wrapper = JsonUtility.FromJson<OutfitListWrapper>(jsonContent);
            loadedOutfits.Clear();

            foreach (OutfitJsonData jsonData in wrapper.outfits)
            {
                OutfitData outfitData = ConvertJsonToOutfitData(jsonData);
                loadedOutfits.Add(outfitData);
            }

            Debug.Log($"JSON에서 {loadedOutfits.Count}개의 의상 로드 완료");
            OnOutfitsLoaded?.Invoke(loadedOutfits);
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON 파싱 오류: {e.Message}");
        }
    }

    private OutfitData ConvertJsonToOutfitData(OutfitJsonData jsonData)
    {
        OutfitData outfitData = ScriptableObject.CreateInstance<OutfitData>();
        
        // ID를 ScriptableObject name으로 설정 (검색용)
        outfitData.name = jsonData.id;
        outfitData.outfitName = jsonData.outfitName;
        outfitData.texturePath = jsonData.texturePath;
        
        // ClothingType enum 변환
        if (Enum.TryParse<ClothingType>(jsonData.type, out ClothingType clothingType))
        {
            outfitData.type = clothingType;
        }
        
        // TopCategory enum 변환
        if (!string.IsNullOrEmpty(jsonData.topCategory) && 
            Enum.TryParse<TopCategory>(jsonData.topCategory, out TopCategory topCategory))
        {
            outfitData.topCategory = topCategory;
        }
        
        // BottomCategory enum 변환
        if (!string.IsNullOrEmpty(jsonData.bottomCategory) && 
            Enum.TryParse<BottomCategory>(jsonData.bottomCategory, out BottomCategory bottomCategory))
        {
            outfitData.bottomCategory = bottomCategory;
        }

        // 썸네일 이미지 로드 (비동기)
        StartCoroutine(LoadThumbnail(jsonData.thumbnailUrl, outfitData));

        return outfitData;
    }

    private IEnumerator LoadThumbnail(string url, OutfitData outfitData)
    {
        // 이미 캐시된 이미지가 있는지 확인
        if (thumbnailCache.ContainsKey(url))
        {
            outfitData.thumbnail = thumbnailCache[url];
            yield break;
        }

        // URL이 실제 이미지 URL이 아닌 경우 기본 처리
        if (string.IsNullOrEmpty(url) || url.StartsWith("https://example.com"))
        {
            // 임시로 기존 Resources의 이미지를 사용하거나 기본 이미지 생성
            outfitData.thumbnail = CreateDefaultThumbnail();
            yield break;
        }

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            
            outfitData.thumbnail = sprite;
            thumbnailCache[url] = sprite;
        }
        else
        {
            Debug.LogWarning($"썸네일 로드 실패: {url} - {request.error}");
            outfitData.thumbnail = CreateDefaultThumbnail();
        }

        request.Dispose();
    }

    private Sprite CreateDefaultThumbnail()
    {
        // 기본 썸네일 생성 (단색 정사각형)
        Texture2D texture = new Texture2D(128, 128);
        Color[] pixels = new Color[128 * 128];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0.7f, 0.7f, 0.7f, 1f); // 회색
        }
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 128, 128), Vector2.one * 0.5f);
    }

    public List<OutfitData> GetLoadedOutfits()
    {
        return loadedOutfits;
    }

    /// <summary>
    /// ID로 특정 옷 데이터 찾기
    /// </summary>
    public OutfitData GetOutfitById(string id)
    {
        foreach (OutfitData outfit in loadedOutfits)
        {
            if (outfit.name == id) // ScriptableObject의 name이 id로 설정됨
            {
                return outfit;
            }
        }
        
        Debug.LogWarning($"ID '{id}'에 해당하는 옷을 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// 데이터 로딩 완료 여부 확인
    /// </summary>
    public bool IsDataLoaded()
    {
        return loadedOutfits.Count > 0;
    }
}