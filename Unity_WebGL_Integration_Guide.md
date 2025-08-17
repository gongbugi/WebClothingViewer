# Unity WebGL 의류 뷰어 연동 가이드 📋

---

## 📦 **전달받을 파일들**

Unity WebGL 빌드 후 다음 파일들을 받게 됩니다:

```
Build/
├── unity-build.data
├── unity-build.framework.js
├── unity-build.wasm
├── unity-build.loader.js
└── index.html (참고용)
```

---

## 🌐 **1. 기본 HTML 구조**

```html
<!DOCTYPE html>
<html lang="ko">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>의류 3D 뷰어</title>
    <style>
        #unity-container {
            width: 800px;
            height: 600px;
            margin: 0 auto;
        }
        #unity-canvas {
            width: 100%;
            height: 100%;
        }
    </style>
</head>
<body>
    <!-- Unity WebGL이 로드될 컨테이너 -->
    <div id="unity-container">
        <canvas id="unity-canvas"></canvas>
        <div id="unity-loading-bar">
            <div id="unity-logo"></div>
            <div id="unity-progress-bar-empty">
                <div id="unity-progress-bar-full"></div>
            </div>
        </div>
    </div>

    <!-- 의류 선택 버튼들 -->
    <div id="controls">
        <button onclick="changeClothing('tshirt_001')">티셔츠</button>
        <button onclick="changeClothing('shirt_001')">셔츠</button>
        <button onclick="changeClothing('pants_001')">바지</button>
        <button onclick="changeClothing('shorts_001')">반바지</button>
    </div>

    <!-- Unity WebGL 로더 스크립트 -->
    <script src="Build/unity-build.loader.js"></script>
    <script>
        // 여기에 JavaScript 코드 작성 (아래 참고)
    </script>
</body>
</html>
```

---

## 🔧 **2. JavaScript 초기화 코드**

```javascript
let unityInstance = null;
let isUnityReady = false;

// Unity 인스턴스 생성
createUnityInstance(document.querySelector("#unity-canvas"), {
    dataUrl: "Build/unity-build.data",
    frameworkUrl: "Build/unity-build.framework.js",
    codeUrl: "Build/unity-build.wasm",
}, (progress) => {
    // 로딩 진행률 표시
    document.querySelector("#unity-progress-bar-full").style.width = 100 * progress + "%";
}).then((instance) => {
    // Unity 로드 완료
    unityInstance = instance;
    document.querySelector("#unity-loading-bar").style.display = "none";
    console.log("Unity 인스턴스 생성 완료");
});

// Unity에서 오는 메시지 수신 함수 (필수!)
window.receiveUnityMessage = function(message) {
    console.log('Unity 메시지:', message);
    
    if (message === 'unityReady') {
        isUnityReady = true;
        console.log('Unity 준비 완료 - 이제 API 호출 가능');
    } else if (message.startsWith('outfitsLoaded:')) {
        const count = message.split(':')[1];
        console.log(`의류 데이터 로드 완료: ${count}개`);
    } else if (message.startsWith('clothingChanged:')) {
        const outfitId = message.split(':')[1];
        console.log(`의류 변경 완료: ${outfitId}`);
    } else if (message.startsWith('error:')) {
        const error = message.split(':')[1];
        console.error(`Unity 오류: ${error}`);
    }
};
```

---

## 📡 **3. API 호출 함수들**

### **의류 변경**
```javascript
function changeClothing(outfitId) {
    if (!unityInstance || !isUnityReady) {
        console.warn('Unity가 아직 준비되지 않았습니다');
        return;
    }
    
    unityInstance.SendMessage('WebGLManager', 'ChangeClothingFromWeb', outfitId);
}

// 사용 예시
changeClothing('tshirt_001');  // 티셔츠로 변경
changeClothing('shirt_001');   // 셔츠로 변경
changeClothing('pants_001');   // 바지로 변경
changeClothing('shorts_001');  // 반바지로 변경
```

### **현재 의류 정보 조회**
```javascript
function getCurrentClothingInfo() {
    if (!unityInstance || !isUnityReady) return;
    
    unityInstance.SendMessage('WebGLManager', 'GetCurrentClothingInfo', '');
}

// Unity에서 'currentClothingInfo:정보' 메시지로 응답
```

### **사용 가능한 의류 목록 조회**
```javascript
function getAvailableOutfits() {
    if (!unityInstance || !isUnityReady) return;
    
    unityInstance.SendMessage('WebGLManager', 'GetAvailableOutfits', '');
}

// Unity에서 'availableOutfits:데이터' 메시지로 응답
```

### **서버 API URL 설정**
```javascript
function setServerUrl(apiUrl) {
    if (!unityInstance || !isUnityReady) return;
    
    unityInstance.SendMessage('WebGLManager', 'SetServerUrl', apiUrl);
}

// 사용 예시
setServerUrl('https://your-api.com/outfits');
```

### **서버에서 의류 데이터 재로드**
```javascript
function reloadOutfitsFromServer() {
    if (!unityInstance || !isUnityReady) return;
    
    unityInstance.SendMessage('WebGLManager', 'ReloadOutfitsFromServer', '');
}
```

---

## 🔄 **4. Unity ↔ 웹 통신 흐름**

