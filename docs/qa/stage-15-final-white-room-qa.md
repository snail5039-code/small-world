# Stage 15 최종장 — 아무것도 남지 않은 하얀 방 QA 계약

> 상태: **실제 런타임 계약 연결 완료**  
> 정본: `docs/archive/STORY.original.md`의 `## 최종장 — 아무것도 남지 않은 하얀 방`  
> 기준 커밋: `628f412`  
> 범위: 최종장 진입부터 최종 선택 직전 준비 상태까지의 독립 QA와 수동 QA. 최종 선택 실행·엔딩 결과·런타임·씬·생성기 수정은 범위 밖이다.

## 확인한 기존 계약

- 장 enum은 `StoryChapterId.FinalChapter`까지 정의되어 있다.
- 6장 정상 귀환은 Chapter6를 완료하고 현재 장을 FinalChapter로 옮기며 `final-chapter-unlocked`를 기록한다.
- 6장 `reality-link` 결과는 `body-connected`, `partial-cut`, `city-power-cut` 세 가지이고 각각 난이도, 구출 가능 희생자, 개발자 생존 상태 플래그가 저장된다.
- 진행 저장은 `StoryProgress`를 `SaveDataStoryProgressStore`에 넣고 `BinarySaveDataSerializer`로 이진 왕복한다.
- 실제 계약은 `OpeningStoryAction`, `Stage15OpeningStoryService.TryPerform`, `OpeningStoryResult.Accepted`를 사용한다.
- 정순 공개 액션은 `EnterLivingHouse` → 장별 `PreserveChapter*Furniture`/`DestroyChapter*Furniture` → `DestroyManagementCore1..6` → `EnterWhiteRoom` → `SitInFirstChair` → `SitInSecondChair` → `ActivateOldComputer` → `HearGirlAsDeveloper1..3` → `PrepareFinalChoice`다.
- 현실 육체 반영은 `EnterWhiteRoom`에서 기존 `reality-link`를 읽어 `reality-body-remaining-personality` 선택과 `reality-body-personality-*` 플래그로 기록한다.
- 자동 fixture는 실제 공개 enum과 저장 ID만 사용하는 `Stage15FinalWhiteRoomAcceptanceTests`로 연결했다.

## 범위 경계

합격 종점은 다음 조건을 모두 만족하는 **최종 선택 직전 준비 상태**다.

- 최종장 진입 조건이 검증되었다.
- 살아 있는 집의 기억 가구 보존/파괴 결과가 확정되었다.
- 관리 핵심 처리 순서와 그에 따른 희생자/소녀 결과가 확정되었다.
- 기존 `reality-link` 3분기가 최종장 상태에 반영되었다.
- 하얀 방 대화와 소녀→현실 개발자 변신 순서가 완료되었다.
- 최종 선택 6개의 가용/비가용 상태와 진엔딩 조건 가용성이 계산되었다.
- 준비 상태가 저장 왕복 후 동일하다.

어떤 선택도 실행하지 않으며 FinalChapter 완료, 엔딩 ID, 크레딧, 다음 단계 해금 또는 엔딩 결과 상태를 기록하지 않는다.

## 자동 acceptance 의미 계약

### 1. 진입 조건과 우회 차단

- Chapter6 미완료, 현재 장 불일치, `reality-link` 누락 중 하나라도 있으면 최종장 첫 행동을 거부한다.
- 6장 완료와 최종장 해금 상태가 모두 유효할 때만 첫 행동을 허용한다.
- 거부된 진입은 최종장 단계, 가구 상태, 관리 핵심, 대화, 변신, 선택 가용성에 흔적을 남기지 않는다.
- 유효한 진입을 중복 실행해도 일회 상태와 보상을 중복 기록하지 않는다.

### 2. 살아 있는 집 — 기억 보존/파괴

- 기억 가구별로 보존 또는 파괴 중 정확히 하나의 결과만 확정된다.
- 미처리 기억 가구가 남아 있으면 관리 핵심 단계로 진행할 수 없다.
- 보존은 해당 기억과 희생자 복원 가능성을 유지하고, 파괴는 탈출 진행을 허용하되 해당 기억과 복원 가능성을 제거한다.
- 이미 확정된 가구 결과를 반대 결과로 덮어쓰거나 같은 결과를 중복 집계할 수 없다.
- 모든 가구 보존, 혼합, 모든 가구 파괴 상태를 독립적으로 검증한다.

### 3. 관리 핵심 — 순서와 결과

