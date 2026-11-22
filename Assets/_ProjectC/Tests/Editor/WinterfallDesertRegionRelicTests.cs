using System; // 실제 전투 검사 원본 정리
using System.Collections; // 원래 플레이 모드 진입과 종료 대기
using NUnit.Framework; // 지역별 피해와 전투 효과 검증
using UnityEngine; // 원본 데이터와 실제 지역 태그 작성
using UnityEngine.TestTools; // 실제 적 행동과 탐사 이벤트 검사
using Object = UnityEngine.Object; // 검사에서 만든 원본 정리

public sealed class WinterfallDesertRegionRelicTests // 성채와 사막 및 기존 윈터폴 원본의 효과 확인
{ // 명시 지역 전투와 선택 이벤트 검사 시작
    [UnityTest] // 성채 기본 방어도와 실제 선제 유지 확인
    public IEnumerator ManorWickGrantsOpeningBlockOnlyToLivingAlliesInFortressEncounters() // 명시 성채와 이전 지역 기본값 비교
    { // 심지의 실제 방어도 적용 검사 시작
        yield return new EnterPlayMode(); // 실제 지역 전투와 탐사 유물 보유 시작
        Acquire("SleeplessManorWick"); // 원본 잠들지 않는 저택의 심지 획득
        foreach (bool ambush in new[] { false, true }) // 일반 개막과 적 선제 개막의 실제 처리 순서 비교
        { // 원본 성채 지역의 방어도 적용 확인 시작
            using var battle = new Trial(ExplorationRegion.Fortress); // 직렬화된 성채 조우를 연결한 실제 전투
            battle.AllyTwo.ApplyPersistentHealth(0); // 실제 전투불능 아군의 개막 적용 제외 준비
            Assert.AreEqual(ExplorationRegion.Fortress, battle.Turn.Region); // 조우 원본의 명시 성채 지역이 턴에 전달됐는지 확인
            Assert.IsTrue(battle.Turn.StartBattle(0, ambush)); // 각 실제 조우의 첫 진영을 사용한 전투 시작
            Assert.AreEqual(15, battle.Ally.CurrentBlock); // 다른 진영의 행동 전 살아 있는 아군 방어도 열다섯 확인
            Assert.Zero(battle.AllyTwo.CurrentBlock); // 쓰러진 출전자는 개막 방어도 미부여 확인
            if (!ambush) // 일반 전투의 첫 실제 적 진입 전에 방어도가 남는지 확인
            { // 일반 첫 적 턴 준비 시작
                Assert.IsTrue(battle.Turn.EndPlayerTurn()); // 올바른 첫 아군 턴 종료와 적 행동 진입
                Assert.AreEqual(15, battle.Ally.CurrentBlock); // 첫 적이 행동할 때까지 시작 방어도가 남는지 확인
            } // 일반 첫 적 턴 준비 종료
            Assert.IsTrue(battle.Turn.CompleteEnemyTurn()); // 실제 첫 적 턴 뒤 일반 방어도 초기화
            Assert.Zero(battle.Ally.CurrentBlock); // 원래 다음 아군 턴에는 개막 방어도 반복 없음 확인
        } // 원본 성채 지역의 방어도 적용 확인 종료
        using (var legacy = new Trial(null)) // 지역 태그 없이 이전 규칙으로 만든 실제 조우
        { // 이전 조우의 기본값 보존 확인 시작
            Assert.AreEqual(ExplorationRegion.Winterfall, legacy.Encounter.Region); // 옛 직렬화 원본의 윈터폴 기본값 확인
            Assert.IsTrue(legacy.Turn.StartBattle(0, true)); // 태그 없는 원래 적 선제 전투 시작
            Assert.Zero(legacy.Ally.CurrentBlock); // 성채 밖 기존 기본 방어도 유지 확인
        } // 이전 조우의 기본값 보존 확인 종료
        yield return new ExitPlayMode(); // 실제 성채 전투 검사 정리
    } // 심지의 실제 방어도 적용 검사 종료

