using System.Collections; // 실제 탐사 화면의 프레임과 플레이 모드 전환
using System.Globalization; // 화면 골드의 고정 숫자 형식 비교
using NUnit.Framework; // 실제 지도 핀과 잠금 상태 검증
using TMPro; // 살아 있는 화면 문구 컴포넌트 확인
using UnityEngine; // 검증용 실제 캔버스 및 화면 객체
using UnityEngine.TestTools; // Unity의 실제 입출력 검증 코루틴
using UnityEngine.UI; // 실제 버튼과 지도 그림 및 진행률 확인

public sealed class ExpeditionWorldMapViewTests // 월드맵의 실제 지역 선택과 도움말 검사
{ // 월드맵의 기존 흐름에서 빠진 선택 확인 시작
    [UnityTest] // 실제 8개 지역과 잠금 선택 및 골드 상태 확인
    public IEnumerator PresentsRealRegionsLockedSelectionLiveGoldAndModalHelp() // 잠긴 핀을 선택해 조건을 보면서 출발을 막는 실제 흐름
    { // 실제 지도 UI 검증의 준비 시작
        yield return new EnterPlayMode(); // 게임과 같은 Unity 플레이 모드에서 지도 생성
        var root = new GameObject("WorldMapViewTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); // 독립 월드맵 검사의 실제 캔버스
        var canvas = root.GetComponent<Canvas>(); // 화면 표시 부모의 실제 컴포넌트
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 로비와 같은 오버레이 표시
        var resources = PlayerResourceManager.EnsureInstance(); // 실제 로비가 사용하는 골드 자원 관리자
        int initialGold = resources.Gold; // 검증 후 복원할 실제 당시 자원 값
        var initialProgress = ExpeditionZoneProgress.Capture(); // 기존 실제 지역 진행을 별도로 보관
        try // 독립 화면과 골드 값의 변경 수명 구분
        { // 실제 화면 상태 검증 시작
            ExpeditionZoneProgress.Reset(); // 첫 실제 탐사의 잠금 조건을 독립 검사에 재현
            ExpeditionZoneProgress.RecordBattleVictory(ExpeditionZoneId.WinterfallAshPlains, BattleType.Normal); // 실제 일반 승리로 윈터폴에 양수 진행률 기록
            var map = ExpeditionMapView.Open(canvas); // 로비의 실제 기본 월드맵 진입점
            Assert.IsNotNull(map); // 새 월드맵 생성 확인
            var art = map.transform.Find("WorldMap").GetComponent<RawImage>(); // 실제 월드맵의 리소스를 표시하는 원본 컴포넌트
            Assert.IsNotNull(art.texture); // 본 화면에서 지도 그림의 실제 임포트 확인
            Assert.AreEqual(16f / 9f, art.rectTransform.rect.width / art.rectTransform.rect.height, 0.025f); // 화면 비율과 무관한 그림의 실제 원래 비율
            Assert.AreEqual(8, ExpeditionZones.All.Count); // 원본에 정식 등록한 실제 지역 수
            for (int index = 0; index < ExpeditionZones.All.Count; index++) // 공개된 실제 지도 지역 순회
            { // 개별 지도 버튼 상태 확인 시작
                var definition = ExpeditionZones.All[index]; // 원본 지역 이름과 진행 수치의 참조
                var pin = map.transform.Find("MapOption_" + index); // 실제 원본 각 지역의 독립 핀 입력
                Assert.IsTrue(pin.GetComponent<Button>().interactable); // 잠금 여부와 무관하게 모든 지역의 정보 표시
                Assert.AreEqual(definition.DisplayName, pin.Find("RegionName").GetComponent<TMP_Text>().text); // 실제 정의와 같은 지역 이름
                Assert.AreEqual(ExpeditionZoneProgress.GetPercent(definition.Id) / 100f, ((RectTransform)pin.Find("ProgressGauge/Fill")).anchorMax.x, 0.001f); // 실제 지역 진행률을 기준으로 그려진 채움 폭
            } // 개별 지도 버튼 상태 확인 종료
            var confirm = map.transform.Find("ConfirmButton").GetComponent<Button>(); // 기존 월드맵의 실제 편성 진입 버튼
            Assert.IsFalse(confirm.interactable); // 처음 지도에는 아직 선택한 지역 없음
            var firstPin = map.transform.Find("MapOption_0").GetComponent<Button>(); // 최초 윈터폴의 실제 핀
            firstPin.onClick.Invoke(); // 게임에서 실제 선택과 상세 영역 생성
            Assert.IsTrue(confirm.interactable); // 기본 해금 지역의 실제 편성 가능 상태
            int lockedIndex = ExpeditionZones.All.Count - 1; // 새 로비에서 진행도가 필요한 최종 지역의 실제 위치
            var locked = ExpeditionZones.All[lockedIndex]; // 잠금 조건 원래 정의
            Assert.IsFalse(ExpeditionZoneProgress.IsUnlocked(locked.Id)); // 아직 탐사로 진입하지 않은 정식 잠금 조건
            map.transform.Find("MapOption_" + lockedIndex).GetComponent<Button>().onClick.Invoke(); // 잠긴 실제 핀에서도 정보 표시
            Assert.AreEqual(locked.DisplayName, map.transform.Find("RegionDock/SelectedName").GetComponent<TMP_Text>().text); // 새 상세 화면의 실제 선택 지역
            StringAssert.Contains(locked.UnlockDescription, map.transform.Find("RegionDock/Gate").GetComponent<TMP_Text>().text); // 실제 정의가 제공한 잠금 해제 조건
            Assert.IsFalse(confirm.interactable); // 지역 정보 확인과 실제 편성 진행을 구분
            var goldText = map.transform.Find("TopBar/GoldPlate/Gold").GetComponent<TMP_Text>(); // 실제 출발화면 상단의 플레이어 골드
            resources.AddResources(7, 0, 0, 0); // 실제 자원 변경 이벤트 발생
            Assert.AreEqual(resources.Gold.ToString("N0", CultureInfo.InvariantCulture), goldText.text); // 실시간 증가한 실제 골드
            var help = map.transform.Find("TopBar/HelpButton").GetComponent<Button>(); // 지도 전용 도움말 버튼 조회
            help.onClick.Invoke(); // 현재 정보를 보존한 도움말 팝업 열기
            var popup = map.transform.Find("MapHelp"); // 실제 지도 전용 도움말 오버레이
            Assert.IsTrue(popup.gameObject.activeSelf); // 받는 실제 동작 후 팝업 표시
            Assert.IsFalse(firstPin.interactable); // 도움말을 보는 동안 지도 아래 입력 차단
            popup.Find("HelpPanel/CloseHelp").GetComponent<Button>().onClick.Invoke(); // 팝업 닫기 버튼의 실제 이용
            Assert.IsFalse(popup.gameObject.activeSelf); // 별도 플레이 진행 없이 즉시 지도 복귀
            Assert.IsTrue(firstPin.interactable); // 도움말 종료 뒤 원래 지도 조작 복원
            Assert.IsFalse(confirm.interactable); // 닫기 후 이전 잠금 지역을 그대로 선택
            map.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke(); // 기존 실제 로비 복귀 동작
            yield return null; // 새 UI 진입 전에 이전 지도 객체 제거 대기
            var levelProgress = ExpeditionZoneProgress.Capture(); // 진짜 저장 모델에서 높은 난이도가 열린 상태 복사
            levelProgress.zones[(int)ExpeditionZoneId.WinterfallAshPlains].highestLevel = 2; // 둘째 난이도를 이미 공략할 수 있는 실제 저장 상태
            ExpeditionZoneProgress.Restore(levelProgress); // 현재 지역의 정식 난이도 저장 반영
            var returned = ExpeditionMapView.Open(canvas, ExpeditionZoneId.WinterfallAshPlains, 20); // 실제 편성에서 돌아오는 지역과 난이도 입력
            var currentLevel = returned.transform.Find("RegionDock/LevelControls/CurrentLevel").GetComponent<TMP_Text>(); // 하단 실제 출발 난이도 조회
            var decrease = returned.transform.Find("RegionDock/LevelControls/DecreaseLevel").GetComponent<Button>(); // 실제 선택된 아래 난이도로 이동
            var increase = returned.transform.Find("RegionDock/LevelControls/IncreaseLevel").GetComponent<Button>(); // 실제 해금 범위에서 다음 난이도 이동
            Assert.AreEqual("난이도 2", currentLevel.text); // 돌아왔을 때 실제 최고 해금 난이도로 제한
            Assert.IsFalse(increase.interactable); // 실제 현재 최대 난이도 이상 입력 차단
            decrease.onClick.Invoke(); // 실게임에서 원래 한 단계 낮은 난이도 입력
            Assert.AreEqual("난이도 1", currentLevel.text); // 낮춘 실제 선택 난이도 반영
            Assert.IsFalse(decrease.interactable); // 원래 시작 단계보다 낮은 난이도 차단
        } // 실제 화면 상태 검증 종료
        finally // 검증의 실제 임시 자원 값과 화면 수명 정리
        { // 현재 골드와 독립 캔버스의 복원 시작
            ExpeditionZoneProgress.Restore(initialProgress); // 실제 검사 전 저장 지역 진행 복원
            resources.RestoreResources(initialGold, resources.Screw, resources.IronPlate, resources.Wire); // 원래 검증 전 플레이어 골드 복원
            Object.Destroy(root); // 검증에서 임시로 만든 지도 화면 제거
        } // 현재 골드와 독립 캔버스의 복원 종료
        yield return new ExitPlayMode(); // 실제 맵 선택 검사 환경 종료
    } // 실제 지도 UI 검증의 준비 종료
} // 월드맵의 기존 흐름에서 빠진 선택 확인 종료