- 살아 있는 집 단계 완료 전 첫 관리 핵심 처리는 거부된다.
- 관리 핵심은 구현이 확정한 실제 순서 계약으로만 누적된다. 앞 단계를 건너뛴 시도는 상태를 남기지 않고 현재 올바른 단계로 재시도할 수 있다.
- 처리 순서에 따라 소녀에게 합쳐진 기억과 사라진/복원 가능한 희생자 결과가 서로 구분되어야 한다.
- 희생자 결과는 살아 있는 집에서 파괴한 기억을 복원 가능으로 되살려서는 안 된다.
- 모든 핵심 처리가 끝나기 전 현실 연결 반영 또는 하얀 방 진입은 거부된다.

### 4. 기존 `reality-link` 3분기 반영

- `body-connected`, `partial-cut`, `city-power-cut`을 각각 독립 저장에서 검증한다.
- 최종장 진입 뒤에도 선택 ID와 6장 결과 플래그가 변경되거나 다른 분기로 덮어써지지 않는다.
- 각 분기는 소녀/개발자/주인공 중 현실 육체에 남을 수 있는 인격 조건과 최종 선택 가용성 계산에 서로 다른 입력으로 반영된다.
- 최종장은 6장의 현실 연결 선택을 다시 실행하거나 새 결과로 기록하지 않는다.
- 누락되거나 상충하는 현실 연결 상태는 안전하게 준비 완료를 거부하고 누락 사유를 제공해야 한다.

### 5. 하얀 방 대화와 변신 순서

- 살아 있는 집 → 관리 핵심 → 현실 연결 반영을 마친 뒤에만 하얀 방 대화를 시작한다.
- 두 의자와 낡은 컴퓨터가 있는 최초 공간으로 복귀한 상태가 먼저 확정되고, 맞은편 소녀가 대화 진행에 따라 현실 개발자의 모습으로 변한다.
- 대화와 변신은 구현이 확정한 단계 순서를 건너뛸 수 없다. 오순서 시도는 성공 상태를 남기지 않고 즉시 올바른 단계로 재시도할 수 있다.
- 마지막 대화/변신 전에는 선택 가용성을 노출하거나 `final-choice-ready`로 판정하지 않는다.
- 준비 완료 뒤에도 FinalChapter의 네 완료 플래그와 `IsComplete`는 거짓이어야 한다.

### 6. 최종 선택 6개와 진엔딩 조건의 **가용성 계산만** 검증

가용성 계산 대상은 정본의 다음 여섯 선택이다.

1. 프로그램 종료
2. 모형 집 연결
3. 소녀와 함께 남기
4. 새로운 관리자 되기
5. 소녀를 현실로 보내기
6. 희생자 복원 후 자신과 소녀의 기억 분배

- 계산 입력은 보존한 기억, 이름표 선택, 자율 수치, 현실 연결 상태와 구현 담당자가 확정한 선행 상태다.
- 각 선택은 `available` 또는 `unavailable`과 사유를 제공하되 실행 콜백, 엔딩 ID, 결과 플래그를 발생시키지 않는다.
- 진엔딩 조건은 별도의 가용성으로 계산하며 실제 진엔딩을 시작하지 않는다.
- 조건이 같은 저장은 반복 계산해도 동일한 결과를 내고 진행/관계/선택/플래그를 변경하지 않는다.
- 조건 하나를 경계값 아래/위로 바꾼 독립 fixture로 해당 가용성만 의도대로 달라지는지 검증한다.
- 여섯 선택의 정확한 내부 ID, 조건식, 자율 수치 원천과 임계값은 구현 담당자 계약 승인 전 QA가 발명하지 않는다.

### 7. `final-choice-ready` 저장 왕복

- 준비 완료 직전과 완료 직후를 별도 저장으로 만든다.
- 왕복 후 현재 장, 최종장 세부 단계, 가구별 보존/파괴, 관리 핵심 순서, 희생자/소녀 결과, `reality-link`, 하얀 방 대화/변신 단계, 여섯 선택 및 진엔딩 가용성이 동일해야 한다.
- 완료 직전 저장은 계속 잠겨 있고, 남은 정순 행동을 수행한 뒤에만 준비 완료가 된다.
- 완료 직후 저장은 `final-choice-ready`지만 선택 미실행, FinalChapter 미완료, 엔딩 결과 없음 상태를 유지한다.
- 로드나 가용성 재계산이 관계값, 자율 수치, 희생자 수, 일회 대화/변신을 중복 적용해서는 안 된다.

