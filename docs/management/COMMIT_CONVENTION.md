# 커밋 규칙

이 프로젝트는 [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/) 형식을 따른다.

## 형식

`type(scope): 한국어 설명`

- 제목은 한국어로 작성하고 72자 이내로 간결하게 쓴다.
- `type`은 `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `build`, `ci`, `style`, `perf` 중 하나를 사용한다.
- 기능 추가는 `feat`, 버그 수정은 `fix`, 문서는 `docs`, 테스트는 `test`를 우선 사용한다.
- 필요하면 본문에 변경 이유와 검증 결과를 한국어로 기록한다.
- 호환성을 깨는 변경은 본문 또는 footer에 `BREAKING CHANGE: 한국어 설명`을 기록한다.
- 하위 세션은 커밋과 푸시를 하지 않는다. 관리자가 검토·검증 후 커밋한다.
- 커밋 전 `git diff --check`, 관련 테스트, Windows 빌드, 런타임 스모크를 관리자가 확인한다.

## 예시

```text
feat(stage12): 기억 공간 입장과 복귀 흐름 추가
fix(interaction): 시계 조사 시 오브젝트가 이동하지 않도록 수정
docs: 12단계 진행 상태와 커밋 규칙 갱신
test(stage12): 기억 공간 전환 회귀 테스트 추가
```

## 적용 범위

이 규칙은 앞으로 생성하는 모든 커밋에 적용한다. 커밋 메시지는 명령어 타입 표기만 영문으로 두고 설명·본문·footer는 한국어로 작성한다.
