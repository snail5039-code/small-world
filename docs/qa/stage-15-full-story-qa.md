# 15단계 전체 스토리 QA 계획

## 범위와 완료 판정

새 게임에서 프롤로그, 1~6장을 순서대로 지나 최종 선택 직전의 하얀 방까지 막힘 없이 진행되는지 검증한다. 각 장의 집 대화, 퍼즐, 기억 공간, 관계 분기, 중요 선택, 외부 존재 연출, 복선, 저장·복원과 최종장 잠금/해금이 서로 모순 없이 이어져야 한다. 14단계의 첫 기억 공간 왕복과 기존 이동·상호작용·대화·기록·소녀·저장 기능에 회귀가 없어야 합격이다.

기능 코드와 씬은 QA 범위 밖이며 외부 에셋을 사용하지 않는다. 현재 2~6장 전용 코어가 없으므로 자동 테스트는 기존 `SaveData`, `BinarySaveDataSerializer`, `MemoryJourneyFlow` 위에 기대 계약 골격을 둔다. 통합 시 테스트 내부 `ExpectedStoryProgress`를 실제 진행 API로 교체하되 테스트 의도와 플래그 행렬은 유지한다.

## 자동 테스트 계약

| ID | 테스트 | 기대 결과 |
|---|---|---|
| S15-A01 | `NewGame_IsLockedAndRequiresPrologueThroughChapterSixInOrder` | 새 게임은 프롤로그부터 시작하고 장 건너뛰기가 불가능하며, 6장 완료만으로 최종장이 열리지 않는다. |
| S15-A02 | `ChapterCompletion_RequiresDialoguePuzzleAndMemoryFlags` | 장의 대화·퍼즐·기억 플래그 중 하나라도 빠지면 완료 처리되지 않는다. |
| S15-A03 | `RelationshipBranchAndImportantChoice_DoNotBreakMandatoryProgress` | 낮음·중립·높음 관계 분기가 서로 다른 값을 보존하면서도 필수 진행을 막지 않는다. |
| S15-A04 | `BinaryRoundTrip_PreservesBranchesChoicesCluesAndFinalUnlock` | 2~6장 중요 선택, 외부 존재, 반복 109 복선, 활성 장면과 최종장 해금이 저장 왕복 후 유지된다. |
| S15-A05 | `Stage14FirstMemoryRegression_PartialSaveStaysLockedAndSolvedSaveReturns` | 1장 부분 퍼즐 저장은 출구가 잠기고, 복원 후 해결하면 하얀 방으로 복귀한다. |
| S15-A06 | 전체 EditMode | 14단계까지의 126개 기준 테스트와 15단계 테스트가 모두 통과하고 컴파일 오류가 없다. |

### 프롤로그 + 1장 `네 번째 자리` 수용 기준

| ID | 수용 기준 | 자동 검증 |
|---|---|---|
| S15-P1-01 | 새 게임의 현재 장은 프롤로그이며 필수 목표·대화·퍼즐·기억 중 하나라도 빠지면 1장으로 진행할 수 없다. | `PrologueAndFourthSeat_StaySequentialAndRequireEveryStoryBeat` |
| S15-P1-02 | 프롤로그 네 요소를 모두 완료한 뒤에만 1장이 열리고, 1장도 같은 네 요소를 모두 완료한 뒤에만 2장이 열린다. | `PrologueAndFourthSeat_StaySequentialAndRequireEveryStoryBeat` |
| S15-P1-03 | 프롤로그 완료, 1장 부분 진행, 네 번째 자리 이름 선택, `반복 109`, 첫 기억 문, 유나 관계값, 활성 장면이 저장·직렬화·복원 뒤 동일하다. | `PrologueAndFourthSeat_RoundTripPreservesSceneChoiceClueAndRelationship` |
| S15-P1-04 | `04_StoryRoute`에 진행 어댑터와 프롤로그/1장 노드가 있고 각 노드의 도착·대화·퍼즐·기억 진입점이 모두 연결된다. | `StoryRoute_IntegratesPrologueAndFourthSeatWithAllEntryPoints` |
| S15-P1-05 | 14단계 첫 기억 공간의 부분 저장, 미해결 탈출 잠금, 해결 후 하얀 방 복귀가 유지된다. | `Stage14FirstMemoryRegression_PartialSaveStaysLockedAndSolvedSaveReturns` |

### 2장 `마지막 승강장` 수용 기준

