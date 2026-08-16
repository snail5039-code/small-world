# Stage 15 조사 프롬프트 수명주기 QA

## 회귀 원인

Windows 런타임 `Player.log`에서 StoryRoute 진입·UI 비활성화 경계에 `InteractionPromptView.SetSuppressed`가 이미 Unity에 의해 종료된 feedback coroutine을 다시 정지하려 하며 예외가 발생한 이력이 있다. 이 오류는 일반적인 조사 성공 흐름만으로는 드러나지 않고, 긴 피드백 표시 중 장면 전환 또는 GameObject 비활성화가 먼저 일어날 때 재현된다.

## 자동 계약

`InteractionPromptLifecyclePlayModeTests`는 다음 두 수명주기를 실제 프레임과 coroutine으로 실행한다.

1. feedback coroutine 시작 → 한 프레임 진행 → 프롬프트 host 비활성화 → `SetSuppressed(true)` → 다음 프레임.
2. `04_StoryRoute`를 Single 모드로 실제 로드 → 활성 `InteractionPromptView` 확인 → feedback 시작 → UI 비활성화·억제 → 다음 프레임.

두 흐름 모두 `LogType.Exception`이 0건이어야 하며 Test Runner의 예상하지 않은 로그도 없어야 한다. 검증 대상은 결과 UI가 숨겨지는지만이 아니라 coroutine 소유권이 Unity의 비활성화 처리와 충돌하지 않는지이다.

## 수동 Windows 스모크

- RealityRoom 조사 피드백이 보이는 동안 StoryRoute 출구로 진입한다.
- StoryRoute에서 조사 피드백이 보이는 동안 Pause 또는 기록 UI를 열고 닫는다.
- 프롤로그의 현실방 복귀 게이트를 피드백 직후 사용한다.
- 각 흐름 뒤 `Player.log`에서 `InteractionPromptView`, `SetSuppressed`, `Coroutine`, `NullReferenceException`을 검색한다.
- 화면상 조사가 계속 가능하고 예외·MissingReference·중복 프롬프트가 모두 0건이어야 합격이다.

외부 에셋과 추가 라이선스는 사용하지 않는다.
