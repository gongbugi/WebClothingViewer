# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

WebClothingViewer는 Unity로 개발된 웹 기반 의류 3D 뷰어 애플리케이션입니다. WebGL로 빌드되어 웹 브라우저에서 실행되며, JavaScript와 Unity 간의 통신을 통해 의류 모델을 동적으로 표시하고 조작할 수 있습니다. **서버 기반 동적 로딩 시스템**을 통해 JSON 데이터로부터 옷 정보를 로드합니다.

## 개발 환경

- **Unity 버전**: 6000.1.9f1 (Unity 6.0.1)
- **렌더 파이프라인**: Universal Render Pipeline (URP) 17.1.0
- **플랫폼**: WebGL
- **입력 시스템**: Unity Input System 1.14.0

## 핵심 아키텍처

### 주요 스크립트 구조

1. **OutfitData.cs** (`Assets/Scripts/OutfitData.cs`)
   - ScriptableObject 기반 의류 데이터 구조체
   - 의류 타입, 카테고리, 모델/텍스처 경로 정보 포함

2. **OutfitDataLoader.cs** (`Assets/Scripts/OutfitDataLoader.cs`)
   - JSON 기반 의류 데이터 로더 (로컬/서버 지원)
   - 싱글톤 패턴으로 전역 접근 가능
   - 썸네일 이미지 캐싱 및 비동기 로드

3. **ClothingModel.cs** (`Assets/Scripts/ClothingModel.cs`)
   - OutfitData 기반 3D 모델 표시 시스템
   - Resources 폴더에서 동적으로 모델과 텍스처 로드
   - ID 기반 의류 검색 및 변경 기능
   - **Cloth_Texture.mat 통합 시스템**: 모든 메쉬에 공통 Material 적용 후 Base Map만 변경

4. **WebGLCommunicator.cs** (`Assets/Scripts/WebGLCommunicator.cs`)
   - WebGL과 JavaScript 간의 통신 인터페이스
   - OutfitDataLoader와 통합된 초기화 시스템
   - 서버 URL 변경 및 재로드 기능

5. **CameraController.cs** (`Assets/Scripts/CameraController.cs`)
   - 의류 타입에 따른 자동 카메라 포지셔닝 시스템
   - Top (상의): x0 y1.41 z1.22 위치로 카메라 이동
   - Bottom (하의): x0 y0.89 z1.61 위치로 카메라 이동
   - 부드러운 애니메이션 지원 및 즉시 이동 옵션

6. **ClothRotator.cs** (`Assets/Scripts/ClothRotator.cs`)
   - 의류 모델 전용 마우스 회전 컨트롤러
   - 마우스 클릭 및 드래그로 의류 모델 회전
   - Y축 회전 (좌우), X축 회전 (상하) 지원
   - 회전 범위 제한, 관성 효과, UI 충돌 방지 기능
   - ClothingModel과 자동 연동
   - **WebGL 호환**: 메쉬에 미리 설정된 Collider 사용 권장

### 데이터 구조

- **OutfitData**: ScriptableObject 기반 의류 정보
  - `outfitName`: 의류 이름 (UI 표시용)
  - `type`: ClothingType enum (Top/Bottom)
  - `topCategory`: TopCategory enum (Tshirt/Shirt)
  - `bottomCategory`: BottomCategory enum (Pants/Shorts)
  - `texturePath`: Resources 폴더 내 텍스처 경로
  - `thumbnail`: 썸네일 이미지 (동적 로드)
  - `GetModelPath()`: 카테고리 기반 모델 경로 자동 생성

### 데이터 로딩 시스템

```
초기화 순서:
1. WebGLCommunicator.Start()
2. OutfitDataLoader.LoadOutfits() (JSON 로드)
3. 데이터 변환 및 캐싱
4. 웹에 "unityReady" 신호 전송
```

### Material 시스템

**Cloth_Texture.mat 통합 시스템:**
- 모든 의류 메쉬는 `Assets/Resources/Cloth_Texture.mat` Material을 공유
- 의류 변경 시 Material 인스턴스 생성 없이 Base Map만 교체
- URP 호환: `_BaseMap` 프로퍼티와 `mainTexture` 동시 설정
- 메모리 효율성 향상 및 렌더링 최적화

### JSON 데이터 구조

