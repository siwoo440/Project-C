using System.Collections; // 플레이 모드 단계별 검사 사용
using System.Linq; // 현재 아군 화면 검색 사용
using System.Reflection; // 비공개 전투 명단 경계 검사 사용
using NUnit.Framework; // 테스트 결과 확인 사용
using UnityEditor; // 실제 전투 유닛 프리팹 조회 사용
using UnityEngine; // 테스트 화면과 게임 객체 사용
using UnityEngine.EventSystems; // 실제 클릭과 드래그 이벤트 사용
using UnityEngine.TestTools; // 플레이 모드 전환 사용
using UnityEngine.UI; // 실제 선택 테두리 확인 사용
using Fight = WinterfallSwordCombatTests.Fight; // 기존 손패와 전투 구성 사용

public sealed class WinterfallRevivalCardTests // 생명의 빛 실전 입력과 부활 범위 검사
{ // 부활 검사 클래스 시작
    [UnityTest] // 실제 전투 화면 클릭 검사 지정
    public IEnumerator LightOfLife_ClickRestoresDeployedAllyWithoutReplayingCombatEvents() // 실제 클릭 부활과 전투 정산 확인
    { // 클릭 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 전투 화면 플레이 모드 진입
        yield return CheckClickRevival(); // 플레이 모드에서 별도 검사 실행
        yield return new ExitPlayMode(); // 전투 화면 플레이 모드 종료
    } // 클릭 플레이 모드 검사 종료

    [UnityTest] // 실제 전투 화면 드래그와 경계 검사 지정
    public IEnumerator LightOfLife_DragRequiresDeployedFallenAllyAndOngoingBattle() // 드래그와 현재 전투 참가자 범위 확인
    { // 드래그 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 전투 화면 플레이 모드 진입
        yield return CheckDragRevival(); // 플레이 모드에서 별도 검사 실행
        yield return new ExitPlayMode(); // 전투 화면 플레이 모드 종료
    } // 드래그 플레이 모드 검사 종료