| ID | 수용 기준 | 자동 검증 |
|---|---|---|
| S15-C2-01 | 2장은 접속 시간 노선도 4단계 → 분실물 4개 반환 → 역재생 안내방송 3구간 → 목적지 선택 → 안전 구역 탈출 → 집 복귀 순서를 건너뛸 수 없다. 거부된 행동은 진행 플래그를 남기지 않는다. | `LastPlatform_EnforcesPuzzleAndEscapeOrder` |
| S15-C2-02 | 노선도 조각을 잘못 잇거나 분실물을 잘못 돌려주거나 방송 구간을 순서 밖에서 뒤집으면 오답 피드백만 반환하고, 같은 단계의 정답을 다시 제출할 수 있다. | `LastPlatform_WrongAnswersDoNotAdvanceAndRemainRetryable` |
| S15-C2-03 | 목적지는 `dohyeon-home`, `game-home`, `white-station` 중 정확히 하나만 기록된다. 각각 희생자 복원, 애정 기억, 자율/최초 AI 음성 단서를 남기며 어느 분기도 필수 탈출과 3장 진행을 막지 않는다. | `LastPlatform_DestinationBranchesPersistDistinctConsequences` |
| S15-C2-04 | 목적지 선택과 2장 퍼즐 단계·단서·활성 장면은 `SaveDataStoryProgressStore` 저장/복원 후 동일하고, 복원한 완료 상태에서는 일회 선택이나 보상을 중복 기록할 수 없다. | `LastPlatform_SaveRoundTripPreservesChoiceAndCompletion` |
| S15-C2-05 | 2장의 목표·대화·퍼즐·기억 공간이 모두 완료되고 집 복귀가 끝난 뒤에만 현재 장이 3장으로 바뀐다. 일부만 완료한 저장은 3장 노드를 열지 않는다. | `LastPlatform_UnlocksOnlyChapterThreeAfterReturnHome` |
| S15-C2-06 | `04_StoryRoute`의 `chapter-2` 노드는 `Last Platform` 표시명과 도착·대화·퍼즐·기억 진입점이 모두 연결되어 있고, 인접한 1장/3장 노드 순서를 유지한다. | `StoryRoute_IntegratesLastPlatformLandmarksBetweenChaptersOneAndThree` |
| S15-C2-07 | 프롤로그와 1장의 순서 잠금·선택·복선 및 14단계 첫 기억 공간 저장/복귀 테스트가 2장 추가 뒤에도 그대로 통과한다. | 기존 `Stage15OpeningStoryTests`, `Stage15StoryProgressContractTests`, `Stage14FirstMemoryRegression_PartialSaveStaysLockedAndSolvedSaveReturns` |

2장 런타임 액션의 테스트 계약 이름은 `HearDohyeon`, `ReadPlatformBoard`, `ConnectLoginTime1..4`, `ReturnEmployeeCard`, `ReturnChildShoe`, `ReturnHospitalBand`, `ReturnGameCartridge`, `ReturnItemToWrongShadow`, `ReverseAnnouncement1..3`, `ChooseRealityHome`, `ChooseGameHouse`, `ChooseWhiteStation`, `CrossSafeZone1..3`, `ReturnFromPlatform`이다. 오답은 별도 액션 이름을 만들지 않고 실제 순서형 액션을 앞 단계에서 시도해 거부·재시도 가능성을 검증한다. 저장 키 `platform-destination`과 결과 ID `reality-home`, `game-house`, `white-station`은 저장 호환 계약으로 유지한다.

### 현실방→스토리 방 입력 회귀 기준

| ID | 수용 기준 | 자동 검증 |
|---|---|---|
| S15-INPUT-01 | `04_StoryRoute` 진입 후 Tab으로 기록 오버레이를 열고 닫을 수 있으며 플레이어 입력과 커서가 정확히 복원된다. | `StoryRoute_TabOwnsAndRestoresPlayerInputAndCursor` |
| S15-INPUT-02 | Esc로 일시정지를 열고 닫을 수 있으며 `Time.timeScale`, 플레이어 입력, 커서가 정확히 복원된다. | `StoryRoute_EscapePausesAndRestoresRuntimeState` |
| S15-INPUT-03 | 저장 UI가 열려 있을 때 스토리 방 Tab/Esc 처리기가 입력 소유권을 빼앗지 않는다. | `StoryRoute_DoesNotStealInputWhileSavePanelOwnsIt` |

## 장별 필수 진행 매트릭스

