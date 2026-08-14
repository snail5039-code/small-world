# 현재 프로젝트 상태

> 목적: 지금 필요한 범위와 다음 단계를 한 화면에서 확인합니다.
> 읽는 경우: 모든 작업 시작, 인수인계, 단계 완료 판단 시.
> 관련 문서: [`INDEX.md`](INDEX.md), [`roadmap/stage-15.md`](roadmap/stage-15.md), [`management/STAGE14_HANDOFF.md`](management/STAGE14_HANDOFF.md), [`management/WORKFLOW.md`](management/WORKFLOW.md)

- 기능 기준 커밋: `09206d5` (`fix(stage15): 표지 방향과 이전 방 이동 수정`)
- 14단계 인수인계: [`management/STAGE14_HANDOFF.md`](management/STAGE14_HANDOFF.md)
- 완료 단계: 1~14
- 현재 구현 단계: **15단계 — 전체 스토리 구현**
- 현재 단계 문서: [`roadmap/stage-15.md`](roadmap/stage-15.md)
- 15단계 구현 현황: 프롤로그부터 최종장 `아무것도 남지 않은 하얀 방`의 최종 선택 직전 준비 상태까지 진행과 `04_StoryRoute` 장면 연결이 구현되었습니다. 실제 최종 선택과 엔딩 결과는 아직 구현하지 않았습니다.
- 최근 구현: `OpeningStoryAction` 150개를 `04_StoryRoute`의 실제 조사 오브젝트에 정확히 한 번씩 연결했습니다. 2장~최종장의 요약 Dialogue/Puzzle/Memory 표식만 눌러 장을 완료하던 우회 경로를 차단했고, 방별 상호작용 갤러리·발광 비콘·형태와 팔레트 차이를 추가했습니다. 최종 선택 준비 이후에는 명시적으로 중단하며 엔딩은 실행하지 않습니다.
- 디자인 상태: 반복 상자 갤러리를 제거하고 150개 행동을 의미 오브젝트로 구분했습니다. 뒤집히던 월드 표지를 플레이어 정면으로 교정하고 유나를 시작점 7~9m의 측면으로 축소·재배치해 중앙 시야와 목표를 가리지 않게 했습니다. 최종 아트·텍스처·사운드와 실제 퍼즐 조작 연출은 아직 필요합니다.
- 방 이동: PageUp/PageDown 및 각 방의 한국어 이전·다음 방 게이트로 해금된 과거 방을 왕복할 수 있습니다. 과거 방문 중에는 CurrentChapter·저장·행동 진행이 바뀌지 않으며 Pause/저장 UI가 열려 있으면 모든 이동 경로가 차단됩니다.
- UI 상태: StoryRoute 일시정지·기록 문구를 한국어화하고 Pause 패널을 우상단 반응형 크기로 축소해 중앙 시야를 가리지 않도록 했습니다.
- 다음 구현 범위: 장별 조사 스테이션을 실제 퍼즐 조작과 기억 공간 연출로 고도화.
- 최신 검증: Unity 6000.3.18f1 일반 Editor Test Runner에서 EditMode 206/206, PlayMode 54/54 통과, Windows Development 빌드 성공, 런타임 스모크 PASS.
- 라이선스 참고: 이 환경의 Personal 좌석은 배치 전용 `com.unity.editor.headless` 권한이 없어 `-batchmode` Editor 실행이 return code 198로 종료된다. 관리자 검증은 일반 Editor 자동 실행으로 수행한다.
- 이후 단계: 시작 금지. 15단계는 가시적 하위 작업을 만들고 진행합니다.
- 커밋·푸시 권한: 관리자에게만 있으며, 하위 작업은 별도 지시 없이는 금지합니다.

상태 갱신 시 이 파일과 `roadmap/README.md`의 상태 표시, `AGENTS.md`의 현재 단계 링크를 함께 변경합니다.
