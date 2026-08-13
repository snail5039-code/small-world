# Stage 15 Chapter 6 — 창문 안의 도시 QA 계약

> 상태: **실제 런타임 계약 연결 완료**  
> 정본: `docs/archive/STORY.original.md`의 `## 6장 — 창문 안의 도시`  
> 기준 커밋: `e9ed262`  
> 범위: 독립 QA 계약 및 수동 QA. 런타임·씬·생성기 수정 없음.

## 확인한 기존 계약

- 액션 enum: `SmallWorld.Flow.OpeningStoryAction`
- 실행 진입점: `Stage15OpeningStoryService.TryPerform(SaveData, StoryProgress, OpeningStoryAction)`
- 성공/거부: `OpeningStoryResult.Accepted`
- 진행 기록: `StoryProgress`, `StoryFlowService.RecordChoice`, `SetFlag`
- 저장 왕복: `SaveDataStoryProgressStore` → `BinarySaveDataSerializer`
- 5장 종료 액션: `ReturnFromGravelessFuneral`
- 5장 종료 결과: Chapter5 완료, `CurrentChapter == Chapter6`, Chapter6 미완료, 6장 해금 플래그 기록
- 기존 테스트 패턴: 선행 조건 없는 액션 거부 → `Perform(...)`로 정순 실행 → 선택/플래그/관계/가구/다음 장을 검증 → 직렬화 왕복 후 동일 상태 검증

자동 acceptance fixture `Stage15WindowCityAcceptanceTests`는 구현 담당자가 확정한 실제 enum, 선택 ID와 플래그를 그대로 사용한다.

## 확정된 실제 계약

- 정순: `EnterWindowCityLastRoom` → `MatchDeveloperRoomTime` → `MatchDeveloperRoomFurniture` → `MatchDeveloperRoomRainDirection` → `ArrangeMonitorLoop1..3` → `ObserveRealtimeBackView` → `OverlayAdminGirlWaveform1..3` → 현실 연결 선택 → `WitnessCityWindowsStare` → `CarryCollapsingCity1..3` → `ReturnFromWindowCity`
- 선택 ID: `reality-link`
- 선택 결과: `KeepDeveloperBodyConnected`/`body-connected`, `CutSomeRealityCables`/`partial-cut`, `CutCityPower`/`city-power-cut`
- 선택별 결과: `final-difficulty-hard|normal|severe`, `rescuable-victims-many|some|few`, `developer-state-alive-connected|survival-uncertain|cannot-survive`
- 진행 공개: `realtime-player-back-view`, `admin-girl-waveform-perfect-match`, `future-girl-is-admin-ai`, `city-windows-stare-together`
- 가구: `furniture-completed-model-city`, `furniture-developer-stopped-wristwatch`, `furniture-last-room-front-door`
- 반전/해금: `future-girl-prepared-more-perfect-escape`, `final-chapter-unlocked`

## 자동 QA 기대 계약 초안

### 1. 진입과 순서 차단

- Chapter6가 현재 장이어도 Chapter5가 완결되지 않았다면 첫 6장 액션은 거부된다.
- Chapter5 완료 상태에서는 첫 6장 액션이 허용된다.
- 6장 완료 전 최종장 액션, 최종장 해금, 6장 귀환은 거부된다.
- 거부된 액션은 성공 액션 표식, 선택, 단서, 관계도, 가구, 장 완료 상태를 변경하지 않는다.
- 이미 성공한 단일 액션의 반복은 기존 `Has(progress, action)` 계약대로 거부되어야 한다.

### 2. 퍼즐 A — 현실 개발자 찾기

- 반복 속의 시간, 가구 배치, 비의 방향 단서를 모두 확인하기 전 개발자 방 확정은 거부된다.
- 잘못된 창문 또는 개발자 방 오답은 성공 진행으로 기록되지 않는다.
- 오답 직후 올바른 개발자 방을 다시 선택할 수 있어야 한다.
- 정답 완료 시에만 현실 개발자의 방을 찾았다는 단서가 기록되고 퍼즐 B가 열린다.

### 3. 퍼즐 B — 모니터의 시선

