# WebGLCommunicator 씬 설정 가이드

## 1. 기본 오브젝트 구조 생성

### Hierarchy 구조 (권장)
```
Scene
├── WebGLManager (빈 오브젝트)
│   ├── WebGLCommunicator (스크립트)
│   └── OutfitDataLoader (스크립트)
├── ClothingDisplay (빈 오브젝트)
│   ├── ClothingModel (스크립트)
│   └── ModelParent (빈 오브젝트) - 3D 모델들이 생성될 부모
├── CameraRig (빈 오브젝트)
│   ├── Main Camera
│   └── CameraTarget (빈 오브젝트) - 카메라가 바라볼 지점
└── Lighting/Environment (기본 조명 설정)
```

## 2. 단계별 설정 방법

### Step 1: WebGLManager 오브젝트 생성
1. Hierarchy에서 우클릭 → Create Empty
2. 이름을 "WebGLManager"로 변경
3. WebGLCommunicator 스크립트 추가
4. OutfitDataLoader 스크립트도 같은 오브젝트에 추가

### Step 2: ClothingDisplay 오브젝트 생성
1. 새 빈 오브젝트 생성 → "ClothingDisplay"로 명명
2. ClothingModel 스크립트 추가
3. 자식 오브젝트로 "ModelParent" 빈 오브젝트 생성

### Step 3: 카메라 설정
1. Main Camera 위치 조정 (예: Position(0, 1, 3), Rotation(0, 0, 0))
2. "CameraTarget" 빈 오브젝트 생성 (Position(0, 1, 0))

### Step 4: 스크립트 참조 연결

#### WebGLCommunicator 설정:
- **Clothing Model**: ClothingDisplay 오브젝트 드래그
- **Outfit Data Loader**: WebGLManager 오브젝트 드래그 (자기 자신)
- **Main Camera**: Main Camera 오브젝트 드래그
- **Camera Target**: CameraTarget 오브젝트 드래그

#### ClothingModel 설정:
- **Model Parent**: ModelParent 오브젝트 드래그
- **Model Rotation**: (0, 0, 0) 또는 원하는 회전값
- **Model Scale**: (1, 1, 1) 또는 원하는 스케일

#### OutfitDataLoader 설정:
- **Json File Name**: "outfits.json"
- **Use Local File**: ✓ (체크) - 로컬 테스트용
- **Server Url**: "https://yourserver.com/api/outfits" (서버 연동시)

## 3. WebGL 빌드 및 테스트

### 빌드 설정
1. File → Build Settings
2. Platform을 WebGL로 변경
3. Add Open Scenes로 현재 씬 추가
4. Build 실행

### 웹 페이지 연동
```html
<!DOCTYPE html>
<html>
<head>
    <title>Clothing Viewer</title>
</head>
<body>
    <div id="unity-container">
        <!-- Unity WebGL이 여기에 로드됩니다 -->
    </div>
    
    <div id="clothing-buttons">
        <button onclick="changeClothing('tshirt_001')">티셔츠</button>
        <button onclick="changeClothing('shorts_001')">반바지</button>
    </div>

    <script>
        // Unity가 로드된 후 호출할 함수
        function changeClothing(outfitId) {
            // WebGLManager는 WebGLCommunicator가 붙은 GameObject 이름
            SendMessage('WebGLManager', 'ChangeClothingFromWeb', outfitId);
        }
        
        // Unity에서 메시지 받기
        window.addEventListener('message', function(event) {
            console.log('Unity에서 메시지:', event.data);
            
            if (event.data === 'unityReady') {
                console.log('Unity 준비 완료!');
                // 옷 목록 요청
                SendMessage('WebGLManager', 'GetAvailableOutfits', '');
            }
            
            if (event.data.startsWith('clothingChanged:')) {
                const outfitId = event.data.split(':')[1];
                console.log('옷 변경 완료:', outfitId);
            }
        });
    </script>
</body>
</html>
```

## 4. 디버깅 및 테스트

### Unity 에디터에서 테스트
1. Play 모드로 진입
2. Console 창에서 "옷 데이터 로드 완료" 메시지 확인
3. WebGLCommunicator의 ChangeClothingFromWeb 메서드를 직접 호출해서 테스트

### 브라우저에서 테스트
1. 브라우저 개발자 도구 → Console 탭
2. Unity 로그 메시지 확인
3. JavaScript에서 SendMessage 함수 직접 호출:
   ```javascript
   SendMessage('WebGLManager', 'ChangeClothingFromWeb', 'tshirt_001');
   ```

## 5. 문제 해결

### 일반적인 문제들:
1. **"OutfitDataLoader not found" 에러**
   → WebGLCommunicator의 Outfit Data Loader 참조 확인

2. **"ClothingModel not found" 에러** 
   → WebGLCommunicator의 Clothing Model 참조 확인

3. **"3D 모델을 찾을 수 없습니다" 에러**
   → Resources 폴더에 Tshirt.prefab, Shorts.prefab 등이 있는지 확인

4. **카메라가 움직이지 않음**
   → Camera Target이 올바르게 설정되었는지 확인

### 로그 확인 방법:
- Unity 에디터: Console 창
- WebGL 빌드: 브라우저 개발자 도구 Console 탭