# Third-Party Assets Register

이 문서는 SmallWorld에 포함되는 외부 에셋과 무료 리소스의 출처 및 이용 조건을 기록합니다.
에셋을 프로젝트에 추가하기 전에 아래 표를 작성하고 라이선스 원문 또는 증빙을 보관해야 합니다.

## 등록 원칙

- 상업적 이용이 명시적으로 허용된 에셋만 사용합니다.
- 출처 URL, 제작자, 취득일, 라이선스 버전을 기록합니다.
- 수정, 재배포, 크레딧 표기 조건을 각각 확인합니다.
- 라이선스 원문이 있으면 저장 위치와 원문 파일의 SHA-256 해시를 기록합니다.
- 조건이 불명확하거나 검증 상태가 미완료인 에셋은 출시 빌드에 포함하지 않습니다.
- Unity Package Manager 패키지는 `SmallWorld/Packages/manifest.json`과
  `packages-lock.json`에서 버전을 관리하며, 출시 전 패키지별 고지 의무를 별도로 검토합니다.

## 외부 에셋 목록

| 에셋명 | 용도 | 제작자/배포자 | 원본 URL | 취득일 | 버전 | 라이선스 | 상업 이용 | 수정 | 재배포 | 표기 조건 | 프로젝트 경로 | 라이선스 원문/증빙 | SHA-256 | 검증 상태 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Unity URP Core Template Content | 초기 프로젝트 템플릿 및 렌더 파이프라인 | Unity Technologies ApS | [Unity Editor Software Terms](https://unity.com/legal/editor-terms-of-service/software), [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license) | 2026-08-04 | Unity 6000.3.18f1 / URP 17.3.0 | Unity Editor Software Terms 및 Unity Companion License | 가능(Unity 기반 프로젝트) | 가능(Unity 기반 프로젝트) | 완성 게임에 포함 가능, 원본 패키지의 독립 재배포 금지 | URP의 `Third Party Notices.md`에 기재된 구성 요소 고지를 배포 문서에 유지 | `SmallWorld/Assets/Settings`, `SmallWorld/Assets/TutorialInfo`, `com.unity.render-pipelines.universal` | `SmallWorld/Library/PackageCache/com.unity.render-pipelines.universal@35356061dd01/LICENSE.md`, `Third Party Notices.md` | 패키지 잠금은 `SmallWorld/Packages/packages-lock.json`으로 검증 | 검토 완료(2026-08-04) |

## 출시 전 확인

- [ ] 모든 외부 에셋의 검증 상태가 완료이다.
- [ ] 크레딧 표기가 필요한 항목이 게임 내 크레딧과 배포 문서에 반영되었다.
- [ ] 재배포가 제한된 원본 파일이 별도로 노출되지 않는다.
- [ ] 교체되거나 삭제된 에셋의 기록과 증빙이 보존되어 있다.
- [ ] 패키지 및 에셋 업데이트 후 라이선스 변경 여부를 다시 확인했다.
