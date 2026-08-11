# 14단계 장면 연결과 진행 검증 QA 계획

## 범위와 합격 기준

하얀 방과 첫 기억 공간의 실제 장면 왕복, 순서 퍼즐, 탈출 잠금, 저장·재실행·불러오기, 하얀 방 복귀 변화를 검증한다. 이동·상호작용·대화·사진 퍼즐·기록·소녀 캐릭터·수동 저장 슬롯 3개의 기존 핵심 흐름에 회귀가 없어야 합격이다.

기능 코드와 씬 파일은 QA 범위 밖이다. 외부 에셋은 사용하지 않는다.

## 자동 테스트

| ID | 테스트 | 기대 결과 |
|---|---|---|
| S14-A01 | `MemoryPuzzleFlowTests`의 정답 1→2→3, 오답 초기화, 손상 상태 정규화 | 완료는 한 번만 적용되고 오답은 진행만 초기화하며 실수 횟수를 남긴다. |
| S14-A02 | `MemorySpaceProgressTests`의 입장→안전 구역→복귀와 재입장 | 입장·복귀·방문 횟수·단계가 저장 계약에 맞는다. |
| S14-A03 | `Stage14ProgressRoundTripTests.BinarySaveRoundTrip_PreservesMemoryReturnAndExistingCoreProgress` | 하얀 방 복귀, 퍼즐 완료도, 사진 퍼즐·관계·기록 데이터가 이진 저장 왕복 후 함께 보존된다. |
| S14-A04 | `Stage14ProgressRoundTripTests.PartialPuzzleProgress_RoundTripsWithoutMarkingMemoryExited` | 기억 공간 내부의 부분 진행을 복원해도 탈출 완료로 오인하지 않는다. |
| S14-A05 | 전체 EditMode 테스트 | 기존 이동 관련 계약, 상호작용, 대화, 사진 퍼즐, 기록, 소녀, 저장 코어 테스트가 모두 통과한다. |

