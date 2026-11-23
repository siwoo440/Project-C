using System; // 실제 설비 종류와 원래 레벨 복구
using System.Collections; // 실제 플레이 모드 입력에 필요한 코루틴
using NUnit.Framework; // 선택, 비용 및 장착 자원 검사
using TMPro; // 선택 설비의 한글 문구
using UnityEngine; // 실제 Unity 화면과 원화 컴포넌트
using UnityEngine.TestTools; // 플레이 모드에서 실제 UI 호출
using UnityEngine.UI; // 가로 목록과 실제 설비 선택 버튼
using Object = UnityEngine.Object; // 검증 전용 화면 객체의 수명 정리

public sealed class FacilityBlueprintViewTests // 설비 도면과 가로 선택의 동기화 검사
{ // 실제 설비 화면의 회귀 검사 시작
    [UnityTest] // 두 실제 입력이 동일한 강화 대상 및 자원을 갱신하는 흐름
    public IEnumerator BlueprintAndRibbonSelectTheSameNativeUpgrade() // 도면 원화와 현재 효과 및 강화 비용의 실제 확인
    { // 독립 설비 창과 실제 자원 준비 시작
        yield return new EnterPlayMode(); // 실제 Unity 로비에 쓰이는 자원 관리자의 실행 환경
        var root = new GameObject("FacilityBlueprintTestCanvas", typeof(RectTransform), typeof(Canvas)); // 임시 설비 창의 실제 화면 부모
        var resources = PlayerResourceManager.EnsureInstance(); // 기존 로비의 실제 영구 자원 관리자
        var facilities = FacilityUpgradeManager.EnsureInstance(); // 기존 실제 설비 강화 규칙 관리자
        var originalResources = (resources.Gold, resources.Screw, resources.IronPlate, resources.Wire); // 테스트 후 돌려놓을 원래 자원
        FacilityType[] types = (FacilityType[])Enum.GetValues(typeof(FacilityType)); // 새 디자인에 반영할 실제 열 설비
        var originalLevels = Array.ConvertAll(types, facilities.GetLevel); // 실제 기존 플레이어의 설비별 단계 보관
        try // 실제 원본 게임 상태를 별도로 보호할 검증 구간
        { // 실제 두 선택 경로의 비교 시작
            facilities.ResetFacilities(); // 실제 모든 설비의 시작 단계 재현
            resources.RestoreResources(100, 20, 20, 20); // 기존 강화가 가능한 실제 자원 지급
            int closeCount = 0; // 닫기 알림의 실제 호출 횟수
            var overlay = FacilityUpgradePanel.Create(root.GetComponent<Canvas>(), () => closeCount++); // 원래 설비 모달과 닫힘 알림 연결
            var panel = overlay.transform.Find("FacilityPanel"); // 실제 기존 최상위 설비 패널
            var header = (RectTransform)panel.Find("Resources"); // 우측 상단의 네 자원 영역
            var resourceFrame = header.GetComponent<Image>(); // 네 자원에 한 번 연결하는 탐사 배경
            Assert.IsNotNull(resourceFrame); // 네 자원 뒤에 실제 이미지 컴포넌트가 있는지 확인
            Assert.AreSame(Resources.Load<Texture2D>("UI/Lobby/ExplorationResourceFrame"), resourceFrame.sprite.texture); // 기존 탐사 화면의 정확한 자원 프레임 재사용 확인
            Assert.IsFalse(resourceFrame.raycastTarget); // 자원 배경의 마우스 입력 통과 확인
            Assert.GreaterOrEqual(header.anchorMin.x, 0.58f); // 표시 기준의 우측 상단 시작 위치 확인
            Assert.AreEqual(4, header.childCount); // 하나의 자원 프레임 안에 네 칸 확인
            string[] resourceNames = { "Gold", "Screw", "Iron", "Wire" }; // 사용자가 보는 네 자원 순서
            string[] initialValues = { "100", "20", "20", "20" }; // 기존 보유 자원의 실제 표시 수량
            for (int index = 0; index < resourceNames.Length; index++) // 같은 프레임 안의 네 자원 표시 순회
            { // 자원 순서와 카운터 검증 시작
                var slot = header.GetChild(index); // 원래 순서의 개별 자원 칸 조회
                Assert.AreEqual("Resource_" + resourceNames[index], slot.name); // 네 칸의 실제 자원 순서 확인
                Assert.AreSame(Resources.Load<Texture2D>("UI/Lobby/" + resourceNames[index]), slot.Find("Icon").GetComponent<Image>().sprite.texture); // 기존 자원 아이콘의 정확한 참조 확인
                Assert.AreEqual(initialValues[index], slot.Find("Value").GetComponent<TMP_Text>().text); // 초기 현재 보유 자원의 화면 수량 확인
            } // 자원 순서와 카운터 검증 종료
            var close = panel.Find("CloseButton").GetComponent<Button>(); // 우측 상단의 실제 닫기 버튼
            Assert.Less(header.anchorMax.x, ((RectTransform)close.transform).anchorMin.x); // 자원 프레임과 X 버튼 간 여백 확인
            Assert.AreSame(Resources.Load<Texture2D>("UI/Lobby/Button"), close.image.sprite.texture); // 기존 거점 버튼 이미지 재사용 확인
            Assert.AreEqual("X", close.GetComponentInChildren<TMP_Text>().text); // 닫기 버튼의 X 표시 확인
            var board = panel.Find("Blueprint/Board"); // 원본 도면과 좌표를 공유하는 실제 보드
            Assert.IsNotNull(board.Find("BlueprintTexture").GetComponent<RawImage>().texture); // 실제 별도 기술 도면의 리소스 연결
            var nodes = board.Find("BlueprintNodes"); // 도면에 표시되는 실제 열 설비의 부모
            string[] ribbonPath = { "FacilityList", "Viewport", "Content" }; // 기존 설비 목록의 실제 호환 경로
            var ribbon = panel.Find(string.Join("/", ribbonPath)); // 열 개의 가로 설비 선택 영역
            Assert.AreEqual(10, nodes.childCount); // 실제 도면에 열 설비 선택점
            Assert.AreEqual(10, ribbon.childCount); // 가로 목록에 같은 열 설비 선택점
            Assert.IsTrue(panel.Find("FacilityList").GetComponent<ScrollRect>().horizontal); // 가로 전용 실제 목록 탐색
            foreach (FacilityType type in types) // 각 설비의 실제 독립 그림과 같은 고유 버튼 확인
            { // 같은 설비 식별자의 두 진입점 검사 시작
                var pin = nodes.Find("Blueprint_" + type); // 실제 도면 안의 클릭 조작
                Assert.IsTrue(pin.GetComponent<Button>().interactable); // 각 설비에 실제 접근하는 포인터 조작
                var frame = pin.Find("Frame").GetComponent<RawImage>(); // 독립 빨간 설비 위치 프레임
                Assert.IsNotNull(frame.texture); // 실제 투명 프레임 원화 사용
                Assert.IsFalse(frame.raycastTarget); // 프레임 그림이 설비 클릭을 막지 않는지 확인
                var illustration = pin.Find("Illustration").GetComponent<RawImage>(); // 각 종류의 독립 설비 그림
                Assert.AreSame(Resources.Load<Texture2D>("UI/Facility/Facilities/" + type), illustration.texture); // 실제 설비별 게임 리소스 연결
                Assert.IsNotNull(illustration.texture); // 독립 원화의 실제 임포트 확인
                Assert.IsFalse(illustration.raycastTarget); // 설비 이미지가 실제 장치 버튼 입력을 막지 않는지 확인
            } // 같은 설비 식별자의 두 진입점 검사 종료
            var defensePin = nodes.Find("Blueprint_DefenseBarrier"); // 원하는 방어 설비로 이동하는 도면 조작
            defensePin.GetComponent<Button>().onClick.Invoke(); // 도면에서 실제 방어 차폐 장치 선택
            var detail = panel.Find("Detail"); // 실제 오른쪽 상세 영역
            Assert.AreEqual("방어 차폐 장치", detail.Find("FacilityName").GetComponent<TMP_Text>().text); // 실제 설비의 이름
            StringAssert.Contains("피해", detail.Find("Description").GetComponent<TMP_Text>().text); // 선택한 방어 설비의 구체적인 설명
            StringAssert.Contains("철판 10", detail.Find("Cost").GetComponent<TMP_Text>().text); // 실제 원래 다음 구매 비용 표시
            Assert.IsTrue(ribbon.Find("Facility_DefenseBarrier").GetComponent<Outline>().enabled); // 도면을 누르면 실제 아래 선택도 함께 표시
            ribbon.Find("Facility_PowerSupply").GetComponent<Button>().onClick.Invoke(); // 가로 목록에서 다른 설비의 실제 진입
            Assert.AreEqual("전력 공급기", detail.Find("FacilityName").GetComponent<TMP_Text>().text); // 두 번째 입력 방식의 상세 갱신
            Assert.IsFalse(ribbon.Find("Facility_DefenseBarrier").GetComponent<Outline>().enabled); // 이전 선택 강조 해제 확인
            defensePin.GetComponent<Button>().onClick.Invoke(); // 실제 방어 설비의 다음 강화를 다시 선택
            var upgrade = detail.Find("UpgradeButton").GetComponent<Button>(); // 기존 게임이 사용하는 실제 강화 버튼
            upgrade.onClick.Invoke(); // 실제 재화 차감과 방어 설비 강화
            Assert.AreEqual(1, facilities.GetLevel(FacilityType.DefenseBarrier)); // 기존 원래 강화 관리자에 같은 단계 반영
            Assert.AreEqual(15, resources.Screw); // 실제 지급 비용의 나사 차감
            Assert.AreEqual(10, resources.IronPlate); // 실제 지급 비용의 철판 차감
            Assert.AreEqual("15", header.Find("Resource_Screw/Value").GetComponent<TMP_Text>().text); // 기존 헤더 나사 수량의 즉시 갱신 확인
            Assert.AreEqual("10", header.Find("Resource_Iron/Value").GetComponent<TMP_Text>().text); // 기존 헤더 철판 수량의 즉시 갱신 확인
            Assert.IsFalse(upgrade.interactable); // 원래 다음 단계 철판 부족 처리
            facilities.RestoreFacilityLevel(FacilityType.DefenseBarrier, FacilityUpgradeManager.MaximumLevel); // 원래 최대 단계 진행 조건의 실제 구성
            StringAssert.Contains("최대 단계", detail.Find("NextEffect").GetComponent<TMP_Text>().text); // 기존 최대 단계의 한글 안내
            Assert.IsFalse(upgrade.interactable); // 실제 최대 단계 강화 차단
            close.onClick.Invoke(); // 실제 X 버튼의 닫기 동작 실행
            Assert.IsFalse(overlay.gameObject.activeSelf); // 닫기 직후 설비 창과 입력 비활성 확인
            Assert.AreEqual(1, closeCount); // 원래 로비의 닫힘 알림 한 번 확인
        } // 실제 두 선택 경로의 비교 종료
        finally // 실제 플레이어의 원래 상태 및 테스트 모달 복원
        { // 검사 전 자원과 각 설비 단계 복원 시작
            for (int index = 0; index < types.Length; index++) // 원래 열 설비의 단계 복원
            { // 실제 해당 설비 하나의 원래 기록 적용 시작
                facilities.RestoreFacilityLevel(types[index], originalLevels[index]); // 기존 원래 단계와 상태 변경 알림
            } // 실제 해당 설비 하나의 원래 기록 적용 종료
            resources.RestoreResources(originalResources.Gold, originalResources.Screw, originalResources.IronPlate, originalResources.Wire); // 실제 기존 지갑대로 복원
            Object.Destroy(root); // 이 검사에서 만든 독립 설비 화면 제거
        } // 검사 전 자원과 각 설비 단계 복원 종료
        yield return new ExitPlayMode(); // 실제 Unity 원래 모드 복귀
    } // 독립 설비 창과 실제 자원 준비 종료
} // 실제 설비 화면의 회귀 검사 종료
