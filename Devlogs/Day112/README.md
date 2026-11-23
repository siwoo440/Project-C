# 112일차 저장 화면과 버튼 이미지

---
## 작업 내용

- 자동 저장 1칸과 수동 저장 4칸의 화면 구성
- 각 슬롯의 저장, 불러오기, 삭제 버튼과 빈 슬롯 표시
- 참조 이미지에 맞춘 종이 배경과 금속 버튼 이미지 제작
- 버튼의 기스와 거친 표면을 제거한 간결한 이미지 적용
- 실제 저장 파일 연결과 메뉴 진입은 113일차에 진행

---
## 이미지 제작

- 제작 방식: 내장 이미지 생성 도구
- 배경: `Assets/_ProjectC/Resources/UI/SaveMenu/Background.png`
- 저장: `Assets/_ProjectC/Resources/UI/SaveMenu/Save.png`
- 불러오기: `Assets/_ProjectC/Resources/UI/SaveMenu/Load.png`
- 삭제: `Assets/_ProjectC/Resources/UI/SaveMenu/Delete.png`
- 배경 프롬프트: `Use case: ui-mockup. Asset type: standalone background texture for a Unity dark-fantasy save/load menu. Reference image: use the attached first image only for its worn monochrome archival paper, industrial instrument accents, centered broad parchment panel, dark vignette and five-row list composition. Create an original high-resolution wide 16:9 background, straight-on, with a large weathered gray-brown paper sheet and five subtly separated empty horizontal slot areas. Keep the central writing spaces calm and legible for independently overlaid UI controls. No words, no letters, no numerals, no annotations, no thumbnails, no buttons, no recognizable game logo, no watermark. Full opaque background.`
- 버튼 수정 프롬프트: `Edit the attached target image for a Unity game UI. Preserve the exact overall square mechanical button silhouette, clean beveled double metal rim, transparent background, and central [downward arrow entering a shallow tray / upward arrow leaving a shallow tray / red X]. Simplify the design substantially: smooth uniform gray metal surfaces and clean edges, no scratches, no dents, no cracks, no rust, no dirt, no speckles, no grain, no texture. Restrained subtle lighting, clear icon, no text, one button only, centered with transparent padding.`

---
## 확인 범위

- Unity EditMode 전체 테스트 157개 통과: 슬롯 구조, 상태 표시, 버튼별 슬롯 전달 포함
- 실제 게임 화면의 시각적 배치는 별도 확인 필요
