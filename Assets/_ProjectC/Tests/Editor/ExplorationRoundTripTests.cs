using System.Collections; // 프레임 대기 사용
using System.Collections.Generic; // 화면 입력 결과 목록 사용
using NUnit.Framework; // 검사 결과 확인
using UnityEngine; // 게임 객체와 시간 사용
using UnityEngine.EventSystems; // 화면 좌표의 클릭 대상 확인
using UnityEngine.InputSystem; // 시험용 키보드 사용
using UnityEngine.InputSystem.LowLevel; // 키 누름 상태 입력
using UnityEngine.Profiling; // 유니티 메모리 측정
using UnityEngine.SceneManagement; // 실제 씬 이름 확인
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 화면 버튼 사용

public sealed class ExplorationRoundTripTests // 탐사와 로비 반복 전환 검사
{ // 반복 전환 검사 시작
    [UnityTest] // 플레이 모드 반복 검사 지정
    public IEnumerator ThreeRoundTripsRestoreInputAndReportMemory() // 세 번의 왕복 후 입력과 메모리 확인
    { // 왕복 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 자동 저장 전용 검증 프로필 확인
        { // 일반 저장 보호 시작
            Assert.Ignore("자동 저장 격리를 위해 복제본의 Company Name과 Product Name을 모두 ProjectCVerification으로 지정하세요."); // 일반 게임 프로필 검사 생략
        } // 일반 저장 보호 종료
        yield return new EnterPlayMode(); // 실제 게임 실행
        SceneManager.LoadScene("00_Boot"); // 게임 시작 씬 열기
        yield return WaitForScreen<MainMenuController>("10_MainMenu"); // 첫 메뉴 초기화 대기
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 검증 프로필에서 새 게임 시작
        yield return WaitForScreen<LobbyLayoutView>("20_Lobby", "ExpeditionButton"); // 기준 로비의 탐사 버튼 준비
        long baseline = Profiler.GetTotalAllocatedMemoryLong(); // 첫 로비의 유니티 할당량 측정
        Assert.Greater(baseline, 0L, "유니티 메모리 계측값 없음"); // 메모리 계측 가능 여부 확인
        Debug.Log($"[Day194] baseline lobby allocatedBytes={baseline} unity={Application.unityVersion} screen={Screen.width}x{Screen.height} graphics={SystemInfo.graphicsDeviceType}"); // 기준값과 측정 환경 기록
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 실제 기기와 분리된 시험용 키보드 생성
        try // 입력 장치 정리 보장
        { // 반복 측정 시작
            for (int cycle = 1; cycle <= 3; cycle++) // 세 차례 같은 경로 순회
            { // 한 차례 왕복 시작
                LobbyLayoutView lobby = Object.FindFirstObjectByType<LobbyLayoutView>(); // 현재 로비 화면 조회
                Assert.AreEqual(SceneManager.GetActiveScene(), lobby.gameObject.scene, "로비 메뉴가 다른 씬의 캔버스에 연결됨"); // 복귀 후 로비 메뉴의 실제 씬 소속 확인
                Click(lobby.transform, "ExpeditionButton"); // 로비에서 탐사 지역 선택 열기
                ExpeditionMapView map = Object.FindFirstObjectByType<ExpeditionMapView>(); // 실제 지역 선택 화면 조회
                Assert.IsNotNull(map); // 로비 클릭 처리 확인
                yield return WaitForButton(map.transform, "MapOption_0"); // 새 지도의 지역 버튼 렌더링 대기
                Click(map.transform, "MapOption_0"); // 기본 탐사 지역 선택
                Click(map.transform, "ConfirmButton"); // 파티 편성 화면 열기
                PartyFormationView formation = Object.FindFirstObjectByType<PartyFormationView>(); // 실제 편성 화면 조회
                Assert.IsNotNull(formation); // 편성 화면 표시 확인
                yield return WaitForButton(formation.transform, "StartButton"); // 새 편성 화면의 출발 버튼 렌더링 대기
                if (cycle == 1) // 첫 출발의 빈 편성 확인
                { // 첫 캐릭터 선택 시작
                    yield return WaitForButton(formation.transform, "CharacterList/Viewport/Grid/Character_0"); // 첫 캐릭터 프로필 렌더링 대기
                    Click(formation.transform, "CharacterList/Viewport/Grid/Character_0"); // 첫 캐릭터 출전 선택
                } // 첫 캐릭터 선택 종료
                double start = Time.realtimeSinceStartupAsDouble; // 출발 버튼 누르기 직전 시각 기록
                Click(formation.transform, "StartButton"); // 선택한 파티로 탐사 출발
                AssertLoading(); // 탐사 전환 중 로딩 화면 확인
                yield return WaitForScreen<ExplorationRelicWindow>("30_Exploration", "RelicButton"); // 탐사 씬과 유물 버튼 준비 대기
                Report(cycle, "exploration", start, baseline); // 탐사 진입 시간과 메모리 기록
                ExplorationRelicWindow relics = Object.FindFirstObjectByType<ExplorationRelicWindow>(); // 현재 탐사 유물 창 조회
                Click(relics.transform, "RelicButton"); // 화면 좌표로 유물 창 열기
                Assert.IsTrue(relics.transform.Find("Overlay").gameObject.activeInHierarchy); // 실제 유물 창 표시 확인
                Assert.IsTrue(ExplorationPlayerController.InputBlocked); // 유물 창의 탐사 이동 차단 확인
                Rigidbody2D body = Object.FindFirstObjectByType<ExplorationPlayerController>().GetComponent<Rigidbody2D>(); // 실제 플레이어 몸체 조회
                Vector2 origin = body.position; // 키 입력 전 위치 보관
                keyboard.MakeCurrent(); // 게임의 현재 장치로 시험 키보드 선택
                InputState.Change(keyboard, new KeyboardState(Key.D), InputUpdateType.Dynamic); // 게임 프레임의 가상 키보드에 D 키 누름 기록
                double blockedUntil = Time.fixedTimeAsDouble + 2d * Time.fixedDeltaTime; // 두 번의 물리 갱신 목표 시각
                double deadline = Time.realtimeSinceStartupAsDouble + 10d; // 물리 갱신 중단 감지 시각
                while (Time.fixedTimeAsDouble < blockedUntil && Time.realtimeSinceStartupAsDouble < deadline) // 이동 차단 중 물리 처리 대기
                { // 물리 갱신 대기 시작
                    yield return null; // 다음 프레임 대기
                } // 물리 갱신 대기 종료
                Assert.GreaterOrEqual(Time.fixedTimeAsDouble, blockedUntil); // 물리 시간이 실제로 진행됐는지 확인
                Assert.AreSame(keyboard, Keyboard.current); // 플레이어가 읽는 키보드 확인
                Assert.Less(Vector2.Distance(origin, body.position), 0.01f); // 창이 열린 동안 제자리 유지 확인
                yield return WaitForButton(relics.transform, "Overlay/Window/Close"); // 유물 창 닫기 버튼 렌더링 대기
                Click(relics.transform, "Overlay/Window/Close"); // 좌표 클릭으로 유물 창 닫기
                Assert.IsFalse(relics.transform.Find("Overlay").gameObject.activeInHierarchy); // 실제 유물 창 숨김 확인
                Assert.IsFalse(ExplorationPlayerController.InputBlocked); // 유물 창 닫은 뒤 이동 잠금 해제 확인
                deadline = Time.realtimeSinceStartupAsDouble + 10d; // 이동 재개 감지 시각
                while (body.position.x < origin.x + 0.04f && Time.realtimeSinceStartupAsDouble < deadline) // 누른 키로 오른쪽 이동 대기
                { // 오른쪽 이동 대기 시작
                    yield return null; // 다음 프레임 대기
                } // 오른쪽 이동 대기 종료
                InputState.Change(keyboard, new KeyboardState(), InputUpdateType.Dynamic); // 게임 프레임의 가상 키보드에서 D 키 해제
                Assert.GreaterOrEqual(body.position.x, origin.x + 0.04f, $"이동 복구 실패 (이동 잠금 {ExplorationPlayerController.InputBlocked}, 물리 몸체 {body.bodyType}, 물리 활성 {body.simulated})"); // 창을 닫은 뒤 실제 오른쪽 이동 확인
                Assert.IsTrue(ExplorationSessionManager.Instance.CompleteExplorationSuccess()); // 복귀에 필요한 탐사 성공 정산 실행
                deadline = Time.realtimeSinceStartupAsDouble + 10d; // 정산 화면 생성 중단 감지 시각
                GameObject result; // 탐사 정산 화면 보관
                while ((result = GameObject.Find("ExplorationResult")) == null) // 게임 갱신에서 생성되는 정산 화면 확인
                { // 정산 화면 대기 시작
                    Assert.Less(Time.realtimeSinceStartupAsDouble, deadline, "탐사 성공 후 정산 화면 준비 시간 초과"); // 정산 화면 생성 지연 확인
                    yield return null; // 정산 화면을 만드는 게임 프레임 대기
                } // 정산 화면 대기 종료
                yield return WaitForButton(result.transform, "ReturnButton"); // 정산 복귀 버튼의 렌더링 대기
                start = Time.realtimeSinceStartupAsDouble; // 복귀 버튼 누르기 직전 시각 기록
                Click(result.transform, "ReturnButton"); // 실제 정산 화면에서 거점 복귀
                AssertLoading(); // 로비 전환 중 로딩 화면 확인
                yield return WaitForScreen<LobbyLayoutView>("20_Lobby", "ExpeditionButton"); // 새 로비와 탐사 버튼 준비 대기
                Report(cycle, "lobby", start, baseline); // 로비 진입 시간과 기준 대비 메모리 기록
            } // 한 차례 왕복 종료
            Click(Object.FindFirstObjectByType<LobbyLayoutView>().transform, "ExpeditionButton"); // 마지막 복귀 직후 탐사 지역 다시 열기
            Assert.IsNotNull(Object.FindFirstObjectByType<ExpeditionMapView>()); // 세 번째 복귀 후 로비 입력 확인
        } // 반복 측정 종료
        finally // 검사 결과와 관계없는 장치 정리
        { // 장치 정리 시작
            InputSystem.RemoveDevice(keyboard); // 시험용 키보드 제거
        } // 장치 정리 종료
        yield return new ExitPlayMode(); // 실제 게임 실행 종료
    } // 왕복 검사 종료

