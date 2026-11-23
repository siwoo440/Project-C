using System.Collections; // 플레이 모드 대기 사용
using System.Collections.Generic; // 카드 목록 사용
using NUnit.Framework; // 카드 화면 동작 검증 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 손패 배경 이미지 사용

public sealed class BattleDrawAvailabilityTests // 선택 카드 실제 사용 가능 상태 검사
{ // 카드 화면 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator ChoiceAndRecoveryCards_EnableOnlyWhenTargetsExist() // 바보와 마법사 카드 사용 조건 확인
    { // 카드 사용 조건 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterData fool = roster.Characters[0]; // 바보 캐릭터 조회
        CharacterData magician = roster.Characters[1]; // 마법사 캐릭터 조회
        CharacterData other = roster.Characters[2]; // 다른 카드 소유자 조회
        CardData choiceCard = Resources.Load<CardData>("StarterCards/FoolChoice"); // 바보 전용 카드 조회
        CardData recoveryCard = Resources.Load<CardData>("StarterCards/MagicianRepair"); // 마법사 전용 카드 조회
        CardData otherCard = Resources.Load<CardData>("StarterCards/Attack"); // 테스트 대상 카드 조회
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 덱 원본 생성
        EnemyData enemySource = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        GameObject canvas = new GameObject("AvailabilityCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 테스트 캔버스 생성
        try // 테스트 화면과 원본 정리 보장
        { // 카드 사용 조건 검증 시작
            BattleUnitRuntime foolUnit = BattleUnitRuntime.CreateAlly(fool); // 바보 전투 유닛 생성
            BattleUnitRuntime magicianUnit = BattleUnitRuntime.CreateAlly(magician); // 마법사 전투 유닛 생성
            BattleUnitRuntime otherUnit = BattleUnitRuntime.CreateAlly(other); // 다른 소유자 전투 유닛 생성
            BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemySource); // 생존 적 유닛 생성
            List<IDeckCardEntry> entries = new List<IDeckCardEntry> // 실제 카드 소유 항목 생성
            { // 카드 항목 설정 시작
                new RunDeckCardEntry(choiceCard, fool), // 바보 전용 카드 추가
                new RunDeckCardEntry(recoveryCard, magician), // 마법사 전용 카드 추가
                new RunDeckCardEntry(otherCard, other) // 다른 소유자의 카드 추가
            }; // 카드 항목 설정 종료
            BattleUnitRuntime[] allies = { foolUnit, magicianUnit, otherUnit }; // 실제 아군 목록 생성
            BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, allies, 20, 1); // 실제 전투 덱 생성
            Assert.AreEqual(3, deck.DrawCards(3)); // 모든 테스트 카드 드로우
            CardInstance candidate = FindCard(deck.Hand, CardEffectType.Damage); // 다른 소유자의 카드 조회
            Assert.IsTrue(deck.DiscardCard(candidate)); // 다른 소유자의 카드 버리기
            Assert.AreEqual(1, deck.PrepareDrawChoices(1).Count); // 버린 카드를 덱으로 재구성
            otherUnit.ApplyPersistentHealth(0); // 다른 카드의 소유자 사망 상태 설정
            BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 전투 행동력 생성
            BattleTurnRuntime turns = new BattleTurnRuntime(deck, points, allies, new[] { enemy }, 0, BattleType.Normal); // 실제 턴 관리자 생성
            Assert.IsTrue(turns.StartBattle(0)); // 플레이어 턴 시작
            GameObject handObject = new GameObject("AvailabilityHand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
            handObject.transform.SetParent(canvas.transform, false); // 캔버스에 손패 연결
            BattleHandView hand = handObject.GetComponent<BattleHandView>(); // 손패 화면 조회
            Assert.IsTrue(hand.Bind(deck, points, turns)); // 실제 덱과 손패 연결
            int clicks = 0; // 활성 카드 클릭 수 초기화
            hand.CardClicked += selected => clicks++; // 실제 사용 가능 카드 클릭 기록
            FindView(hand, CardEffectType.ChooseDraw).OnPointerClick(null); // 사망 카드만 있을 때 바보 카드 클릭
            FindView(hand, CardEffectType.RecoverDiscard).OnPointerClick(null); // 버린 카드가 없을 때 마법사 카드 클릭
            Assert.AreEqual(0, clicks); // 후보가 없는 두 카드의 입력 차단 확인
            otherUnit.ApplyPersistentHealth(otherUnit.MaxHealth); // 다른 카드 소유자 부활 상태 설정
            hand.RefreshCardAvailability(); // 부활 상태 기준 손패 갱신
            FindView(hand, CardEffectType.ChooseDraw).OnPointerClick(null); // 생존 후보가 있을 때 바보 카드 클릭
            Assert.AreEqual(1, clicks); // 바보 카드 입력 활성 확인
            Assert.AreEqual(1, deck.DrawCards(1)); // 부활한 소유자의 카드 드로우
            Assert.IsTrue(deck.DiscardCard(candidate)); // 회수할 카드를 버린 더미에 추가
            FindView(hand, CardEffectType.RecoverDiscard).OnPointerClick(null); // 회수 후보가 있을 때 마법사 카드 클릭
            Assert.AreEqual(2, clicks); // 마법사 카드 입력 활성 확인
            hand.Unbind(); // 손패 이벤트 연결 해제
            turns.Dispose(); // 전투 턴 연결 해제
        } // 카드 사용 조건 검증 종료
        finally // 독립 테스트 자원 정리
        { // 테스트 자원 정리 시작
            Object.DestroyImmediate(canvas); // 손패와 캔버스 제거
            Object.DestroyImmediate(enemySource); // 임시 적 원본 제거
            Object.DestroyImmediate(source); // 임시 덱 원본 제거
        } // 테스트 자원 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 카드 사용 조건 검사 종료
    private static CardInstance FindCard(IReadOnlyList<CardInstance> cards, CardEffectType effect) // 지정 효과의 카드 조회
    { // 카드 조회 시작
        foreach (CardInstance candidate in cards) // 현재 카드 목록 순회
        { // 카드 효과 확인 시작
            if (candidate.EffectType == effect) // 지정 효과 일치 확인
            { // 일치 카드 반환 시작
                return candidate; // 찾은 카드 반환
            } // 일치 카드 반환 종료
        } // 카드 효과 확인 종료
        return null; // 카드 없음 반환
    } // 카드 조회 종료
    private static BattleCardView FindView(BattleHandView hand, CardEffectType effect) // 지정 효과의 손패 화면 조회
    { // 손패 화면 조회 시작
        foreach (BattleCardView view in hand.GetComponentsInChildren<BattleCardView>()) // 활성 손패 화면 순회
        { // 화면 카드 확인 시작
            if (view.RuntimeCard != null && view.RuntimeCard.EffectType == effect) // 지정 효과 일치 확인
            { // 일치 화면 반환 시작
                return view; // 찾은 화면 반환
            } // 일치 화면 반환 종료
        } // 화면 카드 확인 종료
        return null; // 손패 화면 없음 반환
    } // 손패 화면 조회 종료
} // 카드 화면 테스트 종료