    [UnityTest] // 실제 적 실명 확률에 사막 및 두 효과가 함께 적용되는 경계 확인
    public IEnumerator DesertBlindResistanceAddsToHandkerchiefAndLastsBeyondTheFirstEnemyTurn() // 첫 적 턴 및 다음 적 턴의 실제 동일 난수 대조
    { // 사막 팔찌의 적 효과 경계 검사 시작
        yield return new EnterPlayMode(); // 원래 적 상태 행동의 실제 확률 경로 시작
        Acquire("DesertGlassShardBracelet"); // 실제 사막 유리조각 팔찌 획득
        Acquire("DustEatenHandkerchief"); // 첫 실제 적 턴 실명 저항의 기존 손수건 획득
        using (var desert = new Trial(ExplorationRegion.Desert)) // 명시 사막 조우와 실제 적 행동 준비
        { // 합산한 오십 미만 저항 및 두 번째 턴 비교 시작
            Assert.IsTrue(desert.Turn.StartBattle(0, true)); // 두 저항이 함께 적용될 첫 적 선제 차례 시작
            Assert.IsFalse(ApplyBlind(desert, 65)); // 확률 육십오의 첫 실패 경계 확인
            Assert.IsTrue(ApplyBlind(desert, 64)); // 손수건과 팔찌를 합한 정상 마지막 성공 경계 확인
            desert.Ally.RemoveStatusEffect(BattleStatusEffectType.Blind); // 두 번째 적 턴의 판정 전 실제 실명 초기화
            Assert.IsTrue(desert.Turn.CompleteEnemyTurn() && desert.Turn.EndPlayerTurn()); // 첫 적 턴만의 손수건 효과를 마친 실제 다음 적 턴
            Assert.IsFalse(ApplyBlind(desert, 85)); // 사막의 단독 십오 퍼센트포인트 저항 경계 확인
            Assert.IsTrue(ApplyBlind(desert, 84)); // 첫 적 턴이 지난 뒤에도 팔찌 효과가 계속 남는지 확인
        } // 합산한 오십 미만 저항 및 두 번째 턴 비교 종료
        using (var legacy = new Trial(null)) // 태그가 없는 예전 윈터폴 조우 비교
        { // 팔찌를 적용하지 않는 기존 첫 턴 저항 시작
            Assert.IsTrue(legacy.Turn.StartBattle(0, true)); // 이전에도 동일하게 적용되는 첫 적 선제 차례 시작
            Assert.IsFalse(ApplyBlind(legacy, 80)); // 윈터폴의 첫 적 턴 손수건 저항 스무 점 확인
            Assert.IsTrue(ApplyBlind(legacy, 79)); // 팔찌를 추가하지 않는 원래 성공 경계 확인
            legacy.Ally.RemoveStatusEffect(BattleStatusEffectType.Blind); // 다음 저항 동안 상태 중첩 없는 동일 대상 준비
            Assert.IsTrue(legacy.Turn.CompleteEnemyTurn() && legacy.Turn.EndPlayerTurn()); // 기본 지역 요건의 다음 적 턴
            Assert.IsTrue(ApplyBlind(legacy, 99)); // 기본 지역은 첫 턴 뒤 추가 저항 없음 확인
        } // 팔찌를 적용하지 않는 기존 첫 턴 저항 종료
        yield return new ExitPlayMode(); // 실제 적 실명 지역 검사 종료
    } // 사막 팔찌의 적 효과 경계 검사 종료