    private static IEnumerator WaitForScreen<T>(string scene, string buttonPath = null) where T : Component // 씬 전환과 실제 화면 준비 대기
    { // 화면 준비 확인 시작
        double deadline = Time.realtimeSinceStartupAsDouble + 60d; // 멈춘 씬 전환 감지 시각
        while (SceneManager.GetActiveScene().name != scene || SceneFlowManager.Instance == null || SceneFlowManager.Instance.IsLoadingScene || Object.FindFirstObjectByType<T>() == null) // 대상 씬과 화면 준비 확인
        { // 화면 준비 대기 시작
            Assert.Less(Time.realtimeSinceStartupAsDouble, deadline, scene + " 화면 준비 시간 초과"); // 무한 대기 방지
            yield return null; // 다음 프레임 대기
        } // 화면 준비 대기 종료
        yield return null; // 이전 로딩 화면 파괴와 새 화면 첫 갱신 대기
        Assert.IsEmpty(Object.FindObjectsByType<LoadingScreenView>(FindObjectsInactive.Include, FindObjectsSortMode.None)); // 숨겨진 로딩 화면까지 제거 확인
        if (buttonPath != null) // 도착 화면의 입력 준비 확인 여부
        { // 버튼 준비 검사 시작
            yield return WaitForButton(Object.FindFirstObjectByType<T>().transform, buttonPath); // 대상 버튼의 실제 표시 대기
        } // 버튼 준비 검사 종료
    } // 화면 준비 확인 종료