권장 명령(프로젝트가 Unity 에디터에서 닫힌 상태):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe' -batchmode -nographics -projectPath '<workspace>\SmallWorld' -runTests -testPlatform EditMode -testResults '<workspace>\SmallWorld\TestResults\stage14-editmode.xml' -logFile '<workspace>\SmallWorld\Logs\stage14-editmode.log' -quit
```

종료 코드 0, XML 실패 0, 로그의 컴파일 오류 0을 모두 확인한다. Unity가 이미 프로젝트를 열고 있으면 배치 실행 대신 Test Runner에서 같은 범위를 실행한다.

## 실제 장면 스모크

테스트 전 새 게임 슬롯과 기존 진행 슬롯을 구분하고, 각 시나리오에서 Console 오류와 현재 장면·저장 슬롯을 기록한다.

| ID | 선행 조건 / 절차 | 기대 결과 |
|---|---|---|
| S14-M01 | 새 게임 → 하얀 방 이동·둘러보기 → 기억 공간 입구 상호작용 | 입력과 카메라가 정상이며 첫 기억 공간 장면으로 한 번만 전환된다. |
| S14-M02 | 기억 공간에서 탈출 입력을 먼저 시도 | 하얀 방으로 이동하지 않고 잠금 사유가 플레이어에게 전달된다. |
| S14-M03 | 1→오답 입력 후 1→2→3 입력 | 오답 뒤 진행이 초기화되고 정답 완료 연출과 출구 활성화가 한 번만 발생한다. |
| S14-M04 | 해결 후 출구 사용 → 하얀 방 복귀 | 하얀 방 장면으로 전환되고 기억 완료를 나타내는 영구적 변화가 보인다. 중복 전환·조작 잠김이 없다. |
| S14-M05 | 복귀 후 다시 기억 공간 진입 | 완료 상태와 복귀 변화가 유지되며 완료 보상이나 방문 횟수가 비정상 중복 적용되지 않는다. |
| S14-M06 | 퍼즐 1→2까지 진행 → 저장 → 플레이 종료 → 재실행 → 해당 슬롯 불러오기 | 첫 기억 공간과 부분 진행 2가 복원되고 미해결 출구는 계속 잠겨 있다. |
| S14-M07 | 퍼즐 해결·하얀 방 복귀 → 수동 슬롯 저장 → 재실행·불러오기 | 하얀 방에서 시작하고 해결·입장·탈출·복귀 변화가 모두 유지된다. |
| S14-M08 | 이동 후 조사 대상 상호작용 → 대화 선택 → 사진 퍼즐 한 단계 → 기록 열람 → 소녀 접근/상호작용 | 각 기존 흐름이 입력을 받고 상태·UI·카메라를 정상 복구하며 Console 예외가 없다. |
| S14-M09 | 서로 다른 상태를 수동 슬롯 0, 1, 2에 저장 → 각 슬롯 순차 불러오기 | 정확히 세 슬롯이 독립적으로 표시되고 선택한 슬롯의 장면·퍼즐·관계·기록 상태만 복원된다. |
| S14-M10 | Windows Development 빌드 실행 → S14-M01~M04 핵심 경로 → 종료 | 에디터 전용 참조 없이 빌드되며 `[Stage2Smoke] PASS`와 함께 핵심 왕복이 동작한다. |

## 결함 보고 형식

`[Stage14][심각도][S14-Mxx] 제목`

- 환경: 에디터/Windows 빌드, 새 게임 또는 슬롯 번호, 기준 커밋
- 재현: 시작 장면부터 번호가 있는 최소 단계
- 기대: 한 문장으로 관찰 가능한 결과
- 실제: 한 문장으로 관찰한 결과
- 증거: Console 전체 예외, `Player.log` 시각, 스크린샷 또는 영상
- 재현율: 예: 3/3

진행 차단·저장 손실·잘못된 슬롯 덮어쓰기는 Blocker, 핵심 흐름 회귀는 Critical로 보고한다.

## 기준 구현에서 발견한 통합 문제

### `[Stage14][Critical][S14-M02] 미해결 퍼즐의 복귀 차단 계약이 컨트롤러에 없음`

- 재현: `03_FirstMemory` 진입 → 1→2→3을 완료하지 않음 → Escape 또는 복귀 동작 실행
- 기대: 기억 공간에 남고 탈출 잠금 사유가 표시된다.
- 실제(코드 경로): `Stage12MemorySpaceController.ReturnToWhiteRoom()`은 `Stage13MemoryPuzzleController.IsCompleted`를 확인하지 않고 즉시 `HasExited=true`, `ActiveSceneId=RealityRoom`을 저장한 뒤 장면 전환을 요청한다.
- 통합 확인: 기능 통합 후 S14-M02를 실제 장면에서 다시 재현하고, 직접 메서드 호출 경로도 동일하게 차단되는지 확인한다.

### `[Stage14][Blocker][S14-M06] 순서 퍼즐 진행이 저장 슬롯 계약과 분리됨`

- 재현: 기억 공간에서 1→2 입력 → 수동 저장 → 프로세스 종료 → 다른 상태의 슬롯을 거쳐 원래 슬롯 불러오기
- 기대: 선택한 슬롯에 저장된 퍼즐 진행 2가 복원된다.
- 실제(코드 경로): `Stage13MemoryPuzzleController`는 진행·완료·실수를 전역 `PlayerPrefs`에만 저장한다. `MemorySpaceProgress.PuzzleProgress` 또는 슬롯의 `SaveData`를 읽고 쓰는 연결이 없어 슬롯별 진행 복원을 보장하지 못한다.
- 통합 확인: 퍼즐 상태를 Stage10 저장 캡처·복원에 연결한 뒤 S14-M06과 슬롯 교차 불러오기를 수행한다.

위 두 항목은 기능 코드 수정 권한이 있는 통합 작업에서 해결해야 하며 QA 작업에서는 수정하지 않는다.

## 통합 후 관리자 필수 검증

1. Unity 컴파일 오류 0개와 전체 EditMode 통과 수를 확인한다.
2. 실제 씬에서 S14-M01~M09를 새 게임과 기존 슬롯 양쪽으로 수행한다.
3. Windows Development 빌드 성공 후 S14-M10과 런타임 로그를 확인한다.
4. 기능 코드·씬 통합 변경을 포함한 최종 Git 범위를 검토한다.
5. 외부 에셋이 추가되지 않았고 등록부 변경이 불필요한지 확인한다.

QA 하위 작업은 커밋·푸시하지 않는다.

## 이번 QA 실행 기록

- 실행 시도: Unity 6000.3.18f1 전체 EditMode 배치 테스트
- 결과: 테스트 발견·컴파일 전에 Unity 라이선스 검증에서 종료되어 결과 XML이 생성되지 않음
- 로그 핵심: `No valid Unity Editor license found. Please activate your license.` / Unity 반환 코드 `198`
- 상태: 자동 테스트 미실행(환경 차단). 유효한 라이선스 세션에서 위 권장 명령 또는 Test Runner로 재실행해야 한다.