    private static IEnumerator CheckClickRevival() // 부활 카드 두 장의 단일 클릭과 전투 흐름 확인
    { // 클릭 부활 검사 시작
        using (var fight = new Fight(CreateBattleView, CreateBattleView, "LIGHT_OF_LIFE", "LIGHT_OF_LIFE")) // 실제 아군 프리팹과 카탈로그 보물로 전투 구성
        { // 실제 전투 검사 시작
            BattleCardView card = fight.View("LIGHT_OF_LIFE"); // 실제 손패 생명의 빛 조회
            CardInstance queued = card.RuntimeCard; // 사용할 카드의 개별 인스턴스 저장
            Assert.AreEqual(CardTargetType.FallenAlly, queued.TargetType); // 원본 카드의 단일 전투불능 대상 확인
            Assert.AreEqual(CardEffectType.ReviveAlly, queued.EffectType); // 원본 카드의 실제 부활 효과 확인
            Assert.AreEqual(CardType.Cup, queued.CardType); // 원본 카드의 컵 속성 확인
            Assert.AreEqual(30, queued.SourceData.EffectValue); // 원본 카드의 최대 체력 비율 확인
            Assert.AreEqual(3, queued.ApCost); // 원본 카드의 행동력 비용 확인
            card.OnPointerClick(null); // 생존 아군뿐인 경우 첫 카드 클릭
            Assert.IsNull(fight.Controller.SelectedCard); // 부활 대상 없을 때 카드 선택 차단
            BattleUnitRuntime fallen = fight.Allies[1]; // 전투 중 쓰러뜨릴 동료 조회
            BattleUnitView fallenView = FindView(fight, fallen); // 실제 부활 대상 초상화 조회
            GameObject marker = fallenView.GetComponentsInChildren<Image>(true).Single(image => image.name == "DeadMarker").gameObject; // 현재 유닛의 실제 전투불능 표시 조회
            var pointer = new PointerEventData(EventSystem.current); // 사망 표시에서 보낼 포인터 입력 준비
            int deaths = 0; // 실제 사망 알림 횟수
            int defeats = 0; // 발생자가 포함된 실제 처치 알림 횟수
            int damageEvents = 0; // 사망에 사용한 실제 피해 알림 횟수
            int healingEvents = 0; // 기존 회복 알림 횟수
            fallen.Died += _ => deaths++; // 실제 사망 검사 연결
            fallen.Defeated += (_, _) => defeats++; // 처치 발생자 알림 검사 연결
            fallen.DamageTaken += (_, _) => damageEvents++; // 피해 재적용 감시 연결
            fallen.HealthRestored += (_, _) => healingEvents++; // 일반 회복 경로 재사용 감시 연결
            DeckData source = DeckData.CreateRuntime(new[] { new DeckCardEntry(queued.SourceData, fallen.CharacterSource) }); // 부활할 동료가 소유한 별도 검사 덱 구성
            try // 계산과 UI 및 드로우 동작 확인
            { // 부활 검증 시작
                BattleDeckRuntime ownedDeck = BattleDeckRuntime.Create(source, fight.Allies, 1, 247); // 현재 전투 명단을 공유하는 소유자별 카드 확인
                fallen.ModifyMaxHealth(101 - fallen.MaxHealth); // 소수점 올림 검사를 위한 최대 체력 설정
                fallen.GainBlock(7); // 사망 전 남겨둘 방어도 설정
                fallen.TakeDamage(1000, BattleDamageType.None, fight.Enemies[0], bypassBlockPercent: 100); // 방어도를 관통하는 실제 사망 발생
                int mentalAtDeath = fallen.CurrentMental; // 사망 정산이 끝난 정신력 저장
                Assert.IsTrue(marker.activeSelf); // 실제 전투불능 표시 활성화 확인
                Assert.AreEqual(7, fallen.CurrentBlock); // 부활 직전 사망 유닛의 기존 방어도 확인
                Assert.AreEqual(0, ownedDeck.DrawCards(1)); // 사망 중 카드 드로우 차단
                ExecuteEvents.ExecuteHierarchy(marker, pointer, ExecuteEvents.pointerClickHandler); // 선택 전에 사망 표시 클릭
                Assert.IsTrue(fallen.IsDead); // 카드 선택 없이 클릭한 사망 유닛의 상태 확인
                card.OnPointerClick(pointer); // 부활 카드 클릭 후 대상 선택 단계 진입
                Assert.AreSame(queued, fight.Controller.SelectedCard); // 실제 손패 카드 선택 확인
                FindView(fight, fight.Allies[2]).OnPointerClick(pointer); // 생존 아군 공격 불가 대상 입력
                Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 생존 아군 클릭의 비용 보존
                Assert.IsTrue(marker.GetComponent<Outline>().enabled); // 실제 전투불능 표시에 노란 선택 테두리 확인
                ExecuteEvents.ExecuteHierarchy(marker, pointer, ExecuteEvents.pointerClickHandler); // 사망 표시에서 조상 유닛으로 실제 클릭 전달
                Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 단일 카드 행동력 한 번 차감 확인
                yield return WaitForCard(fight.Runner); // 실제 충돌 효과와 지원 연출 종료 대기
                Assert.IsFalse(fallen.IsDead); // 원래 참조의 유닛이 실제 전투에 복귀했는지 확인
                Assert.AreEqual(31, fallen.CurrentHealth); // 최대 체력 백일의 삼십 퍼센트를 올린 삼십일 확인
                Assert.AreEqual(mentalAtDeath, fallen.CurrentMental); // 부활 전후의 정신력 변화 방지 확인
                Assert.AreEqual(0, fallen.CurrentBlock); // 이전 생명의 방어도 제거 확인
                Assert.AreEqual(0, fallen.HealthLostSinceEnemyTurn); // 사망 이전 타격 누적 제거 확인
                Assert.IsFalse(marker.activeSelf); // 원래 초상화에서 전투불능 표시 제거 확인
                Assert.AreEqual(1, ownedDeck.DrawCards(1)); // 동일 소유자 카드의 실제 드로우 재개
                Assert.AreSame(fallen, ownedDeck.Hand[0].OwnerUnit); // 새 유닛 생성 없이 기존 카드 소유자 확인
                Assert.AreEqual(1, deaths); // 부활에 의한 다시 사망 알림 방지
                Assert.AreEqual(1, defeats); // 부활에 의한 처치나 그 보상 재처리 방지
                Assert.AreEqual(1, damageEvents); // 부활에 의한 이전 피해 재처리 방지
                Assert.AreEqual(0, healingEvents); // 부활의 일반 회복 알림 사용 방지
                Assert.AreEqual(BattleResult.None, fight.Turns.Result); // 살아 있는 아군과 적이 있는 동일 전투 진행 확인
                CollectionAssert.Contains(fight.Deck.DiscardPile, queued); // 사용한 한 장의 정상 정산과 버림 확인
                fight.View("LIGHT_OF_LIFE").OnPointerClick(null); // 추가 사망자 없는 상태의 두 번째 카드 시도
                Assert.IsNull(fight.Controller.SelectedCard); // 이미 부활한 유닛의 재선택 차단
            } // 부활 검증 종료
            finally // 임시 소유자별 카드 원본 정리 보장
            { // 검사 자료 정리 시작
                Object.Destroy(source); // 별도 드로우 검사 덱 제거
            } // 검사 자료 정리 종료
        } // 실제 전투 검사 종료
    } // 클릭 부활 검사 종료