각 장은 시작 전 집 대화 → 기억 공간 입장 → 퍼즐/조사 → 중요 선택 → 집 복귀 대화 순서로 확인한다. 모든 행에서 장 완료 플래그는 필수 관찰을 끝낸 뒤 한 번만 기록되어야 한다.

| 구간 | 대화·관계 | 퍼즐·기억 공간 | 중요 선택·복선 | 종료/다음 장 |
|---|---|---|---|---|
| 프롤로그 | 유나 첫 만남과 첫 관계 변화 | 소파 배치, 모형 집 연동, 첫 기억 문 | 예약 메일, `반복 109` | 첫 기억 문 상태가 유지되고 1장만 열린다. |
| 1장 네 번째 자리 | 서윤 말버릇과 의심/구원 감정 | 네 시각, 네 번째 접시, 사진 배열, 반복 복도 | 네 번째 자리의 이름 | 식탁·액자·지하실 열쇠가 반영되고 2장만 열린다. |
| 2장 마지막 승강장 | 도현과 유나의 종료 시간 언급 | 노선도, 분실물, 역재생 방송, 추격 | 현실 집/게임 집/하얀 역, 최초 AI 음성 | 벽시계·신발장·라디오와 선택 단서가 유지된다. |
| 3장 완벽한 하루 | 호감 선택과 선택지 밖 대답 | 틀린 주문, 네 번째 선택지, 멈춘 석양 | 사진 보존/파기, 성격 학습 화면 | 침실·거울·오르골과 관계 증감이 일치한다. |
| 4장 얼굴 없는 사무실 | 개발자 기록 신뢰 분기 | 인격 권한, 삭제 로그, 거울 회의실, 추격 | 삭제/보호/원본 서버; 자율 조건 | 합성 관리자 후보 공개와 서재 가구가 유지된다. |
| 5장 장례식 없는 묘지 | 이름 호출에 여러 목소리 응답 | 사망진단서, 방명록, 빈 묘비 | 빈 이름/이름 입력, 순환 기원 | 빈 액자·묘비 조각·꽃병과 일반 엔딩 분기를 구분한다. |
| 6장 창문 안의 도시 | 관리 AI와 유나 음성 대응 | 개발자 방, 모니터 순서, 파형, 추격 | 연결 유지/부분 차단/전체 차단 | 도시·시계·현관문과 구출/생존 조건이 저장된다. |
| 최종장 입구 | 희생자/유나 상태 대사 | 살아 있는 집→관리 핵심→현실 연결→하얀 방 | 보존 기억·이름·자율·연결 조건 | 조건 미달은 사유와 함께 잠기고 충족 시 최종 선택 직전까지만 진입한다. |

## 수동 종단·분기 시나리오

| ID | 절차 | 기대 결과 |
|---|---|---|
| S15-M01 | 새 슬롯으로 프롤로그→1~6장 최소 필수 경로→최종장 입구 | 순서 역전, 조작 잠김, 무한 대화, 잘못된 장면 전환 없이 최종 선택 직전 도달한다. |
| S15-M02 | 각 장에서 대화/퍼즐/기억 중 하나를 미완료하고 출구·다음 장·최종장 시도 | 이동이 차단되고 누락 사유가 표시되며 완료 보상이나 다음 장 플래그가 생기지 않는다. |
| S15-M03 | 관계 낮음/중립/높음 슬롯으로 장별 집 대화와 중요 선택 수행 | 관계별 대사·표현은 달라도 필수 퍼즐과 다음 장 진입은 모두 가능하다. |
| S15-M04 | 2장 목적지 3개, 3장 사진 2개, 4장 기록 3개, 5장 빈 이름/입력, 6장 연결 3개를 독립 슬롯에서 선택 | 선택별 보상·관계·자율·희생자/생존 단서가 해당 슬롯에만 반영된다. |
| S15-M05 | 4장 자율 부족/충족 상태에서 원본 서버 선택 확인 | 부족 시 선택이 숨김 또는 명확히 비활성화되고 충족 시 선택 가능하다. 다른 선택으로도 진행 가능하다. |
| S15-M06 | 각 장 시작, 퍼즐 중간, 중요 선택 직후, 집 복귀 직후 저장→종료→복원 | 정확한 장면·체크포인트·퍼즐 단계·관계·선택·가구·기억 플래그가 복원되고 일회 연출/보상이 중복되지 않는다. |
| S15-M07 | 상태가 다른 수동 슬롯 0/1/2를 교차 저장·복원 | 장·관계·선택·복선·최종장 잠금 상태가 슬롯 사이에서 섞이지 않는다. |
| S15-M08 | 필수 장 완료 전 최종장 입구 시도 후, 모든 조건 충족 뒤 재시도 | 미달 시 잠금과 누락 사유, 충족 시 단 한 번의 최종장 전환이 발생한다. |
| S15-M09 | 반복 109, 최초 AI 음성, 성격 학습, 합성 인격, 순환 기원, 미래 유나 단서를 순서대로 확인 | 외부 존재와 복선이 너무 이른 정답 공개 없이 누적되고 6장 공개와 모순되지 않는다. |
| S15-M10 | 14단계 첫 기억 공간의 미해결 탈출, 부분 저장, 해결 후 복귀, 재입장 | 미해결 탈출 차단과 슬롯별 부분 진행이 유지되며 보상이 중복되지 않는다. |
| S15-M11 | 이동·조사·대화·사진 퍼즐·기록·소녀 상호작용·수동 슬롯 3개 회귀 | 입력/UI/카메라가 정상 복구되고 기존 기능 데이터가 손실되지 않는다. |
| S15-M12 | Windows Development 빌드에서 S15-M01 핵심 경로와 저장 재실행 | 에디터 전용 참조 없이 빌드되고 Console/`Player.log` 예외 없이 최종 선택 직전 도달한다. |