    private static IEnumerator WaitForButton(Transform parent, string path) // 새 화면에서 버튼이 실제로 그려질 때까지 대기
    { // 버튼 표시 대기 시작
        Transform target = parent.Find(path); // 생성된 버튼 객체 조회
        Assert.IsNotNull(target, path); // 버튼 경로 확인
        Graphic graphic = target.GetComponent<Button>().targetGraphic; // 버튼의 입력 대상 그래픽 조회
        Assert.IsNotNull(graphic, path + " 버튼 그래픽 누락"); // 버튼 그래픽 연결 확인
        double deadline = Time.realtimeSinceStartupAsDouble + 10d; // 그려지지 않는 버튼 감지 시각
        while (graphic.depth < 0) // 레이캐스트 대상에서 제외되는 미표시 상태 확인
        { // 첫 렌더링 대기 시작
            Assert.Less(Time.realtimeSinceStartupAsDouble, deadline, $"{path} 버튼 렌더링 시간 초과 (부모 {graphic.GetComponentInParent<Canvas>()?.name}, 씬 {graphic.gameObject.scene.name})"); // 입력 불가능한 화면 진단
            yield return null; // 실제 화면 렌더링 프레임 대기
        } // 첫 렌더링 대기 종료
    } // 버튼 표시 대기 종료

