using System.Collections; // 실제 탐사 UI 대기 사용
using System.Collections.Generic; // 대량 유물 검사 목록 사용
using System.Reflection; // 서로 다른 유물 식별자 검사 사용
using NUnit.Framework; // 유물 목록과 상세 표시 검증 사용
using TMPro; // 실제 유물 설명 글자 사용
using UnityEngine; // 실제 유니티 UI 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 유물 버튼 사용
public sealed class ExplorationRelicWindowTests // 탐사 유물 목록 화면 검사
{ // 유물 화면 검사 시작
    [UnityTest] // 실제 탐사 유물 화면 검사 지정
    public IEnumerator WindowTracksOwnedRelicsAndRestoresInput() // 보유 유물 선택과 이동 입력 복원 확인
    { // 유물 목록 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사 플레이 시작
        ExplorationPlayerController.SetInputBlocked(false); // 테스트 시작 이동 입력 허용
        GameObject canvasObject = new GameObject("RelicCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); // 실제 탐사 UI 캔버스 생성
        Object.DontDestroyOnLoad(canvasObject); // 씬 전환 중 테스트 캔버스 유지
        Canvas canvas = canvasObject.GetComponent<Canvas>(); // 실제 탐사 캔버스 조회
        RelicInventoryRuntime inventory = new RelicInventoryRuntime(new RelicGoldRuntime()); // 실제 유물 보관함 생성
        ExplorationRelicWindow window = ExplorationRelicWindow.Create(canvas, inventory); // 실제 탐사 유물 창 생성
        Button toggle = window.transform.Find("RelicButton").GetComponent<Button>(); // 실제 유물 열기 버튼 조회
        GameObject overlay = window.transform.Find("Overlay").gameObject; // 실제 유물 목록 화면 조회
        Assert.IsFalse(overlay.activeSelf); // 최초 유물 창 숨김 확인
        toggle.onClick.Invoke(); // 실제 유물 버튼 클릭
        Assert.IsTrue(overlay.activeSelf); // 실제 유물 창 열기 확인
        Assert.IsTrue(ExplorationPlayerController.InputBlocked); // 유물 화면의 탐사 이동 차단 확인
        Transform content = overlay.transform.Find("Window/Grid/Viewport/Content"); // 실제 유물 격자 내용 조회
        TMP_Text name = overlay.transform.Find("Window/Detail/Name").GetComponent<TMP_Text>(); // 선택 유물 이름 조회
        TMP_Text description = overlay.transform.Find("Window/Detail/Description").GetComponent<TMP_Text>(); // 선택 유물 설명 조회
        Assert.AreEqual(0, content.childCount); // 빈 보유 유물 격자 확인
        Assert.AreEqual("보유한 유물이 없습니다", name.text); // 빈 유물 목록 안내 확인
        RelicData first = Resources.Load<RelicData>("Relics/AshSeal"); // 실제 첫 유물 원본 조회
        RelicData second = Resources.Load<RelicData>("Relics/BrokenMusicBox"); // 실제 두 번째 유물 원본 조회
        Assert.IsNotNull(first); // 첫 유물 원본 확인
        Assert.IsNotNull(second); // 두 번째 유물 원본 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(first)); // 첫 유물 실제 획득 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(second)); // 두 번째 유물 실제 획득 확인
        Assert.AreEqual(2, content.childCount); // 실제 보유 유물 두 칸 표시 확인
        Assert.AreEqual(first.DisplayName, name.text); // 최초 유물 상세 이름 확인
        content.GetChild(1).GetComponent<Button>().onClick.Invoke(); // 두 번째 유물 실제 선택
        Assert.AreEqual(second.DisplayName, name.text); // 선택 유물 상세 이름 변경 확인
        Assert.AreEqual(second.Description, description.text); // 실제 선택 유물 효과 설명 확인
        Assert.IsTrue(inventory.TryRemove(second.RelicId)); // 선택 유물 실제 제거 확인
        Assert.AreEqual(1, content.childCount); // 제거 후 유물 격자 갱신 확인
        Assert.AreEqual(first.DisplayName, name.text); // 제거 후 남은 유물 상세 복원 확인
        overlay.transform.Find("Window/Close").GetComponent<Button>().onClick.Invoke(); // 실제 유물 창 닫기 버튼 클릭
        Assert.IsFalse(overlay.activeSelf); // 유물 창 닫힘 확인
        Assert.IsFalse(ExplorationPlayerController.InputBlocked); // 기존 탐사 이동 입력 복원 확인
        ExplorationPlayerController.SetInputBlocked(true); // 다른 화면의 기존 입력 잠금 설정
        toggle.onClick.Invoke(); // 기존 잠금 상태에서 유물 창 열기
        overlay.transform.Find("Window/Close").GetComponent<Button>().onClick.Invoke(); // 실제 유물 창 다시 닫기
        Assert.IsTrue(ExplorationPlayerController.InputBlocked); // 기존 외부 입력 잠금 유지 확인
        ExplorationPlayerController.SetInputBlocked(false); // 테스트 이동 입력 초기화
        Object.DestroyImmediate(canvasObject); // 테스트 유물 화면 정리
        yield return new ExitPlayMode(); // 실제 탐사 플레이 종료
    } // 유물 목록 검사 종료
    [UnityTest] // 대량 유물 격자 검사 지정
    public IEnumerator ManyOwnedRelicsRemainScrollable() // 다수 보유 유물의 스크롤과 격자 확인
    { // 대량 유물 검사 시작
        yield return new EnterPlayMode(); // 실제 탐사 플레이 시작
        ExplorationPlayerController.SetInputBlocked(false); // 테스트 이동 입력 초기화
        GameObject canvasObject = new GameObject("LargeRelicCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); // 대량 유물 테스트 캔버스 생성
        Object.DontDestroyOnLoad(canvasObject); // 씬 전환 중 테스트 캔버스 유지
        RelicInventoryRuntime inventory = new RelicInventoryRuntime(new RelicGoldRuntime()); // 실제 유물 보관함 생성
        ExplorationRelicWindow window = ExplorationRelicWindow.Create(canvasObject.GetComponent<Canvas>(), inventory); // 실제 유물 목록 화면 생성
        RelicData source = Resources.Load<RelicData>("Relics/AshSeal"); // 실제 유물 원본 조회
        Assert.IsNotNull(source); // 실제 유물 원본 존재 확인
        FieldInfo idField = typeof(RelicData).GetField("relicId", BindingFlags.Instance | BindingFlags.NonPublic); // 테스트 유물 식별자 필드 조회
        Assert.IsNotNull(idField); // 실제 유물 식별자 필드 확인
        List<RelicData> copies = new List<RelicData>(); // 테스트 유물 원본 목록 생성
        for (int index = 0; index < 298; index++) // 실제 시트 유물 수만큼 검사
        { // 테스트 유물 보관 시작
            RelicData relic = Object.Instantiate(source); // 실제 유물 원본 복제
            idField.SetValue(relic, "test_relic_" + index); // 서로 다른 테스트 유물 식별자 설정
            copies.Add(relic); // 테스트 유물 정리 목록 등록
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 실제 유물 획득 확인
        } // 테스트 유물 보관 종료
        window.transform.Find("RelicButton").GetComponent<Button>().onClick.Invoke(); // 실제 유물 화면 열기
        ScrollRect scroll = window.transform.Find("Overlay/Window/Grid").GetComponent<ScrollRect>(); // 실제 유물 스크롤 조회
        Assert.AreEqual(298, scroll.content.childCount); // 실제 보유 유물 전체 표시 확인
        Canvas.ForceUpdateCanvases(); // 유물 격자 크기와 스크롤 계산
        scroll.verticalNormalizedPosition = 0f; // 마지막 유물 쪽으로 스크롤 이동
        Assert.Greater(scroll.content.anchoredPosition.y, 0f); // 유물이 많을 때 실제 스크롤 이동 확인
        float previousScroll = scroll.content.anchoredPosition.y; // 유물 선택 전 스크롤 위치 저장
        Transform firstSlot = scroll.content.GetChild(0); // 기존 첫 유물 칸 저장
        scroll.content.GetChild(297).GetComponent<Button>().onClick.Invoke(); // 마지막 유물 실제 선택
        Assert.AreEqual(previousScroll, scroll.content.anchoredPosition.y, 0.01f); // 유물 선택 중 스크롤 위치 유지 확인
        Assert.AreSame(firstSlot, scroll.content.GetChild(0)); // 유물 선택 시 대량 격자 재생성 방지 확인
        window.transform.Find("Overlay/Window/Close").GetComponent<Button>().onClick.Invoke(); // 유물 창 닫기
        Object.DestroyImmediate(canvasObject); // 실제 유물 화면 정리
        foreach (RelicData relic in copies) // 테스트 유물 원본 순회
        { // 테스트 유물 정리 시작
            Object.DestroyImmediate(relic); // 임시 유물 원본 제거
        } // 테스트 유물 정리 종료
        yield return new ExitPlayMode(); // 실제 탐사 플레이 종료
    } // 대량 유물 검사 종료
} // 탐사 유물 목록 화면 검사 종료
