# 현재 프로젝트 상태

> 목적: 지금 필요한 범위와 다음 단계를 한 화면에서 확인합니다.
> 읽는 경우: 모든 작업 시작, 인수인계, 단계 완료 판단 시.
> 관련 문서: [`INDEX.md`](INDEX.md), [`roadmap/stage-15.md`](roadmap/stage-15.md), [`management/STAGE14_HANDOFF.md`](management/STAGE14_HANDOFF.md), [`management/WORKFLOW.md`](management/WORKFLOW.md)

- 기능 기준 커밋: `1b087de` (`fix(stage15): 스토리 방 스폰과 최종장 씬 통합`)
- 14단계 인수인계: [`management/STAGE14_HANDOFF.md`](management/STAGE14_HANDOFF.md)
- 완료 단계: 1~14
- 현재 구현 단계: **15단계 — 전체 스토리 구현**
- 현재 단계 문서: [`roadmap/stage-15.md`](roadmap/stage-15.md)
- 15단계 구현 현황: 프롤로그부터 최종장 `아무것도 남지 않은 하얀 방`의 최종 선택 직전 준비 상태까지 진행과 `04_StoryRoute` 장면 연결이 구현되었습니다. 실제 최종 선택과 엔딩 결과는 아직 구현하지 않았습니다.
- 최근 안정화: 현실방 출구 후 스토리 방 외부에 잘못 스폰되어 회색 골격 전체가 노출되던 문제를 수정했습니다. 신규 게임은 프롤로그, 저장 복원은 현재 장(프롤로그~최종장)의 Arrival로 배치되며 잘못된 값은 프롤로그로 폴백합니다. 8개 방에 차폐·천장·입구 조명·카메라 배경을 추가했습니다.
- 다음 구현 범위: 장별 대화 완성과 실제 플레이 동선 보강.
- 최신 검증: Unity 6000.3.18f1 일반 Editor Test Runner에서 EditMode 183/183, PlayMode 38/38 통과, Windows Development 빌드 성공, 런타임 스모크 PASS.
- 라이선스 참고: 이 환경의 Personal 좌석은 배치 전용 `com.unity.editor.headless` 권한이 없어 `-batchmode` Editor 실행이 return code 198로 종료된다. 관리자 검증은 일반 Editor 자동 실행으로 수행한다.
- 이후 단계: 시작 금지. 15단계는 가시적 하위 작업을 만들고 진행합니다.
- 커밋·푸시 권한: 관리자에게만 있으며, 하위 작업은 별도 지시 없이는 금지합니다.

상태 갱신 시 이 파일과 `roadmap/README.md`의 상태 표시, `AGENTS.md`의 현재 단계 링크를 함께 변경합니다.