## 실패 판정과 증거

- Blocker: 새 게임 종단 진행 불가, 저장 손실/슬롯 오염, 최종장 영구 잠금 또는 조건 우회.
- Critical: 필수 분기 소실, 장 순서 역전, 중요 선택 오기록, 14단계 핵심 흐름 회귀.
- Major: 외부 존재·복선 순서 모순, 관계 분기 오표시, 일회 연출/보상 중복.
- 보고 제목: `[Stage15][심각도][S15-Mxx] 제목`.
- 환경, 기준 커밋, 슬롯, 시작 체크포인트, 최소 재현 단계, 기대/실제, 재현율, Console 전체 예외와 `Player.log` 시각을 첨부한다.

## 통합 후 관리자 필수 검증

1. 2~6장 진행 API가 추가되면 기대 계약 골격을 실제 타입으로 교체하고 테스트가 의미 있게 실패/통과하는지 리뷰한다.
2. Unity 컴파일 오류 0개, 14단계 기준 126개 포함 전체 EditMode 통과 수를 기록한다.
3. 새 게임 S15-M01과 장별 S15-M02~M11을 실제 씬에서 수행하고 Console 오류 수를 기록한다.
4. Windows Development 빌드 성공 후 S15-M12와 런타임 스모크 로그를 확인한다.
5. 기능 코드·씬·테스트의 최종 Git 범위와 외부 에셋 등록부를 확인한다.

QA 하위 작업은 커밋·푸시하지 않는다.

## 이번 QA 실행 기록

- 근접 조사 전용 EditMode: 8/8 통과.
- 근접 조사 변경 후 첫 전체 EditMode: 159개 중 153개 통과, 6개 실패. 실패 원인은 앞선 씬 테스트가 `02_RealityRoom`을 남겨 뒤 탐지 테스트에 실제 씬 콜라이더가 섞인 테스트 순서 오염으로 확인했다.
- 격리 조치: `PlayerInteractionDetectorTests`, `Stage5InteractionTests` 실행 전후 빈 씬을 열도록 보강했다.
- 배치 재실행은 Unity Personal 좌석에 `com.unity.editor.headless` 권한이 없어 return code 198로 종료됐다. Hub에서 Personal 좌석 재활성화 후에도 배치 권한은 생성되지 않았다.
- 일반 Editor 자동 실행으로 전환해 컴파일 오류를 확인했고, 테스트의 Assembly-CSharp 경계를 리플렉션 계약으로 수정했다.
- 최종 전체 EditMode: 162/162 통과.
- Windows Development 빌드: 성공, `Builds/Windows/Development/SmallWorld.exe` 생성.
- 런타임 스모크: 부트→메인 메뉴→현실방→메인 메뉴 흐름 `[Stage2Smoke] PASS`.
- 남은 수동 확인: 현실방 문으로 `04_StoryRoute` 진입 후 Tab 기록 오버레이와 Esc 일시정지의 화면 표시·복원을 실제 조작으로 확인한다.

## 외부 에셋과 라이선스

추가하거나 사용한 외부 에셋 없음. 프로젝트 소유 코드와 문서만 사용했으므로 별도 라이선스 등록 없음.