```json
{
  "outfits": [
    {
      "id": "tshirt_001",
      "outfitName": "기본 티셔츠",
      "type": "Top",
      "topCategory": "Tshirt",
      "texturePath": "https://example.com/textures/tshirt_001.jpg",
      "thumbnailUrl": "https://example.com/thumbnails/tshirt1.jpg"
    }
  ]
}
```

**주요 특징:**
- `modelPath` 제거: `topCategory` 또는 `bottomCategory` 값이 자동으로 모델 경로로 사용
- `texturePath` 지원: URL (서버) 또는 로컬 Resources 경로 모두 지원
- **모델 경로 매핑:**
  - Top 타입: `topCategory` 값 (예: "Tshirt" → Resources/Tshirt 모델 로드)
  - Bottom 타입: `bottomCategory` 값 (예: "Shorts" → Resources/Shorts 모델 로드)
- **텍스처 로딩:**
  - URL 시작 (http/https): 서버에서 다운로드 후 적용
  - 기타: Resources 폴더에서 로드

## 빌드 및 배포

### WebGL 빌드 명령
Unity에서 File > Build Settings > WebGL 플랫폼 선택 후 빌드

### 빌드 설정
- WebGL 메모리 크기: 32MB (초기)
- 최대 메모리: 2048MB  
- 메모리 증가 모드: 동적 증가
- WebGL 템플릿: Default

## JavaScript 통신 인터페이스

### Unity에서 웹으로 보내는 메시지
- `unityReady`: Unity 초기화 완료
- `outfitsLoaded:{count}`: 옷 데이터 로드 완료 (개수 포함)
- `clothingChanged:{outfitId}`: 의류 변경 완료
- `textureLoaded:{outfitId}`: 텍스처 다운로드 및 적용 완료
- `currentClothingInfo:{info}`: 현재 의류 정보
- `availableOutfits:{id|name|type,...}`: 사용 가능한 옷 목록
- `outfitsReloading`: 서버에서 데이터 재로드 시작
- `outfitsReloaded:{count}`: 서버에서 데이터 재로드 완료
- `error:{message}`: 에러 발생 시 메시지

### 웹에서 Unity로 호출 가능한 메서드
- `ChangeClothingFromWeb(string outfitId)`: 의류 변경 (ID 기반)
- `GetCurrentClothingInfo()`: 현재 의류 정보 요청
- `GetAvailableOutfits()`: 사용 가능한 옷 목록 요청
- `SetServerUrl(string url)`: 서버 URL 변경
- `ReloadOutfitsFromServer()`: 서버에서 옷 데이터 재로드

## 개발 시 주의사항

### 의류 데이터 관리
- 새로운 의류 추가 시 JSON 파일(`Assets/StreamingAssets/outfits.json`)만 수정
- `modelPath` 필드는 제거됨: `topCategory` 또는 `bottomCategory` 값이 자동으로 모델 경로가 됨
- Resources 폴더에 해당 카테고리 이름의 3D 모델 파일이 있어야 함 (예: Tshirt.prefab, Shorts.prefab)
- **중요**: 모든 메쉬 프리팹에 Collider 컴포넌트를 미리 설정해야 함 (BoxCollider 권장)
- 서버 연동 시 `OutfitDataLoader.useLocalFile = false` 설정
- ID는 고유해야 하며 ScriptableObject.name으로 사용됨

### 서버 연동
- 로컬 개발: `Assets/StreamingAssets/outfits.json` 사용
- 프로덕션: `OutfitDataLoader.serverUrl` 설정 후 서버 모드 사용
- CORS 정책 및 HTTPS 요구사항 확인

### WebGL 빌드 최적화
- Resources 폴더의 에셋은 빌드 시 모두 포함되므로 불필요한 파일 제거 필요
- 3D 모델은 LOD(Level of Detail) 최적화 권장
- 텍스처 압축 설정 확인 (WebGL 플랫폼용)

### 디버깅
- WebGL 빌드에서는 디버그 로그가 브라우저 콘솔에 출력
- 에디터에서는 `#if UNITY_WEBGL && !UNITY_EDITOR` 조건부 컴파일로 웹 통신 시뮬레이션
- 네트워크 요청 실패 시 브라우저 개발자 도구의 Network 탭 확인

### 에러 처리
- 모든 웹 호출에는 적절한 에러 메시지 반환
- 타임아웃 설정 (기본 30초)
- 데이터 로드 실패 시 fallback 메커니즘 구현