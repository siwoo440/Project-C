using System.Collections; // 플레이 모드 검사 사용
using System.Collections.Generic; // 빈 덱 목록 사용
using NUnit.Framework; // 자동 검증 기능 사용
using TMPro; // 파트너 화면 문구 확인 사용
using UnityEngine; // 유니티 데이터 사용
using UnityEngine.EventSystems; // 실제 카드 드래그 입력 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
public sealed class LoversPartnerTests // 연인 파트너 규칙 검사
{ // 검사 시작
    [UnityTest] // 실제 카드와 화면 검사 지정
    public IEnumerator PartnerCard_SelectsAllyAndDisplaysCardUse() // 파트너 지정과 화면 표시 확인
    { // 실제 파트너 카드 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 실제 부트 씬 시작
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && SceneManager.GetActiveScene().name != "10_MainMenu";) // 메인 메뉴 대기
        { // 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메뉴 대기 종료
        Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 로비 이동 요청
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 로비 초기화 대기
        { // 로비 대기 시작
            yield return null; // 다음 프레임 대기
        } // 로비 대기 종료
        PartyDepartureTestSupport.StartExploration(6, 7); // 연인과 전차 탐사 준비
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<ExplorationEncounterView>() == null;) // 조우 생성 대기
        { // 조우 대기 시작
            yield return null; // 다음 프레임 대기
        } // 조우 대기 종료
        Object.FindFirstObjectByType<ExplorationEncounterView>().SendMessage("OnTriggerEnter2D", Object.FindFirstObjectByType<ExplorationPlayerController>().GetComponent<Collider2D>()); // 실제 조우 진입
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<BattleSceneSetup>() == null;) // 전투 씬 대기
        { // 전투 대기 시작
            yield return null; // 다음 프레임 대기
        } // 전투 대기 종료
        yield return null; // 전투 초기화 대기
        BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 전투 관리자 조회
        Assert.IsTrue(battle.IsInitialized); // 실제 전투 초기화 확인
        BattleUnitRuntime lovers = battle.AllyUnits[0]; // 연인 전투 유닛 조회
        BattleUnitRuntime partner = battle.AllyUnits[1]; // 파트너 후보 조회
        battle.BattleDeck.DrawCards(12); // 전용 카드와 동료 카드 준비
        BattleCardView partnerCard = null; // 파트너 지정 카드 초기화
        BattleCardView attackCard = null; // 동료 공격 카드 초기화
        foreach (BattleCardView card in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 실제 손패 화면 순회
        { // 카드 조회 시작
            if (card.RuntimeCard != null && card.RuntimeCard.SourceData.CardId == "CRD_ARC_06_PARTNER") // 연인 전용 카드 확인
            { // 파트너 카드 저장 시작
                partnerCard = card; // 전용 카드 화면 저장
            } // 파트너 카드 저장 종료
            if (card.RuntimeCard != null && card.RuntimeCard.OwnerUnit == partner && card.RuntimeCard.EffectType == CardEffectType.Damage) // 동료 공격 카드 확인
            { // 동료 카드 저장 시작
                attackCard = card; // 동료 공격 카드 화면 저장
            } // 동료 카드 저장 종료
        } // 카드 조회 종료
        Assert.IsNotNull(partnerCard); // 실제 연인 전용 카드 확인
        Assert.IsNotNull(attackCard); // 실제 동료 공격 카드 확인
        CardInstance partnerAttack = attackCard.RuntimeCard; // 화면 갱신 후 조회할 공격 카드 저장
        yield return null; // 실제 드래그 처리기 설치 대기
        BattleCardHitRegion partnerHit = null; // 연인 카드 입력 영역 초기화
        foreach (BattleCardHitRegion hit in Object.FindObjectsByType<BattleCardHitRegion>(FindObjectsSortMode.None)) // 실제 손패 입력 영역 순회
        { // 연인 카드 입력 영역 검색 시작
            if (hit.CardView == partnerCard) // 연인 전용 카드 연결 확인
            { // 입력 영역 저장 시작
                partnerHit = hit; // 연인 카드 입력 영역 저장
                break; // 입력 영역 검색 종료
            } // 입력 영역 저장 종료
        } // 연인 카드 입력 영역 검색 종료
        Assert.IsNotNull(partnerHit); // 연인 카드 입력 영역 확인
        Assert.IsNotNull(partnerCard.GetComponent<BattleCardDragHandler>()); // 기존 드래그 처리기 연결 확인
        PointerEventData dragPointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = Vector2.zero }; // 테스트 드래그 포인터 생성
        partnerHit.OnBeginDrag(dragPointer); // 독립 입력 영역에서 실제 드래그 시작
        Assert.IsNotNull(GameObject.Find("Day47CardDragGhost")); // 기존 드래그 카드 생성 확인
        partnerHit.OnEndDrag(dragPointer); // 대상 없는 드래그 취소
        yield return null; // 드래그 카드 제거 대기
        BattleUnitView loversView = null; // 연인 화면 초기화
        BattleUnitView partnerView = null; // 파트너 화면 초기화
        BattleUnitView enemyView = null; // 적 화면 초기화
        foreach (BattleUnitView view in Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None)) // 실제 유닛 화면 순회
        { // 유닛 화면 조회 시작
            if (view.RuntimeUnit == lovers) // 연인 화면 확인
            { // 연인 화면 저장 시작
                loversView = view; // 연인 화면 저장
            } // 연인 화면 저장 종료
            if (view.RuntimeUnit == partner) // 파트너 화면 확인
            { // 파트너 화면 저장 시작
                partnerView = view; // 파트너 화면 저장
            } // 파트너 화면 저장 종료
            if (view.RuntimeUnit == battle.EnemyUnits[0]) // 적 화면 확인
            { // 적 화면 저장 시작
                enemyView = view; // 적 화면 저장
            } // 적 화면 저장 종료
        } // 유닛 화면 조회 종료
        Assert.IsNotNull(loversView); // 연인 화면 존재 확인
        Assert.IsNotNull(partnerView); // 파트너 화면 존재 확인
        Assert.IsNotNull(enemyView); // 적 화면 존재 확인
        int previousPoints = battle.SharedActionPoints.CurrentActionPoints; // 기존 행동력 저장
        partnerCard.OnPointerClick(null); // 파트너 지정 카드 선택
        loversView.OnPointerClick(null); // 자기 자신 선택 시도
        Assert.AreEqual(previousPoints, battle.SharedActionPoints.CurrentActionPoints); // 자기 자신 선택 차단 확인
        partnerCard.OnPointerClick(null); // 자기 자신 선택 후 카드 선택 취소
        partnerHit.OnBeginDrag(dragPointer); // 실제 입력 영역에서 파트너 카드 드래그
        dragPointer.pointerCurrentRaycast = new RaycastResult { gameObject = partnerView.gameObject }; // 생존 아군 화면으로 드롭 대상 설정
        partnerHit.OnEndDrag(dragPointer); // 실제 드래그 경로로 파트너 지정
        for (float deadline = Time.realtimeSinceStartup + 5f; Time.realtimeSinceStartup < deadline && partnerView.transform.Find("PartnerView") == null;) // 파트너 표시 대기
        { // 파트너 표시 대기 시작
            yield return null; // 다음 프레임 대기
        } // 파트너 표시 대기 종료
        Transform marker = partnerView.transform.Find("PartnerView"); // 실제 파트너 표시 조회
        Assert.IsNotNull(marker); // 파트너 표시 존재 확인
        Assert.AreEqual("파트너", marker.Find("PartnerText").GetComponent<TMP_Text>().text); // 지정 상태 문구 확인
        BattleActionSequenceRunner runner = battle.GetComponent<BattleActionSequenceRunner>(); // 행동 연출기 조회
        for (float deadline = Time.realtimeSinceStartup + 5f; Time.realtimeSinceStartup < deadline && runner.IsBusy;) // 지정 연출 종료 대기
        { // 연출 대기 시작
            yield return null; // 다음 프레임 대기
        } // 연출 대기 종료
        attackCard = null; // 갱신된 공격 카드 화면 초기화
        foreach (BattleCardView card in Object.FindObjectsByType<BattleCardView>(FindObjectsSortMode.None)) // 갱신된 손패 화면 순회
        { // 공격 카드 화면 조회 시작
            if (card.RuntimeCard == partnerAttack && card.gameObject.activeInHierarchy) // 동일 공격 카드 화면 확인
            { // 공격 카드 화면 저장 시작
                attackCard = card; // 갱신된 공격 카드 화면 저장
                break; // 공격 카드 검색 종료
            } // 공격 카드 화면 저장 종료
        } // 공격 카드 화면 조회 종료
        Assert.IsNotNull(attackCard); // 갱신된 공격 카드 화면 확인
        attackCard.OnPointerClick(null); // 파트너 공격 카드 선택
        enemyView.OnPointerClick(null); // 적 공격 대상 선택
        for (float deadline = Time.realtimeSinceStartup + 5f; Time.realtimeSinceStartup < deadline && marker.Find("PartnerText").GetComponent<TMP_Text>().text != "파트너 사용";) // 파트너 사용 표시 대기
        { // 사용 표시 대기 시작
            yield return null; // 다음 프레임 대기
        } // 사용 표시 대기 종료
        Assert.AreEqual("파트너 사용", marker.Find("PartnerText").GetComponent<TMP_Text>().text); // 파트너 카드 사용 화면 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 파트너 카드 검사 종료
    [Test] // 파트너 지정 검사 지정
    public void Partner_TracksCardUseAndResetsOnChangeTurnAndDeath() // 지정과 카드 사용 기록 확인
    { // 파트너 규칙 검사 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData loversData = roster.Characters[6]; // 연인 원본 조회
        BattleUnitRuntime lovers = BattleUnitRuntime.CreateAlly(loversData); // 연인 생성
        BattleUnitRuntime first = BattleUnitRuntime.CreateAlly(roster.Characters[0]); // 첫 동료 생성
        BattleUnitRuntime second = BattleUnitRuntime.CreateAlly(roster.Characters[1]); // 두 번째 동료 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData deckData = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 적 유닛 생성
        BattleUnitRuntime[] allies = { lovers, first, second }; // 아군 목록 생성
        BattleDeckRuntime deck = BattleDeckRuntime.Create(deckData, new List<IDeckCardEntry>(), allies, 5, 1); // 빈 덱 생성
        BattleTurnRuntime turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(6), allies, new[] { enemy }, 0, BattleType.Normal); // 턴 관리자 생성
        LoversPartnerRuntime link = new LoversPartnerRuntime(turns, allies); // 파트너 관리자 생성
        CardData attack = Resources.Load<CardData>("StarterCards/Attack"); // 공용 공격 카드 조회
        CardInstance firstCard = CardInstance.Create(attack, first, 0); // 첫 동료 카드 생성
        CardInstance secondCard = CardInstance.Create(attack, second, 1); // 두 번째 동료 카드 생성
        Assert.IsTrue(turns.StartBattle(0)); // 전투 시작 확인
        Assert.IsFalse(link.TrySetPartner(lovers, lovers)); // 자기 자신 지정 차단 확인
        Assert.IsFalse(link.TrySetPartner(lovers, enemy)); // 적 유닛 지정 차단 확인
        Assert.IsTrue(link.TrySetPartner(lovers, first)); // 첫 동료 지정 확인
        link.RecordCardUsed(firstCard); // 첫 동료 카드 사용 기록
        Assert.IsTrue(link.PartnerUsedCardThisTurn); // 같은 턴 카드 사용 확인
        Assert.IsTrue(link.TrySetPartner(lovers, second)); // 다른 동료로 변경 확인
        Assert.IsFalse(link.PartnerUsedCardThisTurn); // 변경 시 기존 기록 초기화 확인
        link.RecordCardUsed(firstCard); // 이전 동료 카드 사용 기록
        Assert.IsFalse(link.PartnerUsedCardThisTurn); // 이전 동료 기록 제외 확인
        link.RecordCardUsed(secondCard); // 현재 동료 카드 사용 기록
        Assert.IsTrue(link.PartnerUsedCardThisTurn); // 현재 동료 기록 확인
        Assert.IsTrue(turns.EndPlayerTurn()); // 플레이어 턴 종료 확인
        Assert.IsFalse(link.PartnerUsedCardThisTurn); // 턴 종료 시 기록 제거 확인
        Assert.AreSame(second, link.Partner); // 턴 전환 시 파트너 유지 확인
        second.TakeDamage(100000, BattleDamageType.None); // 현재 파트너 사망 처리
        Assert.IsNull(link.Partner); // 사망 시 파트너 연결 해제 확인
        Assert.IsTrue(turns.HasOtherLivingAlly(lovers)); // 다른 생존 동료 존재 확인
        first.TakeDamage(100000, BattleDamageType.None); // 마지막 동료 사망 처리
        Assert.IsFalse(turns.HasOtherLivingAlly(lovers)); // 단독 생존 시 파트너 후보 없음 확인
        link.Dispose(); // 파트너 관리자 정리
        turns.Dispose(); // 턴 관리자 정리
        Object.DestroyImmediate(enemyData); // 임시 적 원본 정리
        Object.DestroyImmediate(deckData); // 임시 덱 원본 정리
    } // 파트너 규칙 검사 종료
} // 검사 종료