### **Unity → 웹 메시지들**
| 메시지 | 설명 | 예시 |
|--------|------|------|
| `unityReady` | Unity 초기화 완료 | Unity 로드 후 API 호출 가능 |
| `outfitsLoaded:5` | 의류 데이터 로드 완료 | 5개 의류 로드됨 |
| `clothingChanged:tshirt_001` | 의류 변경 완료 | 티셔츠로 변경됨 |
| `currentClothingInfo:기본 티셔츠` | 현재 의류 정보 | 현재 입고 있는 의류 |
| `availableOutfits:데이터` | 사용 가능한 의류 목록 | 전체 의류 리스트 |
| `error:메시지` | 오류 발생 | 에러 처리 필요 |

### **웹 → Unity 호출들**
| 메서드 | 파라미터 | 설명 |
|--------|----------|------|
| `ChangeClothingFromWeb` | `outfitId` | 의류 변경 |
| `GetCurrentClothingInfo` | `''` | 현재 의류 정보 요청 |
| `GetAvailableOutfits` | `''` | 의류 목록 요청 |
| `SetServerUrl` | `url` | API 서버 주소 설정 |
| `ReloadOutfitsFromServer` | `''` | 서버에서 데이터 재로드 |

---

## 🎯 **5. 실제 사용 예시**

### **React 컴포넌트 예시**
```jsx
import { useEffect, useState } from 'react';

function ClothingViewer() {
    const [unityInstance, setUnityInstance] = useState(null);
    const [isReady, setIsReady] = useState(false);
    const [currentClothing, setCurrentClothing] = useState('');

    useEffect(() => {
        // Unity 메시지 수신
        window.receiveUnityMessage = (message) => {
            if (message === 'unityReady') {
                setIsReady(true);
            } else if (message.startsWith('clothingChanged:')) {
                const outfitId = message.split(':')[1];
                setCurrentClothing(outfitId);
            }
        };

        // Unity 인스턴스 생성
        createUnityInstance(document.querySelector("#unity-canvas"), {
            dataUrl: "Build/unity-build.data",
            frameworkUrl: "Build/unity-build.framework.js",
            codeUrl: "Build/unity-build.wasm",
        }).then(setUnityInstance);
    }, []);

    const changeClothing = (outfitId) => {
        if (unityInstance && isReady) {
            unityInstance.SendMessage('WebGLManager', 'ChangeClothingFromWeb', outfitId);
        }
    };

    return (
        <div>
            <canvas id="unity-canvas" width="800" height="600"></canvas>
            
            <div>
                <button onClick={() => changeClothing('tshirt_001')}>티셔츠</button>
                <button onClick={() => changeClothing('shirt_001')}>셔츠</button>
                <button onClick={() => changeClothing('pants_001')}>바지</button>
                <button onClick={() => changeClothing('shorts_001')}>반바지</button>
            </div>
            
            <div>현재 의류: {currentClothing}</div>
            <div>Unity 상태: {isReady ? '준비됨' : '로딩중...'}</div>
        </div>
    );
}
```

---

## 🗄️ **6. 백엔드 API 명세**

Unity가 요청할 JSON API 형식:

### **엔드포인트**: `GET /api/outfits`

### **응답 형식**:
```json
{
  "outfits": [
    {
      "id": "tshirt_001",
      "outfitName": "기본 티셔츠",
      "type": "Top",
      "topCategory": "Tshirt",
      "texturePath": "https://your-cdn.com/textures/tshirt_001.jpg",
      "thumbnailUrl": "https://your-cdn.com/thumbnails/tshirt1.jpg"
    },
    {
      "id": "shirt_001",
      "outfitName": "셔츠",
      "type": "Top",
      "topCategory": "Shirt",
      "texturePath": "https://your-cdn.com/textures/shirt_001.jpg",
      "thumbnailUrl": "https://your-cdn.com/thumbnails/shirt1.jpg"
    }
  ]
}
```

### **필수 CORS 설정**:
```
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Content-Type
```

---

## ⚠️ **7. 주의사항**

### **필수 체크리스트**
- [ ] `window.receiveUnityMessage` 함수가 정의되어 있는가?
- [ ] Unity 로드 완료 후 API 호출하는가?
- [ ] CORS 설정이 되어 있는가?
- [ ] 텍스처 이미지 URL이 접근 가능한가?

### **디버깅 방법**
```javascript
// 콘솔에서 디버깅
console.log('Unity Instance:', unityInstance);
console.log('Unity Ready:', isUnityReady);

// Unity 상태 확인
if (unityInstance) {
    unityInstance.SendMessage('WebGLManager', 'GetCurrentClothingInfo', '');
}
```

### **자주 발생하는 문제들**
1. **CORS 오류**: 서버에서 CORS 헤더 설정 필요
2. **Unity 미준비**: `unityReady` 메시지 받기 전 API 호출
3. **경로 오류**: Build 폴더 경로 확인
4. **텍스처 로드 실패**: 이미지 URL 접근성 확인

---

## 🚀 **8. 배포 체크리스트**

### **파일 업로드**
- [ ] `Build/` 폴더 전체 업로드
- [ ] 웹서버에서 `.wasm` 파일 MIME 타입 설정: `application/wasm`
- [ ] HTTPS 환경에서 테스트 (WebGL 권장)

### **테스트 시나리오**
1. Unity 로드 확인
2. 의류 변경 버튼 클릭 테스트
3. 서버 API 연결 테스트
4. 다양한 브라우저에서 테스트

---

이 가이드대로 구현하면 Unity WebGL 의류 뷰어가 웹사이트에 완벽하게 통합됩니다! 🎉
