# Stage 15 RealityRoom 소녀 이동 충돌 QA

## 범위

`02_RealityRoom.unity`에서 소녀가 순찰(`Observe`), 플레이어 접근(`Approach`), 거리 확보(`KeepDistance`/`Withdraw`) 중 벽과 가구를 관통하지 않는지 검증한다. 런타임 이동 구현, 씬 생성기와 씬 YAML은 이 QA 작업의 수정 범위가 아니다.

## 자동 테스트

테스트 어셈블리: `SmallWorld.Character.PlayMode.Tests`

| ID | 시나리오 | 배치 | 합격 기준 |
|---|---|---|---|
| S15-GC-A01 | 순찰 중 벽 | 소녀와 순찰점 사이에 방 높이의 얇은 벽 Collider를 둔다. | 240프레임 동안 소녀 CapsuleCollider와 벽의 침투 깊이가 1mm를 넘는 프레임이 없다. |
| S15-GC-A02 | 접근 중 가구 | 소녀와 플레이어 사이에 허리 높이의 가구 BoxCollider를 둔다. | 접근하는 모든 프레임에서 소녀와 가구의 침투 깊이가 1mm 이하이다. 우회 또는 정지는 허용한다. |
| S15-GC-A03 | 후퇴 중 뒤쪽 벽 | 플레이어를 소녀 가까이에 두고 후퇴 방향에 벽 Collider를 둔다. | `Withdraw` 이동 중 벽 관통이 없으며, 벽 앞 정지 또는 안전한 우회만 발생한다. |

자동 테스트는 `Physics.ComputePenetration`으로 프레임마다 실제 Collider 겹침을 검사한다. 특정 이동 기술(NavMesh, CharacterController, Rigidbody)에 결합하지 않으며 구현이 우회와 정지 중 어느 정책을 택해도 통과할 수 있다.

## RealityRoom 수용 기준

1. 소녀 루트에 활성 CapsuleCollider 또는 동등한 체적 Collider가 있고, 발부터 머리까지 캐릭터 체형을 합리적으로 감싼다.
2. 순찰 경로와 플레이어 접근·후퇴 가능 영역의 벽 및 가구에 활성 비-Trigger Collider가 있다.
3. 순찰, 접근, `KeepDistance`, `Withdraw` 각각을 최소 30초 관찰하는 동안 벽·가구 안으로 시각 모델이나 캐릭터 Collider가 들어가지 않는다.
4. 막힌 직선 경로에서는 관통하거나 진동하지 않고 정지하거나 통행 가능한 경로로 우회한다.
5. 장애물 접촉 후 행동 상태를 바꾸면 이동이 회복되며, 영구 제자리걸음·순간이동·방 밖 이탈이 없다.
6. 30/60/120 FPS에서 동일 장애물 배치를 반복해도 관통이 재현되지 않는다.
7. Console과 Player 로그에 물리, NavMesh, MissingReference 또는 프레임당 반복 예외가 없다.
8. 기존 Stage 11 상태·대화·저장 테스트와 Stage 4 RealityRoom 씬 테스트가 함께 통과한다.

## 실행 및 판정

- Unity Test Runner의 PlayMode에서 `SmallWorld.Character.PlayMode.Tests`를 실행한다.
- 실제 `02_RealityRoom`에서 벽 모서리, 침대, 책상, 옷장, 책장, 모형 집 테이블을 대상으로 수용 기준 3~7을 확인한다.
- 자동 테스트 하나라도 실패하거나 실제 씬에서 한 프레임이라도 명백한 관통이 보이면 불합격이다.
- 현재 컨트롤러는 위치를 직접 더하는 방식이므로, 충돌 이동 구현이 통합되기 전에는 추가된 테스트가 실패하는 것이 예상된다.

## 외부 에셋 및 라이선스

신규 외부 에셋 없음. 테스트는 Unity 기본 GameObject와 Collider만 생성한다.
