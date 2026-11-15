# 113일차 저장 슬롯 연결과 경고창 UI

---
## 작업 내용

- 자동 저장과 네 개의 수동 슬롯에 저장, 불러오기, 삭제 연결
- 거점과 탐사 화면에 저장 메뉴 진입 버튼 추가
- 탐사 저장 화면을 여는 동안 이동과 게임 시간 일시 정지
- 덮어쓰기와 삭제 전 확인창 표시, 손상된 저장 파일 삭제 지원
- 새로하기와 슬롯 확인창에 공통 경고 배경·버튼 이미지 적용

---
## 이미지 제작

- 제작 방식: 내장 이미지 생성 도구
- 경고 배경: `Assets/_ProjectC/Resources/UI/SaveMenu/WarningBackdrop.png`
- 확인·취소 버튼: `Assets/_ProjectC/Resources/UI/SaveMenu/WarningButton.png`
- 경고 배경 프롬프트: `Use case: game UI asset. Create one original standalone horizontal confirmation dialog PANEL BACKGROUND for a Unity dark fantasy game. First attached image is a layout reference: centered wide rectangular warning box with room for a multi-line message and two buttons. Second attached image is the visual style reference: subdued charcoal, warm gray and archival parchment. Produce a restrained refined dark charcoal gray panel with a simple thin dark-metal frame and a very faint warm paper inner surface, front-on, crisp straight edges, no scratches, no grime, no dents, no dramatic ornament. Center is clean calm and empty for readable light text and separately overlaid buttons. No text, no letters, no symbols, no icons, no buttons, no watermark. Single isolated horizontal rectangular panel with transparent surroundings.`
- 확인 버튼 프롬프트: `Use case: game UI asset. Create a single original WIDE HORIZONTAL rectangular confirmation button background for a Unity dark fantasy save menu. Use the attached clean metal button as visual style reference: smooth charcoal gray metal, subtle beveled double edge, quiet restrained highlights. Front-facing, simple and clear, no scratches, dirt, texture, rust, cracks or dents. Middle dark flat and blank for independently overlaid light Korean text. No text, no arrows, no symbols, no watermark. Single centered wide button on transparent background with generous padding.`

---
## 확인 범위

- Unity EditMode 전체 테스트 160개 통과: 독립 테스트 파일로 슬롯 저장·복원·삭제, 덮어쓰기 확인, 손상 저장 처리 검증
- 실제 게임 화면의 시각적 배치는 별도 확인 필요