### 8. 실제 엔딩 미실행 안전 계약

- fixture는 최종 선택 실행 API를 호출하지 않는다.
- 준비 완료 후에도 엔딩/크레딧 장면 전환, FinalChapter 완료, 일반 엔딩 또는 진엔딩 결과, Stage16 상태가 없어야 한다.
- 선택 실행 API가 같은 서비스에 존재한다면 QA fixture의 테스트 더블 또는 읽기 전용 질의 경로로 가용성만 확인한다.

### 9. 프롤로그~6장 회귀

- 기존 `Stage15OpeningStoryTests`, `Stage15LastPlatformAcceptanceTests`, `Stage15PerfectDayAcceptanceTests`, `Stage15WindowCityAcceptanceTests`, `Stage15StoryProgressContractTests`를 변경 없이 함께 실행한다.
- 프롤로그~6장의 enum 값, 액션 이름, 선택 ID, 결과 플래그와 장 순서를 최종장 추가 때문에 변경하지 않는다.
- 특히 `reality-link` 3분기와 Chapter6 완료/FinalChapter 미완료 계약이 유지되어야 한다.
- Stage14 첫 기억 공간 왕복, 기존 이동·조사·대화·기록·소녀·수동 저장 회귀를 함께 확인한다.

## 확정된 acceptance fixture 연결

다음 실제 계약을 fixture에서 검증한다.

- 진입 잠금, 가구 결정 순서·중복 차단
- 가구 보존/파괴와 `final-core-result-*`, `final-victim-retained-*`, `final-girl-assimilated-*`
- `reality-link` 3분기와 `reality-body-remaining-personality`
- 의자·컴퓨터·`HearGirlAsDeveloper1..3` 정순 변신
- `final-choice-available-*`/`final-choice-unavailable-*` 여섯 쌍과 `true-ending-available`/`true-ending-unavailable`
- `final-choice-ready` 이진 저장 왕복, FinalChapter 미완료, 엔딩 선택 미기록
- 프롤로그~6장 완료 상태와 기존 선택의 저장 보존

## 수동 QA 절차

1. Chapter6 미완료, `reality-link` 누락, 정상 6장 완료 슬롯에서 최종장 진입을 각각 시도해 앞의 두 상태는 무변경 거부, 정상 상태만 1회 진입하는지 확인한다.
2. 살아 있는 집에서 전부 보존, 혼합, 전부 파괴 경로를 독립 슬롯으로 수행한다. 확정 결과의 덮어쓰기와 미처리 상태 우회를 시도한다.
3. 각 경로에서 관리 핵심을 한 번 오순서로 처리한 뒤 정순으로 재시도한다. 처리 순서에 따른 소녀 기억과 희생자 결과를 기록한다.
4. `body-connected`, `partial-cut`, `city-power-cut` 슬롯을 각각 불러 최종장 결과와 선택 가용성 차이를 비교한다. 6장 선택이 바뀌지 않는지 확인한다.
5. 하얀 방 진입 전 대화/변신을 시도하고, 진입 뒤 한 단계를 건너뛴 다음 정순으로 완료한다. 소녀가 대화에 따라 현실 개발자로 바뀌는지 확인한다.
6. 최종 대화 전에는 선택 UI가 잠겨 있고, 완료 뒤 여섯 선택과 진엔딩의 가용/비가용 및 사유가 조건과 일치하는지 확인한다.
7. 각 선택을 강조 표시하는 데까지만 확인하고 확정 입력은 하지 않는다. 엔딩·크레딧 전환과 결과 상태가 생기지 않아야 한다.
8. 살아 있는 집 중간, 관리 핵심 중간, 하얀 방 대화 중간, `final-choice-ready` 직후에 저장→종료→불러오기를 수행한다.
9. 준비 완료 저장에서 FinalChapter 미완료, 엔딩 결과 없음, 가용성 동일을 확인한다.
10. 새 슬롯으로 프롤로그부터 6장까지 대표 정답·오답·선택·저장 왕복을 실행하고 최종장 진입까지 회귀가 없는지 확인한다.

## 자동 테스트 연결

- fixture: `SmallWorld.Tests.EditMode.Flow.Stage15FinalWhiteRoomAcceptanceTests`
- 테스트 수: 5개
- 런타임·씬·생성기는 이 QA 작업에서 수정하지 않는다.

## 외부 에셋 / 라이선스

- 외부 에셋 사용 없음.
- 신규 비용 없음(0원).
- 정본 스토리와 프로젝트 소유 코드·문서만 참조했다.