    private static IEnumerator CheckDragRevival() // 드롭 유효화와 전투 종료 이후 재진입 차단 확인
    { // 드래그 부활 검사 시작
        using (var fight = new Fight(CreateBattleView, CreateBattleView, "LIGHT_OF_LIFE")) // 실제 유닛 프리팹과 부활 카드로 전투 구성
        { // 대상별 범위 검사 시작
            BattleUnitRuntime fallen = fight.Allies[1]; // 현재 전투에서 쓰러질 아군 조회
            BattleUnitRuntime outside = BattleUnitRuntime.CreateAlly(fallen.CharacterSource); // 같은 원본이라도 참가하지 않은 이전 회차 아군 구성
            outside.ApplyPersistentHealth(0); // 명단 밖 유닛에 저장 체력 영 적용
            MethodInfo revive = typeof(BattleTurnRuntime).GetMethod("TryReviveFallenAlly", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 카드가 부르는 현재 명단 검사 조회
            Assert.IsNotNull(revive); // 대상 범위 검사 진입점 확인
            Assert.AreEqual(0, revive.Invoke(fight.Turns, new object[] { outside, 30 })); // 같은 캐릭터 원본의 명단 밖 유닛 부활 거절
            fight.Enemies[0].TakeDamage(1000, BattleDamageType.None, fight.Allies[0]); // 실제로 패배한 적도 후보에서 제외
            Assert.AreEqual(0, revive.Invoke(fight.Turns, new object[] { fight.Enemies[0], 30 })); // 쓰러진 적 부활 거절
            fallen.ModifyMaxHealth(1 - fallen.MaxHealth); // 소수점 올림의 최소 체력 경계 설정
            fallen.TakeDamage(1000, BattleDamageType.None, fight.Enemies[1]); // 준비한 아군의 실제 전투 사망
            BattleUnitView fallenView = FindView(fight, fallen); // 전투불능 아군의 실제 프리팹 조회
            GameObject marker = fallenView.GetComponentsInChildren<Image>(true).Single(image => image.name == "DeadMarker").gameObject; // 드롭 위치의 사망 표시 하위 화면 조회
            BattleCardView card = fight.View("LIGHT_OF_LIFE"); // 드래그할 실제 손패 카드 조회
            var drag = card.GetComponent<BattleCardDragHandler>() ?? card.gameObject.AddComponent<BattleCardDragHandler>(); // 기존 설치 경로와 같은 드래그 요소 준비
            drag.Initialize(card); // 드래그 요소와 실제 카드 연결
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }; // 왼쪽 버튼 드래그 입력 구성
            drag.OnBeginDrag(pointer); // 첫 번째 입력에서 실제 후보 윤곽선 켜기
            Assert.IsTrue(marker.GetComponent<Outline>().enabled); // 드래그 중 전투불능 아군 테두리 확인
            pointer.pointerCurrentRaycast = new RaycastResult { gameObject = fight.EnemyViews[0].gameObject }; // 쓰러진 적 화면으로 드롭 대상 지정
            drag.OnEndDrag(pointer); // 잘못된 대상 드롭으로 카드 사용 취소
            Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 쓰러진 적 드롭의 행동력 보존
            drag.OnBeginDrag(pointer); // 같은 손패로 다음 드래그 정상 시작
            pointer.pointerCurrentRaycast = new RaycastResult { gameObject = marker }; // 전투불능 표시 하위 이미지에서 실제 조상 대상 탐색
            drag.OnEndDrag(pointer); // 부활 대상에 정상 드롭
            Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 유효한 드롭의 단일 행동력 차감
            yield return WaitForCard(fight.Runner); // 실제 부활과 행동 연출 완료 대기
            Assert.AreEqual(1, fallen.CurrentHealth); // 소수 체력의 올림을 적용한 최소 한 점 확인
            Assert.IsTrue(fight.Enemies[0].IsDead); // 먼저 쓰러진 적의 기존 결과 보존
            Assert.IsTrue(outside.IsDead); // 현재 전투에 참가하지 않은 회차 아군 제외 확인
            foreach (BattleUnitRuntime ally in fight.Allies) // 현재 전투 아군 목록 순회
            { // 최종 사망 정산 시작
                ally.TakeDamage(1000, BattleDamageType.None, fight.Enemies[1]); // 부활한 아군도 이후 공격에서 정상 사망
            } // 최종 사망 정산 종료
            Assert.AreEqual(BattleResult.Defeat, fight.Turns.Result); // 실제 아군 전멸로 확정한 패배 확인
            Assert.AreEqual(0, revive.Invoke(fight.Turns, new object[] { fallen, 30 })); // 확정한 패배 뒤 기존 참가자의 부활 차단
            Assert.IsTrue(fallen.IsDead); // 종료된 전투 아군 상태 보존
        } // 대상별 범위 검사 종료
    } // 드래그 부활 검사 종료

