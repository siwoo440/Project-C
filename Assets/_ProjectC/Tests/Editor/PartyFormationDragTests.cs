using System.Collections; // 플레이 모드 검사 사용
using NUnit.Framework; // 편성 화면 검증 사용
using TMPro; // 편성 번호 문구 확인 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.EventSystems; // 실제 드래그 포인터 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 프로필 격자와 버튼 사용
public sealed class PartyFormationDragTests // 프로필 드래그 편성 검사
{ // 드래그 편성 검사 시작
    [UnityTest] // 실제 편성 화면 검사 지정
    public IEnumerator Profiles_DragToSparseSlotsAndRestoreNumbers() // 드래그와 번호 표시 확인
    { // 편성 화면 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        PartyPresetStore.Reset(); // 독립 편성 상태 초기화
        GameObject canvasObject = new GameObject("FormationDragCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 편성 캔버스 생성
        Object.DontDestroyOnLoad(canvasObject); // 씬 전환 중 테스트 캔버스 유지
        canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f); // 기준 화면 크기 설정
        GameObject eventsObject = new GameObject("FormationDragEvents", typeof(EventSystem)); // 독립 포인터 이벤트 생성
        Object.DontDestroyOnLoad(eventsObject); // 씬 전환 중 포인터 이벤트 유지
        PartyFormationView view = PartyFormationView.Open(canvasObject.GetComponent<Canvas>()); // 실제 편성 화면 생성
        Assert.IsNotNull(view); // 편성 화면 생성 확인
        yield return null; // 프로필 격자 크기 갱신 대기
        Transform root = view.transform; // 실제 편성 화면 조회
        Transform grid = root.Find("CharacterList/Viewport/Grid"); // 프로필 격자 조회
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>(); // 실제 격자 배치 조회
        Assert.AreEqual(3, layout.constraintCount); // 세 열 프로필 배치 확인
        Assert.Greater(layout.cellSize.x, 110f); // 기존보다 커진 프로필 확인
        Transform slots = root.Find("Slots"); // 실제 네 편성 칸 조회
        Assert.AreEqual("4", slots.Find("Slot_0/NumberBadge/Label").GetComponent<TMP_Text>().text); // 왼쪽 네 번째 번호 확인
        Assert.AreEqual("1", slots.Find("Slot_3/NumberBadge/Label").GetComponent<TMP_Text>().text); // 오른쪽 첫 번째 번호 확인
        PartyProfileDrag first = grid.Find("Character_0").GetComponent<PartyProfileDrag>(); // 첫 프로필 드래그 조회
        PartyProfileDrag second = grid.Find("Character_1").GetComponent<PartyProfileDrag>(); // 두 번째 프로필 드래그 조회
        Assert.IsNotNull(first); // 첫 프로필 드래그 기능 확인
        Assert.IsNotNull(second); // 두 번째 프로필 드래그 기능 확인
        PointerEventData pointer = new PointerEventData(eventsObject.GetComponent<EventSystem>()) { button = PointerEventData.InputButton.Left, position = Vector2.zero }; // 실제 드래그 포인터 생성
        first.OnBeginDrag(pointer); // 첫 프로필 드래그 시작
        Assert.IsNotNull(GameObject.Find("PartyProfileDragGhost")); // 드래그 초상화 생성 확인
        pointer.pointerCurrentRaycast = new RaycastResult { gameObject = slots.Find("Slot_0").gameObject }; // 왼쪽 네 번째 칸 드롭 설정
        first.OnEndDrag(pointer); // 네 번째 번호 칸에 드롭
        Assert.AreEqual("바보", slots.Find("Slot_0/Name").GetComponent<TMP_Text>().text); // 지정한 네 번째 칸 배치 확인
        Assert.AreEqual("빈 슬롯", slots.Find("Slot_3/Name").GetComponent<TMP_Text>().text); // 오른쪽 빈 칸 유지 확인
        Assert.AreEqual("4", grid.Find("Character_0/NumberBadge/Label").GetComponent<TMP_Text>().text); // 첫 프로필의 네 번째 번호 확인
        second.OnBeginDrag(pointer); // 두 번째 프로필 드래그 시작
        pointer.pointerCurrentRaycast = new RaycastResult { gameObject = slots.Find("Slot_3").gameObject }; // 오른쪽 첫 번째 칸 드롭 설정
        second.OnEndDrag(pointer); // 첫 번째 번호 칸에 드롭
        Assert.AreEqual("마법사", slots.Find("Slot_3/Name").GetComponent<TMP_Text>().text); // 지정한 첫 번째 칸 배치 확인
        Assert.AreEqual("1", grid.Find("Character_1/NumberBadge/Label").GetComponent<TMP_Text>().text); // 두 번째 프로필의 첫 번째 번호 확인
        first.OnBeginDrag(pointer); // 기존 프로필 다시 드래그
        pointer.pointerCurrentRaycast = new RaycastResult { gameObject = slots.Find("Slot_3").gameObject }; // 다른 캐릭터가 있는 칸 설정
        first.OnEndDrag(pointer); // 기존 편성 캐릭터끼리 자리 교환
        Assert.AreEqual("마법사", slots.Find("Slot_0/Name").GetComponent<TMP_Text>().text); // 기존 대상의 자리 교환 확인
        Assert.AreEqual("바보", slots.Find("Slot_3/Name").GetComponent<TMP_Text>().text); // 드래그한 캐릭터의 자리 교환 확인
        Assert.AreEqual("1", grid.Find("Character_0/NumberBadge/Label").GetComponent<TMP_Text>().text); // 교환된 첫 프로필 번호 확인
        Assert.AreEqual("4", grid.Find("Character_1/NumberBadge/Label").GetComponent<TMP_Text>().text); // 교환된 두 번째 프로필 번호 확인
        Object.DestroyImmediate(view.gameObject); // 기존 편성 화면 제거
        view = PartyFormationView.Open(canvasObject.GetComponent<Canvas>()); // 저장된 편성 화면 다시 열기
        Assert.AreEqual("마법사", view.transform.Find("Slots/Slot_0/Name").GetComponent<TMP_Text>().text); // 빈 칸 포함 네 번째 슬롯 복원 확인
        Assert.AreEqual("바보", view.transform.Find("Slots/Slot_3/Name").GetComponent<TMP_Text>().text); // 첫 번째 슬롯 복원 확인
        Assert.AreEqual("빈 슬롯", view.transform.Find("Slots/Slot_1/Name").GetComponent<TMP_Text>().text); // 가운데 빈 슬롯 복원 확인
        Object.DestroyImmediate(canvasObject); // 테스트 캔버스 정리
        Object.DestroyImmediate(eventsObject); // 이벤트 시스템 정리
        PartyPresetStore.Reset(); // 테스트 편성 초기화
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 편성 화면 검사 종료
} // 드래그 편성 검사 종료
