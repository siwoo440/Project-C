# 117일차 맵 선택과 새 편성 화면 진입

---
## 작업 내용

- 로비 탐사 버튼에서 현재 구현된 기본 탐사 맵을 선택하도록 연결
- 맵을 고른 뒤 왼쪽 캐릭터 목록, 가운데 네 편성 칸, 오른쪽 상세 정보로 이동
- 실제 캐릭터 초상화·전투 이미지와 출전 가능 상태 연결
- 네 명 초과 선택 차단, 뒤로 가기, 선택한 파티의 탐사 출발 검증
- 편성 칸과 상세 패널에 제작 이미지를 적용하고 프로필 간격과 흰색 구분선 추가
- 실제 실행 화면 캡처: `Map.png`, `Formation.png`
- 편성 1~4의 개별 저장과 전환은 118일차에 연결
- Unity EditMode 전체 테스트 160개 통과

---
## 이미지 제작

- 제작 방식: 내장 이미지 생성 도구
- 적용 경로: `Assets/_ProjectC/Resources/UI/Lobby/FormationCard.png`, `Assets/_ProjectC/Resources/UI/Lobby/FormationPanel.png`
- 편성 칸 프롬프트: `Use case: game UI asset. Create one original VERTICAL CHARACTER SLOT CARD BACKGROUND for a dark fantasy steampunk Unity game. First image is the existing game's UI style and palette; second is the current formation screen and provides layout context. A portrait-oriented tall rectangular card with a clean dark blue-green face and a narrow simple brushed bronze frame, restrained geometric corners, no scratches, grime, noise or ornate decoration. Keep the entire center dark and empty for a separately overlaid full-body character image and name. No text, no characters, no icons, no logos, no watermark. Isolated single complete card centered on a transparent background.`
- 편성 패널 프롬프트: `Use case: game UI asset. Create one original VERTICAL INTERFACE PANEL BACKGROUND for a dark fantasy steampunk Unity game. First image gives the existing game palette; second provides the character selection layout. A tall rectangular simple panel with a dark blue-green face, narrow refined bronze border and modest geometric corners. Clean smooth surfaces, subdued lighting, no scratches, grime, grain or busy patterns. Spacious empty center for independently overlaid character portraits or stats. No text, no symbols, no characters, no watermark. Isolated single complete panel on transparent background.`
