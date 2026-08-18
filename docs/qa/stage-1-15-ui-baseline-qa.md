# 1~15단계 공통 UI 기본 품질 QA

## 목적과 범위

1~15단계에서 플레이어에게 노출되는 MainMenu, RealityRoom, FirstMemory, StoryRoute UI를 동일한 기본 품질 기준으로 점검합니다. 기능별 미술 완성도가 아니라 안전영역, 한글 가독성, 대비, 정렬, 레이어 우선순위와 입력 소유권의 최소 합격선을 정의합니다.

StoryRoute 좌상단 목표 HUD의 세부 수치 계약은 `stage-15-visual-presentation-qa.md`와 `Stage15GuidanceHudPresentationTests`를 정본으로 사용하며 여기서 중복 정의하지 않습니다.

## 장면·기능 인벤토리

| 장면 | 기본 HUD/메뉴 | 오버레이 | 주요 입력 |
|---|---|---|---|
| `00_Boot` | Loading Canvas | Loading Progress | 자동 전환 |
| `01_MainMenu` | Main Menu Canvas, Title Panel, Menu Panel | Settings Panel, Loading Panel | 마우스/키보드 선택, Esc 설정 닫기 |
| `02_RealityRoom` | Gameplay HUD, Player HUD, Interaction Prompt | Dialogue Card/History, Inspection, Inventory·Records, Photo Puzzle, Manual Save, Settings, Pause, Notification | E 조사, Tab 기록, Esc 우선순위 체인, 마우스, 저장 |
| `03_FirstMemory` | Player HUD, Interaction Prompt | 기억 공간 안내/피드백 | E 조사, Esc 복귀 계열 |
| `04_StoryRoute` | Player HUD, Prompt, 좌상단 목표 HUD | Records, Pause, 최종 선택 준비 정보 | E 조사, Tab 기록, Esc, PageUp/PageDown, Home 복귀 |

## 공통 화면 계약

- 기준 해상도는 1280×720과 1920×1080이며, 16:9 양쪽에서 화면 경계 밖으로 잘리는 필수 버튼·제목·본문이 없어야 합니다.
- Canvas UI는 `Scale With Screen Size`, 기준 1920×1080, 폭·높이 혼합 0.5를 사용합니다.
- 각 사용자 장면에는 `SafeAreaFitter` 또는 같은 역할의 명시적 안전영역 루트가 있어야 합니다.
- 화면 가장자리 고정 UI는 최소 16px, 주요 모달 내용은 최소 24px의 안전 여백을 둡니다.
- 본문 한글은 최소 14px, 버튼 16px, 모달 제목 20px을 사용하고 줄바꿈을 허용합니다.
- 사용자 텍스트에 `Text`, `Button`, `Paused`, `Press Esc`, `Lorem` 같은 개발용 문자열을 노출하지 않습니다. 깨진 문자는 소스에 글리프를 직접 적지 않고 Unicode U+FFFD 코드값으로 검사합니다. 고유명·게임 제목·키 이름은 예외입니다.
- 본문과 배경 대비는 4.5:1, 큰 제목·강조는 3:1 이상을 목표로 합니다. 반투명 배경은 실제 합성 결과로 판단합니다.
- 같은 행의 버튼 크기·간격과 같은 계층의 제목 정렬을 통일합니다. 클릭 영역은 최소 44×44px입니다.
- 비활성 `Stage 7 Dialogue UI`의 `Choice 1`~`Choice 3` 라벨은 런타임 대화 데이터가 채우는 템플릿이므로 직렬화 상태에서만 빈 문자열을 허용합니다. 그 밖의 버튼 라벨은 비어 있으면 안 됩니다.
- 대화 `Advance Button`과 `Choice 1`~`Choice 3`은 같은 위치를 번갈아 사용하는 상호배타 런타임 상태이므로 직렬화 좌표 중첩만으로 실패시키지 않습니다. 실제 대화 프레임에서 둘이 동시에 표시되면 실패입니다.

## 레이어와 입력 소유권

우선순위는 높은 항목이 낮은 항목의 입력과 표시를 소유합니다.

1. 장면 전환/Loading
2. Manual Save 및 확인 대화
3. Pause/Settings
4. Dialogue/Inspection/Photo Puzzle
5. Inventory/Records
6. Interaction Prompt/Notification
7. Gameplay HUD와 StoryRoute 목표 HUD

