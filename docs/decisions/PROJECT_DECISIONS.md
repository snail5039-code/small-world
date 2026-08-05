# 프로젝트 결정 기록

> 목적: 구현 전 반드시 유지해야 할 확정 결정을 빠르게 확인합니다.
> 읽는 경우: 기술, 비용, 콘텐츠 독자성, 에셋 도입 판단 시.
> 관련 문서: [`../design/GAME_DESIGN.md`](../design/GAME_DESIGN.md), [`../management/THIRD_PARTY_ASSETS.md`](../management/THIRD_PARTY_ASSETS.md), [GDD 결정 원문](../archive/GAME_DESIGN_DOCUMENT.original.md#22-현재-결정-사항)

- Unity 6.3 LTS 계열, URP, C#, Windows PC 우선, DX11 우선, Windows IL2CPP를 기준으로 합니다.
- 싱글 플레이 1인칭 심리 공포 어드벤처이며 전투는 없습니다.
- 핵심 독자성은 모형 집, 희생자 기억 복원, 미래의 소녀인 관리 AI, 현실 복제와 하얀 방입니다.
- 프로토타입·버티컬 슬라이스 승인 전 비용은 0원입니다. 유료 구매·구독은 별도 승인 없이는 금지합니다.
- 프로젝트 소유 콘텐츠 또는 무료 상업 이용이 명확히 검증된 에셋만 사용하고 라이선스 등록을 선행합니다.
- 메타 공포는 게임 창과 내부 가짜 화면에 한정하며 실제 개인정보·운영체제 파일을 사용하지 않습니다.
- 자체 파일과 외부 에셋을 분리하고, 핵심 시스템은 데이터 중심·서비스 분리 구조로 직접 구현합니다.

정확한 성능 수치, 패키지 후보, 보류 항목과 세부 근거는 [GDD 기술 기획](../archive/GAME_DESIGN_DOCUMENT.original.md#17-기술-기획) 및 [스토리 기술 구조](../archive/STORY.original.md#15-개발-엔진과-기술-구조)를 참조합니다.