- 퍼즐 A 완료 전 모니터 배치는 거부된다.
- 모니터는 구현이 확정한 반복 순서대로만 누적된다.
- 다음 순서가 아닌 모니터를 배치하면 거부되고, 잘못된 단계가 성공 표식이나 순서 카운트에 포함되지 않는다.
- 오순서 시도 직후 현재의 올바른 모니터부터 다시 진행할 수 있어야 한다.
- 마지막 모니터 정렬 완료 시에만 현재 주인공의 뒷모습이 실시간으로 보인다는 단서가 기록되고 퍼즐 C가 열린다.

### 4. 퍼즐 C — 미래의 관리자

- 퍼즐 B 완료 전 파형 비교는 거부된다.
- 불일치 파형 시도는 거부되고 성공 표식, 완료 단서, 선택 상태를 남기지 않는다.
- 불일치 직후 올바른 파형 겹치기를 다시 시도할 수 있어야 한다.
- 완전히 일치하는 구간을 찾은 뒤에만 관리 AI와 소녀의 음성이 동일하다는 단서와 `관리 AI는 미래의 소녀`라는 핵심 공개가 기록된다.
- 핵심 공개 이전에는 현실 연결 선택과 추격 진입이 모두 거부된다.

### 5. 최종 선택 — 현실 연결

세 결과는 실제 선택 ID `reality-link`를 사용해 각각 독립 테스트에서 검증한다.

1. 개발자의 육체 연결 유지
2. 연결 케이블 일부 차단
3. 도시 전체 전원 차단

- 세 선택은 상호 배타적이며 한 선택이 기록된 뒤 다른 결과로 덮어쓸 수 없다.
- 연결 유지: `body-connected`, `final-difficulty-hard`, `rescuable-victims-many`, `developer-state-alive-connected`.
- 일부 차단: `partial-cut`, `final-difficulty-normal`, `rescuable-victims-some`, `developer-state-survival-uncertain`.
- 도시 전원 차단: `city-power-cut`, `final-difficulty-severe`, `rescuable-victims-few`, `developer-state-cannot-survive`.
- 선택 거부나 중복 시도는 관계도, 자율 수치, 희생자/생존 단서, 장 완료 상태를 추가 변경하지 않는다.

### 6. 추격 순서와 귀환

- 현실 연결 선택 전 첫 추격 단계는 거부된다.
- 추격은 구현이 확정한 체크포인트 순서대로만 누적된다.
- 앞 단계를 건너뛴 시도는 거부되고 올바른 현재 단계로 즉시 재시도할 수 있어야 한다.
- 추격 완료 전 귀환과 Chapter6 완료는 거부된다.
- 모든 창문의 사람들이 플레이어를 바라봄 → 건물들이 거대한 집으로 접힘 → 무너지는 도시를 들고 원래 집으로 귀환하는 순서가 진행 계약에 반영되어야 한다.

### 7. 완료, 가구, 단서, 최종장 입구

정상 선택과 추격을 끝내고 귀환한 뒤 다음을 검증한다.

- Chapter6의 네 완료 플래그가 모두 참이다.
- Chapter6는 완료되고 최종장은 아직 완료되지 않는다.
- 획득 가구/기록: 완성된 모형 도시, 현실 개발자의 멈춘 손목시계, 마지막 방의 현관문.
- 핵심 단서: 관리 AI는 육체를 얻지 못한 미래의 소녀이며, 과거의 소녀를 막는 존재가 아니라 더 완벽한 탈출을 준비한 형태다.
- 최종장 입구는 Chapter6 완료 후에만 해금된다.
- 가구 3개, 미래 소녀 핵심 단서, 현실 연결 결과가 하나라도 빠지면 최종장 입구를 완료 상태로 오인하지 않는다.

플래그 ID와 선택 ID는 기존 kebab-case 관례를 참고하되 QA가 문자열 리터럴을 독자적으로 발명하지 않는다. 구현 담당자가 확정한 실제 상수와 저장 결과를 사용한다.

### 8. 저장 왕복

다음 지점마다 독립 저장을 만들고 `SaveDataStoryProgressStore.Save` → `BinarySaveDataSerializer.Serialize` / `TryDeserialize` → `SaveDataStoryProgressStore.Load`로 왕복한다.

- 개발자 방 찾기 완료 직후
- 모니터 정렬 중간과 완료 직후
- 파형 불일치 직후와 정답 완료 직후
- 현실 연결 세 선택 각각의 확정 직후
- 추격 중간
- 귀환 및 Chapter6 완료 직후