- 상위 오버레이가 열리면 하위 오버레이는 새로 열리지 않고, 플레이어 이동·시점 입력을 받지 않습니다.
- Esc는 현재 최상위 오버레이 하나만 닫으며 같은 입력으로 Pause를 연쇄 실행하지 않습니다.
- Tab은 Dialogue, Inspection, Puzzle, Save, Settings, Pause 중에는 기록을 열지 않습니다.
- 마우스가 필요한 UI는 `CursorLockMode.None`, visible=true를 요청하고 플레이어를 비활성화합니다.
- 마지막 UI를 닫으면 열기 직전의 player enabled, cursor lock/visible, `Time.timeScale`을 정확히 복원합니다.
- Pause만 `Time.timeScale=0`을 소유하며 Dialogue·기록·저장은 기존 값을 임의로 0 또는 1로 덮어쓰지 않습니다.
- 비활성 오버레이의 `CanvasGroup`은 interactable=false, blocksRaycasts=false여야 하며 보이지 않는 UI가 클릭을 가로채지 않습니다.
- Stage 10 저장 입력 소유권은 `Stage 10 Save Integration` 루트가 아니라 `Stage10ManualSavePanel.panel`에 직렬화된 `CanvasGroup`이 담당합니다.

## 기능별 필수 확인

- MainMenu: 새 게임/이어하기/설정/종료 버튼 정렬, 설정 취소와 적용, Loading 중 중복 클릭 차단.
- 대화: 화자·본문·선택지 계층, 긴 한글 줄바꿈, 히스토리 스크롤, 대화 중 조사·Tab·Pause 중첩 차단.
- 조사 프롬프트: 조준점 근처에서 대상명과 `[E]`를 읽을 수 있고 목표 HUD·대화 선택지를 가리지 않음.
- 인벤토리/기록: 탭 선택 상태, 목록·상세 영역 구분, 빈 상태 문구, Tab/Esc 닫기.
- 저장: 슬롯 번호·시간·장소·성공/실패 피드백, 마우스 즉시 사용, Esc 한 번으로 저장만 닫기.
- 설정: 라벨과 현재 값 연결, 적용/취소, 다른 모달 위 중복 개방 금지.
- Pause: 한국어 제목·복귀 안내, 중앙 플레이 시야를 과도하게 가리지 않음, Esc로 정확 복귀.
- Loading: 진행 상태와 장면 전환 중임을 한국어로 알리고 중복 클릭·조사·방 이동을 차단하며, 빈 패널이나 이전 영문 `LOADING`을 노출하지 않음.
- 방 이동 안내: StoryRoute의 이전·다음 방, 현실방 복귀와 PageUp/PageDown/Home 키 의미가 현재 목표보다 높은 위계로 튀지 않고, UI가 열렸을 때 같은 키 입력을 소비하지 않음.

## 화면 정보 계층과 모달 배경

- 모달은 제목 → 현재 상태/설명 → 주요 행동 → 취소·닫기의 순서가 한눈에 보여야 합니다. 제목은 최소 20px이며 본문·버튼보다 명확히 커야 합니다.
- Settings, Loading, Inspection, Dialogue History, Records, Photo Puzzle, Save, Pause에는 게임 화면과 내용을 분리하는 dim/background가 있어야 합니다. 배경은 최소 alpha 0.65이고 raycast를 소유하여 하위 UI 클릭을 막습니다.
- 목표 HUD는 공간명과 현재 목표를, 조사 Prompt는 현재 조준 대상과 `[E]` 행동을 담당합니다. 같은 문장을 중복 표시하거나 서로 겹쳐서는 안 됩니다.
- 대화는 화자·본문·선택지, 기록/인벤토리는 탭·목록·상세·빈 상태, 저장은 슬롯·시간·장소·결과를 각각 구분합니다. 데이터가 아직 없는 경우에도 설명 없는 빈 패널은 허용하지 않습니다.
- 장면에 남은 비활성 런타임 템플릿은 실제 활성화 전에 데이터가 채워져야 합니다. 직렬화 상태의 빈 대화 `Choice` 외에 빈 버튼과 활성 `Placeholder`/`Debug` 오브젝트를 허용하지 않습니다.
- `Inspection Title`은 `InspectionView.titleText`에 직렬화된 런타임 제목 템플릿이므로 빈 초기 문자열을 허용합니다. 시각 컴포넌트가 없는 Safe Area 보조 객체 이름은 플레이어 노출 placeholder로 계산하지 않습니다.

## 수동 스모크 매트릭스

각 행을 1280×720과 1920×1080에서 반복합니다.

