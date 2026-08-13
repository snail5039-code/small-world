# 현재 프로젝트 상태

> 목적: 지금 필요한 범위와 다음 단계를 한 화면에서 확인합니다.
> 읽는 경우: 모든 작업 시작, 인수인계, 단계 완료 판단 시.
> 관련 문서: [`INDEX.md`](INDEX.md), [`roadmap/stage-15.md`](roadmap/stage-15.md), [`management/STAGE14_HANDOFF.md`](management/STAGE14_HANDOFF.md), [`management/WORKFLOW.md`](management/WORKFLOW.md)

- 기능 기준 커밋: `8bb2818` (`feat(stage15): 창문 안의 도시 씬 통합`)
- 14단계 인수인계: [`management/STAGE14_HANDOFF.md`](management/STAGE14_HANDOFF.md)
- 완료 단계: 1~14
- 현재 구현 단계: **15단계 — 전체 스토리 구현**
- 현재 단계 문서: [`roadmap/stage-15.md`](roadmap/stage-15.md)
- 15단계 구현 현황: 프롤로그부터 6장 `창문 안의 도시`까지의 진행과 `04_StoryRoute` 장면 연결이 구현되었습니다.
- 최근 안정화: 현실방 출구, 저장 UI 커서/입력 복원, 소녀 충돌, 근접 조사, 다음 방 Tab 기록/Esc 일시정지 입력을 보강했습니다. 현실방→스토리 방 전환은 현재 메모리 세션을 1회성으로 전달하며, 저장 패널은 RealityRoom Esc/Tab 입력 소유권에 통합되었습니다.
- 다음 구현 범위: 최종장 `아무것도 남지 않은 하얀 방` 진입과 최종 선택 직전까지의 진행.
- 최신 검증: Unity 6000.3.18f1 일반 Editor Test Runner에서 EditMode 179/179, PlayMode 28/28 통과, Windows Development 빌드 성공, 런타임 스모크 PASS.
- 라이선스 참고: 이 환경의 Personal 좌석은 배치 전용 `com.unity.editor.headless` 권한이 없어 `-batchmode` Editor 실행이 return code 198로 종료된다. 관리자 검증은 일반 Editor 자동 실행으로 수행한다.
- 이후 단계: 시작 금지. 15단계는 가시적 하위 작업을 만들고 진행합니다.
- 커밋·푸시 권한: 관리자에게만 있으며, 하위 작업은 별도 지시 없이는 금지합니다.

상태 갱신 시 이 파일과 `roadmap/README.md`의 상태 표시, `AGENTS.md`의 현재 단계 링크를 함께 변경합니다.
