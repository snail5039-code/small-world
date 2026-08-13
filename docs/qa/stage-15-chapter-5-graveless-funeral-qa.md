# Stage 15 Chapter 5 — 장례식 없는 묘지 QA 계약

> 상태: **구현 계약 연결 대기**  
> 정본: `docs/archive/STORY.original.md`의 `## 5장 — 장례식 없는 묘지`  
> 범위: 독립 QA 계약 및 수동 QA. 런타임·씬·생성기 수정 없음.

## 확인한 기존 계약

- 액션 enum: `SmallWorld.Flow.OpeningStoryAction`
- 실행 진입점: `Stage15OpeningStoryService.TryPerform(SaveData, StoryProgress, OpeningStoryAction)`
- 성공/거부: `OpeningStoryResult.Accepted`
- 진행 기록: `StoryProgress`, `StoryFlowService.RecordChoice`, `SetFlag`
- 저장 왕복: `SaveDataStoryProgressStore` → `BinarySaveDataSerializer`
- 4장 종료 액션: `ReturnFromFacelessOffice`
- 4장 종료 결과: Chapter4 완료, `CurrentChapter == Chapter5`, Chapter5 미완료
- 기존 테스트 패턴: 선행 조건 없는 액션 거부 → `Perform(...)`로 정순 실행 → 선택/플래그/관계/가구/다음 장을 검증 → 직렬화 왕복 후 동일 상태 검증

현재 코드에는 Chapter5 액션, `PerformChapterFive`, Chapter5 라우팅이 없다. 아래 이름은 구현에 강제하는 신규 이름이 아니라 **연결 시 구현 담당자가 확정해야 할 의미 계약**이다. 자동 테스트는 확정된 실제 enum 이름을 그대로 사용해야 한다.

## 자동 QA 기대 계약 초안

### 1. 진입 순서 차단

- Chapter5가 현재 장이어도 Chapter4가 완결되지 않았다면 첫 5장 액션은 거부된다.
- Chapter4 완료 상태에서는 첫 5장 액션이 허용된다.
- 5장 완료 전 Chapter6 액션 및 해금은 거부된다.
- 거부된 액션은 완료 표식, 선택, 단서, 관계도, 가구를 변경하지 않는다.

### 2. 퍼즐 A — 네 개의 사망진단서

- 네 문서를 확인하기 전 `RESTORE HER` 복원은 거부된다.
- 구현이 문서 확인 순서를 요구한다면 오순서 입력은 거부되고 재시도 가능해야 한다.
- 오답은 성공 액션으로 기록되지 않으며 정답 재시도를 막지 않는다.
- 정답 완료 후 동일 정답 반복은 기존 `Has(progress, action)` 규칙대로 거부된다.

### 3. 퍼즐 B — 참석하지 않은 조문객

- 퍼즐 A 완료 전 방명록/그림자 연결은 거부된다.
- 잘못 연결한 서명은 영구 진행으로 기록되지 않고 올바른 연결을 다시 시도할 수 있다.
- 모든 연결 완료 시에만 "같은 손의 움직임" 단서가 기록된다.

### 4. 퍼즐 C — 존재하지 않는 묘비

- 퍼즐 B 완료 전 묘비 단계는 거부된다.
- 이름 새기기 지시를 따르는 오답은 핵심 진행을 완료하지 않으며 뒷면 조사 재시도가 가능하다.
- 뒷면 조사 완료 시 개발자의 뇌에 기억이 설치된 날짜 단서가 기록된다.

### 5. 최종 이름 분기

- 이름 입력 UI는 문자열을 받되, 서비스에는 공백 정규화 규칙이 명시되어야 한다.
- 빈 문자열 확정(필요하면 공백만 입력도 빈 값으로 취급)은 핵심 분기다.
- 핵심 분기는 "사랑했던 죽은 사람은 존재하지 않았다", "미래의 소녀가 상실과 죄책감을 설치했다", "순환 자체가 기원"이라는 공개를 보존한다.
- 일반 이름 입력은 입력 문자열 또는 안정적인 선택 ID를 저장하고 일반 엔딩용 반복 분기로 이동한다.
- 일반 이름 분기는 Chapter5 핵심 완료/Chapter6 해금으로 오인되어서는 안 된다. 반복 분기에서 다시 5장에 진입할 수 있는 계약이 필요하다.
- 빈 확정과 일반 이름 입력은 상호 배타적이며, 하나가 확정된 뒤 다른 결과가 덮어쓰이지 않는다.