왕복 후 현재 장, 성공 액션 순서, 퍼즐 완료 상태, 미래 소녀 단서, 현실 연결 선택과 결과 플래그, 추격 체크포인트, 가구 3개, 최종장 입구 해금 상태가 동일해야 한다. 거부된 오답이나 오순서 액션은 왕복 후에도 성공 상태로 나타나지 않아야 한다.

### 9. 프롤로그~5장 회귀

기존 테스트를 변경 없이 함께 실행한다.

- `Stage15OpeningStoryTests`: 프롤로그, 1장, 2장, 4장, 5장 흐름과 저장 계약
- `Stage15PerfectDayAcceptanceTests`: 3장 흐름, 오답 재시도, 저장 계약
- `Stage15LastPlatformAcceptanceTests`: 2장 순서·오답·분기 계약
- `Stage15StoryProgressContractTests`: 장 진행 및 저장 계약
- `ReturnFromGravelessFuneral`이 계속 Chapter6만 열고 Chapter6를 자동 완료하지 않는지 검증
- 기존 액션 이름, 선택 ID, 결과 플래그, 관계 변화가 6장 추가 때문에 바뀌지 않는지 검증

## 수동 QA 절차

1. 프롤로그~5장을 정상 완료하고 Chapter6만 열리며 최종장 입구는 잠겨 있는지 확인한다.
2. 새 게임, Chapter5 미완료 저장, Chapter6 위조 현재 장 상태에서 첫 6장 행동과 최종장 진입이 차단되는지 확인한다.
3. 시간·가구·비 방향 단서를 하나씩 빼고 개발자 방 확정을 시도한다. 각각 거부된 뒤 올바른 단서를 채워 재시도한다.
4. 잘못된 창문/개발자 방을 선택한 뒤 정답 방을 즉시 선택한다. 오답 흔적이 진행이나 저장에 남지 않는지 확인한다.
5. 모니터를 한 번 오순서로 배치한 뒤 현재 정순 단계부터 끝까지 완료한다. 마지막 모니터에 주인공의 실시간 뒷모습이 나타나는지 확인한다.
6. 불일치 파형을 한 번 선택한 뒤 일치 구간을 다시 선택한다. 오답이 미래 소녀 공개를 미리 해금하지 않고 정답 재시도를 막지 않는지 확인한다.
7. 독립 저장 세 개에서 현실 연결 선택을 하나씩 확정한다. 다른 두 선택이 비활성화되고 구현 담당자가 확정한 최종장 난이도·구출 가능 희생자·개발자 생존 결과가 각각 맞는지 확인한다.
8. 현실 연결 전, 추격 중 오순서, 추격 완료 전 귀환을 각각 시도해 차단과 재시도를 확인한다.
9. 추격을 정순으로 완료하고 귀환한다. 가구 3개, 미래 소녀 단서, 현실 연결 결과, Chapter6 완료, 최종장 입구 해금을 확인한다.
10. 위 저장 지점들을 재실행/불러오기해 UI 표시와 내부 진행이 동일한지 확인한다.
11. 프롤로그부터 5장까지 대표 정답·오답·선택·저장 왕복을 다시 실행해 회귀가 없는지 확인한다.

## 자동 테스트 연결

- fixture: `SmallWorld.Tests.EditMode.Flow.Stage15WindowCityAcceptanceTests`
- 순서/재시도: 개발자 방 단서, 모니터, 파형의 오순서가 상태를 남기지 않고 즉시 재시도되는지 검증한다.
- 저장: 모니터·파형 중간 상태와 완료/최종장 해금 상태를 각각 직렬화 왕복한다.
- 분기: 현실 연결 3개 결과를 `TestCase`로 독립 검증하고 다른 선택으로 덮어쓸 수 없음을 확인한다.
- 추격/완료: 도시 응시 전 차단, 운반 오순서 재시도, 조기 귀환 차단, 가구·반전·최종장 해금을 검증한다.
- 런타임·씬·생성기는 이 QA 작업에서 수정하지 않는다.

## 외부 에셋 / 라이선스

- 외부 에셋 사용 없음.
- 신규 비용 없음(0원).
- 정본 스토리와 프로젝트 코드만 참조했다.
