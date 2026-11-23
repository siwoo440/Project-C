using System.Collections; // 실제 탐사 화면 대기 사용
using NUnit.Framework; // 아이템 화면 검증 사용
using TMPro; // 아이템 이름 안내 검사 사용
using UnityEditor; // 실제 포션 에셋 조회 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.SceneManagement; // 실제 탐사 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 아이템 칸 검사 사용
public sealed class ExplorationInventoryViewTests // 탐사 아이템 화면 검사
{ // 탐사 아이템 화면 검사 시작
    [UnityTest] // 실제 탐사 화면 검사 지정
    public IEnumerator ExplorationShowsActualItemsInFiveByTwoGrid() // 실제 소모품 보관함과 화면 연결 확인
    { // 실제 탐사 화면 검사 시작
        yield return new EnterPlayMode(); // 실제 게임 플레이 시작
        SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 대기
        { // 시작 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 시작 메뉴 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 실제 시작 메뉴 확인
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 거점 진입
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 화면 대기
        { // 거점 화면 대기 시작
            yield return null; // 다음 프레임 대기
        } // 거점 화면 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<LobbyLayoutView>()); // 실제 거점 확인
        PartyDepartureTestSupport.StartExploration(0, 1, 2, 3); // 실제 파티로 탐사 시작
        for (float end = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < end && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 화면 대기
        { // 탐사 화면 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 화면 대기 종료
        Assert.IsNotNull(Object.FindFirstObjectByType<ExplorationPlayerController>()); // 실제 탐사 플레이어 확인
        GameObject hud = GameObject.Find("ExplorationPrototypeHUD"); // 실제 탐사 화면 조회
        Assert.IsNotNull(hud); // 실제 탐사 화면 존재 확인
        Assert.IsNull(hud.transform.Find("Instructions")); // 기존 시험용 안내 문구 제거 확인
        ExplorationInventoryView view = hud.GetComponentInChildren<ExplorationInventoryView>(); // 실제 탐사 아이템 화면 조회
        Assert.IsNotNull(view); // 탐사 아이템 화면 존재 확인
        Transform slots = view.transform.Find("Slots"); // 실제 아이템 칸 영역 조회
        Assert.IsNotNull(slots); // 아이템 칸 영역 존재 확인
        Assert.AreEqual(10, slots.childCount); // 가로 다섯 칸과 세로 두 줄 확인
        Assert.AreEqual(5, slots.GetComponent<GridLayoutGroup>().constraintCount); // 아이템 칸 가로 다섯 칸 확인
        ConsumableInventoryRuntime inventory = ConsumableRunManager.EnsureInstance().Inventory; // 실제 회차 소모품 보관함 조회
        inventory.Clear(); // 테스트용 기존 소모품 초기화
        string[] names = { "POTION_TEST_HEAL", "POTION_TEST_MENTAL", "POTION_TEST_BOMB" }; // 실제 시험용 포션 데이터 이름
        for (int index = 0; index < names.Length; index++) // 실제 포션 세 종류 순회
        { // 포션 획득 화면 검사 시작
            PotionData potion = AssetDatabase.LoadAssetAtPath<PotionData>("Assets/_ProjectC/Data/Consumables/" + names[index] + ".asset"); // 실제 포션 데이터 조회
            Assert.IsNotNull(potion); // 실제 포션 데이터 존재 확인
            Assert.IsNotNull(potion.Icon); // 생성한 포션 아이콘 연결 확인
            Assert.IsTrue(inventory.TryAcquire(potion, out int slot)); // 실제 소모품 획득 확인
            Assert.AreEqual(index, slot); // 획득 순서에 따른 아이템 칸 확인
            Image icon = slots.GetChild(index).Find("ItemIcon").GetComponent<Image>(); // 실제 획득 아이템 이미지 조회
            Assert.IsTrue(icon.enabled); // 획득 아이템 이미지 표시 확인
            Assert.AreSame(potion.Icon, icon.sprite); // 실제 보유 포션의 이미지 표시 확인
        } // 포션 획득 화면 검사 종료
        ExplorationInventorySlotView first = slots.GetChild(0).GetComponent<ExplorationInventorySlotView>(); // 첫 번째 아이템 칸 조회
        first.OnPointerEnter(null); // 첫 번째 아이템 마우스 진입
        TMP_Text tooltip = view.transform.Find("Tooltip").GetComponentInChildren<TMP_Text>(); // 실제 아이템 이름 안내 조회
        Assert.IsTrue(tooltip.gameObject.activeInHierarchy); // 보유 아이템 이름 안내 표시 확인
        StringAssert.Contains("체력", tooltip.text); // 실제 보유 포션 이름 표시 확인
        first.OnPointerExit(null); // 첫 번째 아이템 마우스 이탈
        Assert.IsFalse(tooltip.gameObject.activeInHierarchy); // 아이템 이탈 시 이름 안내 숨김 확인
        Assert.IsTrue(inventory.TryMoveOrSwap(0, 9)); // 실제 보관함 아이템 이동 확인
        Assert.IsFalse(slots.GetChild(0).Find("ItemIcon").GetComponent<Image>().enabled); // 이동 후 기존 아이템 칸 비움 확인
        Assert.IsTrue(slots.GetChild(9).Find("ItemIcon").GetComponent<Image>().enabled); // 이동한 아이템의 새 위치 표시 확인
        Assert.IsTrue(inventory.TryRemoveAt(9)); // 실제 보관함 아이템 삭제 확인
        Assert.IsFalse(slots.GetChild(9).Find("ItemIcon").GetComponent<Image>().enabled); // 삭제 후 아이템 칸 비움 확인
        yield return new ExitPlayMode(); // 실제 게임 플레이 종료
    } // 실제 탐사 화면 검사 종료
} // 탐사 아이템 화면 검사 종료