    private static void AssertLoading() // 클릭 직후 로딩 상태 확인
    { // 로딩 표시 확인 시작
        Assert.IsTrue(SceneFlowManager.Instance.IsLoadingScene); // 전환 진행 상태 확인
        Assert.IsNotNull(Object.FindFirstObjectByType<LoadingScreenView>()); // 활성 로딩 화면 확인
    } // 로딩 표시 확인 종료

    private static void Click(Transform parent, string path) // 버튼 중앙의 실제 화면 입력 전달
    { // 화면 클릭 시작
        Canvas.ForceUpdateCanvases(); // 클릭 전 화면 좌표와 격자 배치 갱신
        Transform target = parent.Find(path); // 화면 안에서 대상 버튼 찾기
        Assert.IsNotNull(target, path); // 버튼 존재 확인
        Button button = target.GetComponent<Button>(); // 실제 클릭 버튼 조회
        Assert.IsTrue(button.isActiveAndEnabled && button.IsInteractable(), path); // 버튼의 입력 허용 확인
        EventSystem events = EventSystem.current; // 현재 씬의 입력 시스템 조회
        Assert.IsNotNull(events, path); // 입력 시스템 존재 확인
        Assert.IsNotNull(events.currentInputModule, path); // 활성 입력 모듈 연결 확인
        RectTransform rect = button.GetComponent<RectTransform>(); // 버튼 화면 영역 조회
        PointerEventData pointer = new PointerEventData(events); // 왼쪽 클릭 위치 정보 생성
        pointer.position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)); // 화면 고정 버튼의 중앙 좌표 계산
        List<RaycastResult> hits = new List<RaycastResult>(); // 포인터 아래의 화면 요소 목록 생성
        events.RaycastAll(pointer, hits); // 실제 화면 순서로 클릭 대상 검색
        Graphic graphic = button.targetGraphic; // 버튼 입력에 사용하는 실제 화면 요소 조회
        Assert.IsNotNull(graphic, path + " 버튼 그래픽 누락"); // 입력 대상 그래픽 연결 확인
        Canvas canvas = graphic.canvas; // 실제 버튼이 속한 캔버스 조회
        Assert.IsNotNull(canvas, $"{path} 활성 캔버스 없음 (부모 {button.GetComponentInParent<Canvas>()?.name}, 씬 {button.gameObject.scene.name})"); // 버튼이 보이는 화면에 연결됐는지 확인
        Assert.IsNotEmpty(hits, $"{path} 클릭 대상 없음 (좌표 {pointer.position}, 화면 {Screen.width}×{Screen.height}, 캔버스 {canvas.name}, 표시 영역 {canvas.pixelRect}, 깊이 {graphic.depth})"); // 빈 입력 결과의 화면 배치 확인
        GameObject hit = hits[0].gameObject; // 가장 앞에 있는 클릭 대상 조회
        Assert.AreEqual(button.gameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), path + " 클릭을 가로챈 화면 " + hit.name); // 다른 화면의 입력 가로채기 확인
        ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler); // 가장 앞의 대상에 클릭 전달
    } // 화면 클릭 종료

    private static void Report(int cycle, string destination, double start, long baseline) // 회차별 시간과 메모리 기록
    { // 측정 기록 시작
        double elapsedMs = (Time.realtimeSinceStartupAsDouble - start) * 1000d; // 클릭부터 화면 준비까지의 밀리초 계산
        long allocated = Profiler.GetTotalAllocatedMemoryLong(); // 현재 유니티 할당 바이트 측정
        Debug.Log($"[Day194] cycle={cycle} destination={destination} elapsedMs={elapsedMs:F1} allocatedBytes={allocated} deltaFromInitialLobbyBytes={allocated - baseline}"); // 회차와 기준 로비 대비 할당량 기록
    } // 측정 기록 종료
} // 반복 전환 검사 종료
