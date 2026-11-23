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
        Assert.IsNotNull(panelBackground.sprite); // 제작한 전투 안내 배경 원본 확인
        Assert.AreEqual("TutorialPanel", panelBackground.sprite.name); // 공통 실제 배경의 이름 확인
        Assert.AreEqual(Image.Type.Sliced, panelBackground.type); // 화면 크기에 맞춰 금속 모서리 보존 확인
        Assert.IsTrue(panelBackground.raycastTarget); // 예시 영역에서 뒤 전투 입력 차단 확인
        RectTransform nextRect = overlay.Find("Panel/NextButton").GetComponent<RectTransform>(); // 확인 버튼 위치 조회
        RectTransform previousRect = overlay.Find("Panel/PreviousPage").GetComponent<RectTransform>(); // 이전 버튼 위치 조회
        RectTransform closeRect = overlay.Find("Panel/SkipButton").GetComponent<RectTransform>(); // 즉시 닫기 위치 조회
        Assert.Less(previousRect.anchorMin.x, nextRect.anchorMin.x); // 이전과 다음 버튼의 왼쪽·오른쪽 배치 확인
        Assert.AreEqual(nextRect.anchorMin.y, previousRect.anchorMin.y); // 하단 두 버튼의 동일한 높이 확인
        Assert.AreEqual(Vector2.one, closeRect.anchorMin); // 안내 패널 우측 상단 기준 확인
        Assert.AreEqual("X", closeRect.GetComponentInChildren<TMP_Text>().text); // 닫기 버튼 표시 확인
        Assert.IsNull(overlay.Find("Panel/NextPage")); // 중복 이동 버튼 제거 확인
        Assert.AreEqual(3, overlay.Find("Panel").GetComponentsInChildren<Button>().Length); // 전체 버튼 세 개 확인
        RawImage preview = overlay.Find("Panel/Preview/ExampleFrame/ExampleImage").GetComponent<RawImage>(); // 실제 전투 예시 이미지 조회
        Assert.IsNotNull(preview.texture); // 왼쪽 실제 전투 예시 이미지 확인
        Assert.Less(overlay.Find("Panel/Preview").GetComponent<RectTransform>().anchorMax.x, overlay.Find("Panel/Title").GetComponent<RectTransform>().anchorMin.x); // 예시 영역과 오른쪽 설명 배치 확인
        Assert.IsTrue(overlay.Find("Panel/Description").GetComponent<TMP_Text>().text.Contains("카드")); // 첫 안내 한국어 설명 확인
        overlay.Find("Panel/NextButton").GetComponent<Button>().onClick.Invoke(); // 다음 안내 버튼 클릭
        Assert.AreEqual(BattleTutorialStep.PlayCard, progress.Step); // 실제 카드 사용 대기 단계 확인
        Assert.AreEqual("다음", nextRect.GetComponentInChildren<TMP_Text>().text); // 실습 페이지에서도 하단 다음 문구 유지
        Assert.IsFalse(progress.CanAdvance); // 실제 카드 사용 전 페이지 건너뛰기 차단
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
        progress.OnCardUsed(); // 이번 검사에서 먼저 카드 사용 완료
        progress.OnPhaseStarted(BattleTurnPhase.EnemyTurn); // 실제 적 턴 시작 전달
        progress.OnPhaseStarted(BattleTurnPhase.PlayerTurn); // 실제 적 턴 종료 전달
        var next = nextRect.GetComponent<Button>(); // 하단의 유일한 다음 버튼
        var label = nextRect.GetComponentInChildren<TMP_Text>(); // 현재 다음 또는 완료 문구
        for (int index = 1; index < progress.PageCount; index++) // 시작부터 마지막 전까지 페이지 표기 확인
        { // 다음 버튼으로 모든 설명 순회 시작
            Assert.AreEqual(index, progress.PageNumber); // 미리 실습해도 중간 장의 실제 접근 확인
            Assert.AreEqual("다음", label.text); // 마지막 전까지 다음 버튼 유지 확인
            next.onClick.Invoke(); // 실제 버튼을 한 번 눌러 한 장 이동
        } // 순차 이동 확인 종료
        Assert.AreEqual(progress.PageCount, progress.PageNumber); // 마지막 번호와 실제 페이지 수 일치 확인
        Assert.AreEqual("완료", label.text); // 전투 마지막 장의 완료 버튼 문구 확인
        next.onClick.Invoke(); // 실제 마지막 장의 완료 클릭
        Assert.IsFalse(overlay.gameObject.activeSelf); // 완료 후 전투 튜토리얼 종료 확인
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