### 6. 완료, 획득물, 외부 단서, 다음 장

핵심 빈 이름 분기 완료 후 다음을 검증한다.

- Chapter5의 네 완료 플래그가 모두 참이다.
- `CurrentChapter == StoryChapterId.Chapter6`이다.
- Chapter5는 완료, Chapter6는 미완료다.
- 획득 가구/기록: 빈 액자, 이름 없는 묘비 조각, 하얀 꽃병.
- 외부 단서/연출 상태: 집 안 사진에서 소녀가 모두 사라짐, 이름을 부르면 다른 방에서 서로 다른 목소리가 응답함.
- Chapter6만 해금되며 FinalChapter는 해금되지 않는다.

플래그 ID와 선택 ID는 기존 kebab-case 관례를 따르되 구현 담당자가 실제 상수를 확정하면 테스트도 그 이름을 그대로 참조한다. 문자열 리터럴을 QA에서 독자적으로 발명하지 않는다.

### 7. 저장 왕복

핵심 분기와 일반 반복 분기를 각각 저장 왕복한다.

- `SaveDataStoryProgressStore.Save`
- `BinarySaveDataSerializer.Serialize` / `TryDeserialize`
- `SaveDataStoryProgressStore.Load`

왕복 후 현재 장, Chapter5 완료 여부, 최종 이름 분기, 퍼즐 단서, 가구 3개, 사진 제거 상태, 다중 목소리 상태, Chapter6 해금 상태가 동일해야 한다. 일반 이름 문자열을 직접 저장한다면 한글과 공백 포함 입력도 손실 없이 복원되어야 한다.

### 8. 프롤로그~4장 회귀

기존 테스트를 그대로 실행해 다음을 보장한다.

- `Stage15OpeningStoryTests`: 프롤로그, 1장, 2장, 4장 흐름 및 저장 계약
- `Stage15PerfectDayAcceptanceTests`: 3장 흐름, 오답 재시도, 저장 계약
- 기존 Chapter4 액션 이름과 결과 플래그가 변경되지 않음
- `ReturnFromFacelessOffice`가 계속 Chapter5만 열고 Chapter5를 자동 완료하지 않음

## 수동 QA 절차

1. 프롤로그~4장을 정상 완료하고 5장 진입이 열리는지 확인한다.
2. 새 게임 또는 Chapter4 미완료 저장에서 5장 진입/첫 액션이 차단되는지 확인한다.
3. 퍼즐 A/B/C마다 선행 단계 건너뛰기와 오답을 한 번 수행한다. 진행 상태가 오염되지 않고 즉시 정답 재시도가 되는지 확인한다.
4. 묘비에 임의 이름을 새기는 오답 뒤 뒷면 조사가 가능한지 확인한다.
5. 최종 입력을 빈 상태로 확정한다. 핵심 공개, 가구 3개, 사진 제거, 다중 목소리, Chapter6 해금을 확인한다.
6. 별도 저장에서 한글 이름을 입력해 확정한다. 새로운 소녀/일반 반복 분기로 이동하고 핵심 완료나 Chapter6 해금으로 잘못 처리되지 않는지 확인한다.
7. 퍼즐 중간, 빈 이름 완료 직후, 일반 이름 분기 직후에 각각 저장하고 재실행/불러오기한다. UI와 내부 진행이 동일한지 확인한다.
8. 프롤로그부터 4장까지 대표 정답/오답 분기를 다시 실행해 기존 진행이 깨지지 않았는지 확인한다.

## 연결 대기 항목

자동 테스트 작성 전 구현 담당자에게서 다음 실제 계약이 필요하다.

- Chapter5의 `OpeningStoryAction` 실제 enum 이름과 정순 목록
- 이름 값을 전달하는 API(현재 enum-only `TryPerform`로는 임의 문자열 입력을 표현할 수 없음)
- 일반 이름 입력 반복 분기의 진행 상태와 재진입 규칙
- 선택 ID, 단서/외부 상태/가구 플래그 ID
- Chapter5 완료 및 Chapter6 해금 액션

이 계약이 연결되면 기존 `Stage15OpeningStoryTests`의 `ReadyChapterFour`/`Perform` 패턴과 동일한 별도 Chapter5 acceptance fixture로 구현하며, 런타임 코드는 QA 작업에서 수정하지 않는다.

## 외부 에셋 / 라이선스

- 외부 에셋 사용 없음.
- 신규 비용 없음(0원).
- 정본 스토리와 프로젝트 코드만 참조했다.
