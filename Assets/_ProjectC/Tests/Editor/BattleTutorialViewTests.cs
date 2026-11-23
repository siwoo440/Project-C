using System.Collections.Generic; // 임시 설정 값 목록 사용
using NUnit.Framework; // 안내 화면 동작 검사 사용
using TMPro; // 화면 문구 확인 사용
using UnityEngine; // 안내 화면 객체 사용
using UnityEngine.UI; // 화면 버튼과 이미지 확인 사용
public sealed class BattleTutorialViewTests // 전투 안내 화면 검사
{ // 안내 화면 검사 시작
    [Test] // 안내 화면 입력과 재시작 검사 지정
    public void View_BlocksCoveredBattleAndRestoresTheVisiblePractice() // 안내 버튼과 전투 입력 유지 확인
    { // 화면 동작 검사 시작
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)); // 독립 전투 캔버스 생성
        GameObject targetObject = new GameObject("Target", typeof(RectTransform)); // 강조 대상 화면 생성
        targetObject.transform.SetParent(canvasObject.transform, false); // 강조 대상을 캔버스에 배치
        BattleTutorialProgress progress = new BattleTutorialProgress(new MemoryTutorialStore()); // 독립 안내 진행 생성
        BattleTutorialView view = BattleTutorialView.Create(canvasObject.GetComponent<Canvas>(), progress, step => targetObject.GetComponent<RectTransform>()); // 안내 화면 생성
        progress.StartIfNeeded(true); // 첫 전투 안내 시작
        Transform overlay = view.transform.Find("Guide"); // 안내 화면 영역 조회
        Assert.IsTrue(overlay.gameObject.activeSelf); // 안내 화면 표시 확인
        Assert.IsTrue(overlay.GetComponent<Image>().raycastTarget); // 설명 중 실제 전투 레이캐스트 차단 확인
        Image panelBackground = overlay.Find("Panel").GetComponent<Image>(); // 공통 안내 배경 조회
        Assert.AreEqual(1f, panelBackground.color.a); // 완전히 불투명한 예시 패널 확인
        Assert.AreEqual(panelBackground.color.r, panelBackground.color.g); // 회색 배경의 빨강과 초록 값 확인
        Assert.AreEqual(panelBackground.color.g, panelBackground.color.b); // 회색 배경의 초록과 파랑 값 확인
        Assert.IsTrue(panelBackground.raycastTarget); // 예시 영역에서 뒤 전투 입력 차단 확인
        RectTransform nextRect = overlay.Find("Panel/NextButton").GetComponent<RectTransform>(); // 확인 버튼 위치 조회
        RectTransform skipRect = overlay.Find("Panel/SkipButton").GetComponent<RectTransform>(); // 건너뛰기 버튼 위치 조회
        Assert.Less(nextRect.anchorMin.x, skipRect.anchorMin.x); // 확인 오른쪽의 건너뛰기 배치 확인
        Assert.AreEqual(nextRect.anchorMin.y, skipRect.anchorMin.y); // 두 버튼의 같은 높이 확인
        RawImage preview = overlay.Find("Panel/Preview/ExampleImage").GetComponent<RawImage>(); // 실제 전투 예시 이미지 조회
        Assert.IsNotNull(preview.texture); // 왼쪽 실제 전투 예시 이미지 확인
        Assert.Less(preview.rectTransform.parent.GetComponent<RectTransform>().anchorMax.x, overlay.Find("Panel/Title").GetComponent<RectTransform>().anchorMin.x); // 예시 이미지와 오른쪽 설명 배치 확인
        Assert.IsTrue(overlay.Find("Panel/Description").GetComponent<TMP_Text>().text.Contains("카드")); // 첫 안내 한국어 설명 확인
        overlay.Find("Panel/NextButton").GetComponent<Button>().onClick.Invoke(); // 다음 안내 버튼 클릭
        Assert.AreEqual(BattleTutorialStep.PlayCard, progress.Step); // 실제 카드 사용 대기 단계 확인
        Assert.IsFalse(overlay.Find("Panel/NextPage").GetComponent<Button>().interactable); // 실제 카드 사용 전 페이지 건너뛰기 차단
        Assert.IsTrue(overlay.Find("Panel/NextButton").GetComponent<Button>().interactable); // 예시를 접는 확인 버튼 활성화
        overlay.Find("Panel/NextButton").GetComponent<Button>().onClick.Invoke(); // 실제 카드 사용을 위한 예시 접기
        Assert.IsFalse(overlay.gameObject.activeSelf); // 카드와 대상 위의 화면 및 입력막 제거
        Assert.AreEqual(BattleTutorialStep.PlayCard, progress.Step); // 예시 확인만으로 카드 사용 단계 통과 금지
        view.transform.Find("ReplayButton").GetComponent<Button>().onClick.Invoke(); // 실습 중 같은 설명 다시 열기
        Assert.IsTrue(overlay.gameObject.activeSelf); // 동일 페이지 표시 확인
        Assert.AreEqual(BattleTutorialStep.PlayCard, progress.Step); // 실습 중 도움말의 페이지 위치 보존
        overlay.Find("Panel/SkipButton").GetComponent<Button>().onClick.Invoke(); // 안내 건너뛰기 클릭
        Assert.IsFalse(overlay.gameObject.activeSelf); // 안내 화면 숨김 확인
        view.transform.Find("ReplayButton").GetComponent<Button>().onClick.Invoke(); // 전투 안내 다시 보기 클릭
        Assert.AreEqual(BattleTutorialStep.Overview, progress.Step); // 처음부터 다시 보기 확인
        Object.DestroyImmediate(canvasObject); // 임시 화면 제거
    } // 화면 동작 검사 종료
    private sealed class MemoryTutorialStore : ISettingsStore // 안내 화면 검사용 저장소
    { // 임시 저장소 시작
        private readonly Dictionary<string, int> values = new Dictionary<string, int>(); // 저장 값 목록
        public int Read(string key, int fallback) // 임시 설정 값 조회
        { // 설정 조회 시작
            return values.TryGetValue(key, out int value) ? value : fallback; // 저장 값 또는 기본값 반환
        } // 설정 조회 종료
        public void Write(string key, int value) // 임시 설정 값 기록
        { // 설정 기록 시작
            values[key] = value; // 설정 값 저장
        } // 설정 기록 종료
    } // 임시 저장소 종료
} // 안내 화면 검사 종료