    [UnityTest] // 실제 위험 선택의 지역과 고정 함정 피해 경감 및 한 번 지급 검사
    public IEnumerator WeatheredFootprintReducesOnlyChosenDesertTrapHealthDamage() // 태그 없는 함정과 태그 있는 진짜 선택 결과를 직접 비교
    { // 풍화된 발자국석의 실제 선택 앱 동작 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 실제 사용자 회차와 분리된 탐사 회차 검사 환경 확인
        { // 다른 탐사 기록의 보호 처리 시작
            Assert.Ignore("저장 경로가 분리된 검증 환경에서 실행"); // 원래 사용자의 진행 기록 보존
        } // 다른 탐사 기록의 보호 처리 종료
        yield return new EnterPlayMode(); // 탐사 선택과 실제 영속 체력 검증 시작
        var session = PrepareExpedition(); // 실제 출전 파티를 사용한 새 탐사 회차 준비
        Acquire("WeatheredFootprintStone"); // 실제 발자국석 원본 획득
        var party = BattleResultManager.EnsureInstance(); // 실제 탐사 파티 체력 저장 관리자
        var actor = party.ActiveParty.Members[0]; // 피해를 확인할 선택한 생존 아군
        var desertEvent = ScriptableObject.CreateInstance<ExplorationEventData>(); // 사막 태그를 가진 실제 이벤트 원본 유형
        var oldEvent = ScriptableObject.CreateInstance<ExplorationEventData>(); // 지역이 저장되지 않은 이전 이벤트 원본 유형
        try // 실제 이벤트 원본을 테스트 뒤 지우는 범위
        { // 사막 태그와 기존 위험 선택의 실제 비교 시작
            JsonUtility.FromJsonOverwrite("{\"region\":1}", desertEvent); // 새로 저장한 원본의 명시 사막 지역 지정
            Assert.AreEqual(ExplorationRegion.Desert, desertEvent.Region); // 사막 실제 이벤트 태그의 타입 확인
            Assert.AreEqual(ExplorationRegion.Winterfall, oldEvent.Region); // 태그가 없는 실제 원본의 이전 기본값 확인
            party.RestoreSavedAllyState(actor, 12, 30, 0); // 같은 기준점의 첫 실제 파편 선택 준비
            Assert.IsTrue(AshstormEyeEvent.TryResolve("region-old-pieces", AshstormEyeEvent.Shatter, actor, null, out _, oldEvent.Region)); // 기존 기본 지역의 실제 파편 선택 실행
            Assert.AreEqual(5, Health(party, actor)); // 윈터폴 파편의 원래 체력 일곱 피해 확인
            party.RestoreSavedAllyState(actor, 12, 30, 0); // 같은 체력으로 사막 태그가 붙은 실제 위험 선택 준비
            Assert.IsTrue(AshstormEyeEvent.TryResolve("region-desert-pieces", AshstormEyeEvent.Shatter, actor, null, out string summary, desertEvent.Region)); // 명시 사막 파편 선택의 실제 적용
            Assert.AreEqual(7, Health(party, actor)); // 일곱의 사막 경감 뒤 내림 다섯 피해 확인
            StringAssert.Contains("12 → 7", summary); // 미리 표시한 수치와 같은 기존 상태 안내 확인
            Assert.IsTrue(session.IsEventResolved("region-desert-pieces")); // 실제 한 번의 탐사 사건 해결 상태 확인
            Assert.IsFalse(AshstormEyeEvent.TryResolve("region-desert-pieces", AshstormEyeEvent.Shatter, actor, null, out _, desertEvent.Region)); // 이미 완료한 방의 파편 피해 중복 거절
            Assert.AreEqual(7, Health(party, actor)); // 거절한 재선택에 실제 체력 손실 없음 확인
            Acquire("AfterglowLantern"); // 기존 고정 함정 피해 감소의 조합 준비
            Assert.IsTrue(AshstormEyeEvent.TryResolve("region-desert-combined", AshstormEyeEvent.Shatter, actor, null, out _, desertEvent.Region)); // 기존 및 사막 감소가 함께 있는 별도 실제 파편 선택
            Assert.AreEqual(3, Health(party, actor)); // 고정 감소 뒤 남은 피해에 사막 비율을 적용한 결과 확인
            Assert.IsTrue(AshstormEyeEvent.TryResolve("region-desert-look", AshstormEyeEvent.LookAway, actor, null, out _, desertEvent.Region)); // 사막에 있으나 피해를 고르지 않은 실제 선택
            Assert.AreEqual(3, Health(party, actor)); // 피해 없는 선택의 저장 체력 유지 확인
        } // 사막 태그와 기존 위험 선택의 실제 비교 종료
        finally // 생성한 지역 이벤트 원본의 실제 해제 보장
        { // 테스트 이벤트 원본 정리 시작
            Object.Destroy(desertEvent); // 테스트에서 지정한 사막 원본 정리
            Object.Destroy(oldEvent); // 테스트에서 만든 이전 원본 정리
        } // 테스트 이벤트 원본 정리 종료
        yield return new ExitPlayMode(); // 실제 탐사 함정 검사 종료
    } // 풍화된 발자국석의 실제 선택 앱 동작 검사 종료