    private static BattleUnitView CreateBattleView(Transform parent, BattleUnitRuntime unit) // 실제 전투 화면 원본의 검사 인스턴스 준비
    { // 실물 초상화 준비 시작
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"); // 게임에서 사용하는 전투 유닛 UI 조회
        BattleUnitView view = Object.Instantiate(prefab, parent).GetComponent<BattleUnitView>(); // 실물 유닛 화면을 전투 하위에 생성
        view.Bind(unit); // 실제 체력과 대상 입력 연결
        return view; // 전투 화면 테스트에 사용할 실물 컴포넌트 반환
    } // 실물 초상화 준비 종료

    private static BattleUnitView FindView(Fight fight, BattleUnitRuntime unit) // 동일 참조의 전투 유닛 화면 탐색
    { // 유닛 화면 탐색 시작
        return fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == unit); // 실제 전투 화면의 단일 대상 반환
    } // 유닛 화면 탐색 종료

    private static IEnumerator WaitForCard(BattleActionSequenceRunner runner) // 플레이 코루틴에서 충돌과 연출 종료 대기
    { // 카드 연출 대기 시작
        float deadline = Time.realtimeSinceStartup + 10f; // 실제 시간 기준 입력 확인 기한
        while (runner.IsBusy && Time.realtimeSinceStartup < deadline) // 연출 종료와 검사 기한 확인
        { // 연출 프레임 대기 시작
            yield return null; // 다음 전투 프레임 대기
        } // 연출 프레임 대기 종료
        Assert.IsFalse(runner.IsBusy); // 실제 카드 사용과 연출 종료 확인
    } // 카드 연출 대기 종료
} // 부활 검사 클래스 종료