| 장면/상태 | 확인 절차 | 합격 기준 |
|---|---|---|
| Boot | 실행 직후 Loading 관찰 | 중앙 정렬, 잘림 없음, 전환 중 중복 UI 없음 |
| MainMenu 기본 | 키보드·마우스로 모든 버튼 순회 | 포커스/hover 구분, 44px 클릭 영역, 한글 잘림 없음 |
| MainMenu 설정 | 설정 열기→값 변경→취소/적용→Esc | 배경 클릭 차단, 설정만 닫힘, 값 복원/적용 정확 |
| RealityRoom 기본 | 이동하며 여러 조사 대상 조준 | 프롬프트가 목표를 가리지 않고 E 대상명이 일치 |
| 대화 | 대화 시작→선택지→히스토리→종료 | player 정지, 커서 표시, Tab/Esc 우선순위 정확 |
| 조사/퍼즐 | 조사·사진 퍼즐 열기→오답→재시도→닫기 | 피드백 읽힘, 하위 UI 입력 차단, 상태 정확 복원 |
| 인벤토리/기록 | Tab→세 탭 순회→상세→Esc | 목록/상세 정렬, 빈 상태, Tab/Esc 닫기 정확 |
| 저장 | 저장 의자→슬롯 선택→저장→Esc | Esc 사전 입력 없이 마우스 사용, 저장만 닫힘 |
| Pause/설정 | Esc→설정→복귀→Esc | timeScale 0/복원, 커서·player 소유권 정확 |
| FirstMemory | 진입→조사→귀환 | HUD/프롬프트 안전영역, RealityRoom 상태 보존 |
| StoryRoute | 8방 이동·과거 방문·현재 장 복귀 | 목표 HUD 갱신, 진행/저장 불변, prompt와 비중첩 |
| StoryRoute 복귀 | Home/복귀 게이트→RealityRoom | 중복 전환 없음, 동일 저장 세션, UI 중 차단 |

### 해상도별 글자 승인 게이트

아래 항목은 1280×720과 1920×1080 모두에서 한 건이라도 실패하면 UI 승인을 보류합니다.

| 대상 | 확인 방법 | 합격 기준 |
|---|---|---|
| 전체 UI 문자열 | 모든 기본·hover·선택·오류·빈 상태를 순회 | U+FFFD, 네모 빈 글리프, 영문 placeholder, 개발용 키가 단독 노출되지 않음 |
| 본문·버튼·제목 | 화면 캡처를 100% 배율로 확인 | 본문 14px, 버튼 16px, 제목 20px 이상 체감 크기 유지 |
| 긴 한글 | 대화·기록 상세·조사 결과·저장 피드백에 20자 이상 문구 표시 | 2~4줄로 자연스럽게 wrap되고 마지막 글자·받침·문장부호가 잘리지 않음 |
| 버튼 라벨 | 모든 버튼의 가장 긴 상태 문구 확인 | 라벨이 버튼 배경 안에 있고 좌우·상하 여백이 남으며 인접 버튼과 겹치지 않음 |
| 화면 경계 | 좌상단 목표, 하단 Prompt, 우상단 Pause, 중앙 모달 확인 | safe area 밖, 화면 밖, 월드 표지/HUD/모달 간 중첩이 없음 |
| 동적 데이터 | 빈 기록·세이브 슬롯·런타임 Choice와 실제 데이터 상태 비교 | 설명 없는 빈 패널이나 빈 버튼이 없고 데이터 주입 후에도 높이·wrap이 유지됨 |

## 자동 계약 연결

- `Stage1To15UiBaselineAcceptanceTests`: 장면 UI 인벤토리, Canvas 반응형 설정, 안전영역, 기본 텍스트·클릭 영역, 비활성 raycast 계약.
- `Stage1To15UiMeaningAcceptanceTests`: 5개 장면의 구 영문·깨진 문자, 모달 dim/raycast, 한국어 제목·행동 라벨, 활성 placeholder·빈 버튼을 독립 감사.
- `Stage10ManualSaveInputAcceptanceTests`: 저장·대화·기록의 cursor/player/Esc 복원.
- `Stage8RecordSceneIntegrationTests`: 기록 탭과 입력 중첩 차단.
- `Stage15GuidanceHudPresentationTests`: StoryRoute 목표 HUD 해상도·대비·Pause 비중첩.
- `Stage15VisualPresentationAcceptanceTests`: StoryRoute Pause·방 이동·복귀·시각 구성.

외부 에셋과 추가 라이선스는 사용하지 않습니다.
