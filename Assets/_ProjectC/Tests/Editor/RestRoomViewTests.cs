using System.Collections; // 플레이 모드 코루틴 사용
using System.Collections.Generic; // 실제 회차 카드 항목 목록 사용
using System.Linq; // 시험 덱과 화면 요소 조회
using NUnit.Framework; // 화면 동작 검증
using TMPro; // 실제 회복 및 위험도 문구 조회
using UnityEngine; // 화면과 회차 객체 사용
using UnityEngine.EventSystems; // 휠 입력 전달
using UnityEngine.TestTools; // 플레이 모드 전환 사용
using UnityEngine.UI; // 카드 버튼과 스크롤 조회

public sealed class RestRoomViewTests // 휴식 화면의 실제 선택과 닫기 검증
{ // 휴식 화면 테스트 시작
    [UnityTest] // 실제 휴식 UI 동작 검증 등록
    public IEnumerator RestRoom_UsesExactDeckSlotAndRestoresTimeOnClose() // 같은 카드 중 선택한 복제본만 강화하는지 확인
    { // 실제 휴식 시험 시작
        yield return new EnterPlayMode(); // 런타임 화면 및 관리자 준비
        CharacterData owner = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 준비된 실제 파티원 조회
        CardData card = Resources.Load<CardData>("StarterCards/Attack"); // 강화할 실제 시작 카드 조회
        DeckData deck = DeckData.CreateRuntime(Enumerable.Repeat(new DeckCardEntry(card, owner), 12).ToArray()); // 원본이 같은 열두 덱 슬롯 준비
        PartyData party = PartyData.CreateRuntime(new[] { owner }); // 실제 휴식 대상 파티 구성
        GameObject host = new GameObject("RestRoomViewTest"); // 실제 화면 컴포넌트의 독립 객체 생성
        Object.DontDestroyOnLoad(host); // 초기 로딩 중 독립 시험 화면 보존
        float previousTimeScale = Time.timeScale; // 시험 이전 시간 배율 보관
        bool previousLanguage = ProjectCLanguage.UseEnglish; // 시험 이전 표시 언어 보관

        try // 배율과 시험 리소스 정리 보장
        { // 선택과 회복 검증 시작
            ProjectCLanguage.Apply(false); // 한국어 위험도 문구로 검사 고정
            BattleResultManager result = BattleResultManager.EnsureInstance(); // 영속 파티 상태 관리자 준비
            Assert.IsTrue(result.RegisterParty(party)); // 휴식 대상 파티 등록 확인
            result.RestoreSavedAllyState(owner, 1, 35, 0); // 회복을 확인할 체력과 정신력 기록
            RestRoomRunManager rest = RestRoomRunManager.EnsureInstance(); // 실제 휴식 관리자 조회
            rest.ResetSession(); // 기존 시험 회차 영향 제거
            Assert.IsTrue(rest.Prepare(deck)); // 휴식 대상 회차 덱 연결 확인
            RestRoomPrototypeView view = host.AddComponent<RestRoomPrototypeView>(); // 실제 탐사 호출과 같은 화면 생성
            view.Initialize(rest); // 실제 휴식 관리자 연결
            Time.timeScale = 0.65f; // 복구 여부를 구분할 시간 배율 설정
            view.OpenFromRoom(true); // 기존 고위험 호출로 실제 창 열기
            view.OpenFromRoom(1); // 모달 중복 호출로 위험도 변경 차단 확인
            Assert.AreEqual(0f, Time.timeScale); // 실제 휴식 창 일시 정지 확인
            Assert.AreEqual("퇴색 Lv2", FindChild<TMP_Text>(host, "RiskValue").text); // 기존 bool 위험도와 잠금 확인

            Button execute = FindChild<Button>(host, "ExecuteRest"); // 실제 휴식 실행 버튼 조회
            Assert.IsFalse(execute.interactable); // 카드를 고르기 전 실행 차단 확인
            ScrollRect scroll = FindChild<ScrollRect>(host, "CardScroll"); // 실제 카드 스크롤 조회
            Canvas.ForceUpdateCanvases(); // 휠 입력 전 클리핑 영역 확정
            Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height); // 긴 덱의 실제 스크롤 영역 검사
            PointerEventData wheel = new PointerEventData(null) { scrollDelta = Vector2.down }; // 목록을 내리는 휠 입력 구성
            ExecuteEvents.ExecuteHierarchy(scroll.viewport.gameObject, wheel, ExecuteEvents.scrollHandler); // 표시된 뷰포트의 휠 입력 실행
            Assert.Greater(scroll.content.anchoredPosition.y, 0f); // 실제 목록 이동 확인

            FindChild<Button>(host, "Card_10").onClick.Invoke(); // 원본이 같은 열한 번째 슬롯 직접 선택
            Assert.IsTrue(execute.interactable); // 유효한 카드 선택 후 휴식 허용 확인
            execute.onClick.Invoke(); // 실제 버튼을 통한 휴식 실행
            IReadOnlyList<RunDeckCardEntry> cards = RunDeckManager.EnsureInstance().Cards; // 영속 회차 덱 조회
            Assert.AreEqual(1, cards[10].UpgradeLevel); // 선택된 정확한 복제본의 강화 확인
            Assert.IsTrue(cards.Where((entry, index) => index != 10).All(entry => !entry.IsUpgraded)); // 나머지 복제본의 단계 유지 확인
            Assert.IsTrue(rest.IsUsed); // 실제 휴식 방 사용 소비 확인
            Assert.IsFalse(execute.interactable); // 휴식 후 같은 방의 실행 버튼 잠금 확인
            Assert.IsTrue(result.TryGetSavedAllyState(owner, out int hp, out int sanity, out _)); // 휴식 적용 뒤 실제 캐릭터 상태 조회
            Assert.AreEqual(RestRoomRecoveryService.CalculateRecoveredHealth(1, owner.MaxHealth, 2, RestRoomRecoveryService.CampRecoveryBonus, RestRoomRecoveryService.CampFlatHealthBonus), hp); // 고정 위험도 기준 HP 회복 확인
            Assert.AreEqual(35 + RestRoomRecoveryService.CampMentalRecovery, sanity); // 실제 영속 상태의 정신력 회복 확인

            FindChild<Button>(host, "CloseRest").onClick.Invoke(); // 닫기 버튼으로 실제 창 정리
            Assert.AreEqual(0.65f, Time.timeScale); // 모달 열기 직전의 시간 배율 복원 확인
            view.OpenFromRoom(0); // 사용 완료 상태의 새로운 표시 준비
            Assert.IsFalse(execute.interactable); // 기존 소비 상태와 새 선택 초기화 반영 확인
            host.SetActive(false); // 씬 해제와 같은 컴포넌트 비활성화 경로 실행
            Assert.AreEqual(0.65f, Time.timeScale); // 비활성화 경로의 기존 시간 배율 복원 확인
        } // 선택과 회복 검증 종료
        finally // 성공 및 실패 뒤의 테스트 정리
        { // 시험 리소스 정리 시작
            Object.DestroyImmediate(host); // 모달과 이벤트 연결 해제
            Object.DestroyImmediate(deck); // 독립 회차 카드 원본 제거
            Object.DestroyImmediate(party); // 독립 파티 제거
            Time.timeScale = previousTimeScale; // 시험 전 시간 배율 복구
            ProjectCLanguage.Apply(previousLanguage); // 시험 전 표시 언어 복구
        } // 시험 리소스 정리 종료

        yield return new ExitPlayMode(); // 시험 플레이 모드 종료
    } // 실제 휴식 시험 종료

    private static T FindChild<T>(GameObject root, string childName) where T : Component // 이름으로 실제 UI 구성 요소 조회
    { // 실제 구성 요소 검색 시작
        return root.GetComponentsInChildren<T>(true).Single(child => child.name == childName); // 일치하는 단일 화면 구성 반환
    } // 실제 구성 요소 검색 종료
} // 휴식 화면 테스트 종료