    private static bool ApplyBlind(Trial battle, int roll) // 본체의 기존 적 상태 명령과 난수 입력 연결
    { // 실제 적 실명 단일 행동 시작
        var action = new BattleEnemyAction(battle.Enemy, battle.Ally, EnemyActionType.ApplyStatusEffect, BattleDamageType.None, 15, BattleStatusEffectType.Blind, 1, 1, EnemyTargetRule.FirstLiving, "실명 경계 검사", 1, 1, 1, 0, percentRoll: () => roll); // 현재 보유 유물과 실제 난수 경계를 사용하는 원래 적 행동
        action.Execute(); // 실제 저항과 상태 적용을 거친 적의 행동 실행
        return battle.Ally.GetStatusEffect(BattleStatusEffectType.Blind) != null; // 대상의 실제 활성 실명 여부
    } // 실제 적 실명 단일 행동 종료

    private static void Acquire(string key) // 원래 탐사 유물 인벤토리에 검사 원본 추가
    { // 실제 유물 보유 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 출시 리소스와 같은 실제 유물 원본 조회
        Assert.IsNotNull(relic, key); // 등록된 원본 유물 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 원래 유물 획득 경로로 인벤토리 추가
    } // 실제 유물 보유 종료

    private static ExplorationSessionManager PrepareExpedition() // 저장과 실제 선택 실행에 사용할 출전 파티 준비
    { // 격리 연습 탐사 회차 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 출전 가능 캐릭터 원본 조회
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 실제 회복 상태를 반영하는 파티 편성 생성
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 실제 두 명을 현재 회차 파티에 등록
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 선택 파티의 실제 전투 덱과 출전 자료 생성
        var party = BattleResultManager.EnsureInstance(); // 실제 전투 및 탐사 공용 파티 상태
        party.ResetSavedPartyState(); // 독립 검사의 초기 저장 체력과 정신력 준비
        Assert.IsTrue(party.RegisterParty(PartyLoadoutRuntime.Active.Party)); // 원래 게임의 현재 출전 파티 등록
        RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 실제 원래 회차 전투 덱 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 게임의 실제 탐사 진행 관리자
        session.ResetExploration(); // 다른 독립 테스트의 방과 진행 정보 초기화
        session.SetCurrentFloorSeed(270); // 이 검사의 결정적 실제 층 시드 지정
        return session; // 실제 선택 완료를 조회할 현재 탐사 관리자 반환
    } // 격리 연습 탐사 회차 준비 종료

    private static int Health(BattleResultManager party, CharacterData actor) // 원래 파티 저장 체력 조회
    { // 현재 탐사 진행 중 체력 정보 확인 시작
        Assert.IsTrue(party.TryGetSavedAllyState(actor, out int health, out _, out _)); // 선택 대상의 원래 현재 체력 읽기
        return health; // 판정 뒤 실제 저장 체력 반환
    } // 현재 탐사 진행 중 체력 정보 확인 종료

    private sealed class Trial : IDisposable // 실제 조우 자료를 전달하는 개막과 상태 검사 전투
    { // 실제 참가자와 전투 준비 시작
        private readonly CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 실제 첫 번째 아군 생성 원본
        private readonly CharacterData secondCharacter = ScriptableObject.CreateInstance<CharacterData>(); // 두 번째 출전 아군 생성 원본
        private readonly EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 적 실명 행동의 실제 적 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 기본 전투 덱의 안전한 동작용 원본
        private readonly DeckData deckData; // 출전 원본이 소유한 실제 카드 덱
        private readonly BattleStatusEffectController statuses; // 실제 두 진영 턴의 상태 만료 처리기
        internal readonly EncounterData Encounter = ScriptableObject.CreateInstance<EncounterData>(); // 타입이 지정된 실제 조우 원본
        internal readonly BattleUnitRuntime Ally; // 상태와 방어도의 실제 생존 아군
        internal readonly BattleUnitRuntime AllyTwo; // 전투불능 비교를 위한 실제 다른 아군
        internal readonly BattleUnitRuntime Enemy; // 실제 적 상태 명령을 사용하는 적 개체
        internal readonly BattleTurnRuntime Turn; // 출전과 실제 페이즈를 진행하는 런타임

        internal Trial(ExplorationRegion? region) // 새 태그 또는 이전 생략 기본값의 실제 조우 준비
        { // 원래 조우와 출전 명단 연결 시작
            if (region.HasValue) // 명시한 원본 지역 설정 여부 확인
            { // 테스트에서 만든 실제 직렬화 지역 설정 시작
                JsonUtility.FromJsonOverwrite($"{{\"region\":{(int)region.Value}}}", Encounter); // 출시 조우와 같은 직렬화 필드에 지역 지정
            } // 테스트에서 만든 실제 직렬화 지역 설정 종료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":100}", character); // 첫 아군의 생존과 원래 체력 기준
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":100}", secondCharacter); // 쓰러뜨릴 비교 아군의 실제 기본 체력
            Ally = BattleUnitRuntime.CreateAlly(character); // 실제 전투의 첫 생존 아군
            AllyTwo = BattleUnitRuntime.CreateAlly(secondCharacter); // 다른 생존 상태의 실제 아군
            Enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 같은 조우의 정상 적 런타임
            var allies = new[] { Ally, AllyTwo }; // 두 아군의 실제 출전 명단
            var enemies = new[] { Enemy }; // 실제 적 행동이 연결되는 대상 목록
            deckData = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, character) }); // 실제 기존 덱에 카드와 소유자 연결
            var deck = BattleDeckRuntime.Create(deckData, allies, 3, 270); // 실제 전투 덱의 결정적 손패 구축
            Turn = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), allies, enemies, 0, BattleType.Normal, region: Encounter.Region); // 원본 조우의 지역을 실제 턴 관리자에 전달
            statuses = new BattleStatusEffectController(Turn, allies, enemies, new BattleStatusEffectProcessor()); // 실제 적 턴의 정상 처리와 기간 만료 연결
        } // 원래 조우와 출전 명단 연결 종료

        public void Dispose() // 테스트 전투와 원본 해제
        { // 실제 전투 검사 정리 시작
            statuses.Dispose(); // 진영 상태 지속 처리 연결 해제
            Turn.Dispose(); // 실제 전투의 원래 저항과 참가자 연결 해제
            Object.Destroy(deckData); // 독립된 실제 덱 원본 제거
            Object.Destroy(card); // 테스트 전투 카드 원본 제거
            Object.Destroy(enemyData); // 테스트 적 원본 제거
            Object.Destroy(character); // 실제 검사 시 출전한 첫 원본 제거
            Object.Destroy(secondCharacter); // 별도 출전한 두 번째 원본 제거
            Object.Destroy(Encounter); // 직렬화 지역 확인용 원본 조우 제거
        } // 실제 전투 검사 정리 종료
    } // 실제 참가자와 전투 준비 종료
} // 명시 지역 전투와 선택 이벤트 검사 종료
