using System; // 전투 검사 정리와 난수 입력 처리
using System.Collections; // 실제 플레이 모드 전환 대기
using System.Linq; // 검사 아군과 적의 독립 원본 생성
using System.Reflection; // 에디터 검사에서 실제 저장 자료 생성 호출
using NUnit.Framework; // 전투 유물의 정확한 수치 검증
using UnityEngine; // 실제 전투 원본 및 유물 리소스 조회
using UnityEngine.TestTools; // 실제 전투 시작과 결과 저장 검사
using Object = UnityEngine.Object; // 검사 원본의 유니티 객체 해제
using Random = UnityEngine.Random; // 원래 전역 무작위 상태 보존

public sealed class WinterfallAbyssOathOpeningTests // 실제 개막과 결과 정산의 네 유물 확인
{ // 시작 및 종료 유물 회귀 검사 시작
    [UnityTest] // 첫 적 선제 포함 공격력 약화와 정신력 비용 확인
    public IEnumerator LampWeakensLivingEnemiesForTwoEnemyTurnsAndPaysUnavoidableMentalCost() // 붉은 심연의 등잔의 실제 개막 조건 확인
    { // 등잔의 실제 선제 전투 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 보유와 적 턴을 연결할 검사 환경
        Acquire("RedAbyssLamp"); // 붉은 심연의 등잔의 실제 리소스 획득
        Acquire("BlackAbyssPlate"); // 실제 정신 피해를 한 번 막는 비교 유물 획득
        using (var fight = new OpeningBattle()) // 개막 직전 유효 대상을 정할 실제 전투
        { // 실제 시전 상태와 두 적 차례 확인 시작
            fight.Allies[2].ApplyPersistentHealth(0); // 현재 전투불능이라 비용을 내지 않는 비교 아군
            fight.Enemies[2].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 기존 약화 면역이 막을 생존 적 준비
            fight.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 개막 전에 이미 사망한 비교 적 준비
            Assert.IsTrue(fight.Turns.StartBattle(0, true)); // 아군 행동 전 선제 적 턴이 먼저 오는 실제 개막
            Assert.AreEqual(45, fight.Allies[0].CurrentMental); // 살아 있는 첫 아군의 정확한 정신력 5 비용 확인
            Assert.AreEqual(45, fight.Allies[1].CurrentMental); // 살아 있는 다른 아군의 정신력 비용 확인
            Assert.AreEqual(50, fight.Allies[2].CurrentMental); // 전투불능 아군의 원래 정신력 보존 확인
            Assert.Zero(fight.Allies[0].ChangeMental(-4, BattleMentalChangeReason.DisruptReceived).AppliedDelta); // 등잔 비용 후에도 검은 심연판의 첫 실제 피해 보호 유지 확인
            Assert.AreEqual(-4, fight.Allies[0].ChangeMental(-4, BattleMentalChangeReason.DisruptReceived).AppliedDelta); // 진짜 정신 피해를 한 번 막은 뒤 기존 실제 피해 확인
            var first = fight.Enemies[0]; // 두 적 차례 동안 약화될 실제 선제 공격자
            var intent = Action(first, fight.Allies[0], EnemyActionType.Attack, BattleStatusEffectType.None, 10, 0, () => 0); // 전투 예고와 실전에 쓰는 정상 물리 공격
            int baseline = first.ModifyOutgoingDamage(intent.Amount); // 실제 난이도와 정신 상태를 포함한 원래 공격원 값
            Assert.AreEqual(baseline - 2, intent.AttackAmount); // 기존 공격력 약화가 진짜 적 공격에 두 점 적용 확인
            Assert.AreEqual(2, fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.AttackPowerDown).EffectiveValue); // 다른 실제 생존 적도 같은 정수 약화 적용 확인
            Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 정상 상태 면역을 통과하지 않은 적 제외 확인
            Assert.IsNull(fight.Enemies[3].GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 전투불능 적에게 개막 상태가 붙지 않음 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 실제 적 선제 턴 종료
            Assert.AreEqual(1, first.GetStatusEffect(BattleStatusEffectType.AttackPowerDown).RemainingTurns); // 선제 처리 뒤 남은 적 턴 정확히 하나 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 실제 적 턴 시작
            Assert.AreEqual(baseline - 2, intent.AttackAmount); // 두 번째 적 턴까지 약화 효력 유지 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 종료
            Assert.IsNull(first.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 실제 적 턴 둘이 끝난 뒤 약화 만료 확인
            Assert.AreEqual(baseline, intent.AttackAmount); // 기간이 끝난 예고의 원래 공격력 복원 확인
        } // 실제 시전 상태와 두 적 차례 확인 종료
        yield return new ExitPlayMode(); // 독립 실제 전투 모드 종료
    } // 등잔의 실제 선제 전투 검사 종료

    [UnityTest] // 전투 전체 공격 증가와 저장되는 생존 아군 체력 확인
    public IEnumerator NamelessOathAddsOrdinaryAttackAndSettlesNonlethalMissingHealthOnVictoryAndEscape() // 서약의 전투 전용 공격과 종료 체력 비용 확인
    { // 실제 승리와 도주에서의 결과 정산 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 원본과 전투 관리자가 연결되는 모드
        Acquire("NamelessBlackOath"); // 무명자의 검은 서약의 실제 유물 원본 획득
        using (var fight = new OpeningBattle(BattleType.Boss)) // 전투 종류 제한 없는 실제 우두머리 결과 검사
        { // 승리 저장의 체력 및 비전투 비용 확인 시작
            var first = fight.Allies[0]; // 소수점 버림과 기존 공격 강화가 필요한 첫 아군
            var low = fight.Allies[1]; // 정확한 한 점 생존 하한을 시험할 아군
            var fallen = fight.Allies[2]; // 공격력과 종료 체력 비용에서 제외할 전투불능 아군
            first.ApplyPersistentHealth(41); // 잃은 체력 59의 이십 퍼센트 내림 경계
            low.ApplyPersistentHealth(2); // 서약이 그대로라면 쓰러질 생존 아군 체력 설정
            fallen.ApplyPersistentHealth(0); // 개막 이후 공격력을 받지 않는 전투불능 아군
            first.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 3, 2, 1); // 서약과 독립으로 더해질 일반 카드 공격력 준비
            Assert.IsTrue(fight.Turns.StartBattle(0)); // 실제 전투 시작 시 전체 전투용 공격 강화 획득
            Assert.AreEqual(8, first.AttackPowerBonus); // 서약 5와 일반 강화 3의 원래 공격력 합 확인
            Assert.AreEqual(5, low.AttackPowerBonus); // 다른 실제 생존 아군의 기본 공격 증가 5 확인
            Assert.Zero(fallen.AttackPowerBonus); // 전투불능 아군 개막 공격 증가 제외 확인
            first.GainBlock(30); // 일반 공격 피해였다면 소모될 아군 방어도
            int hits = 0; // 종료 비용이 일반 피해 이벤트를 내지 않는지 감시할 횟수
            first.DamageTaken += (_, __) => hits++; // 실제 피격 이벤트 구독
            foreach (var enemy in fight.Enemies) // 실제 전투에서 모든 적 제거
            { // 우두머리 전투 결과 조건 준비 시작
                enemy.TakeDamage(1000, BattleDamageType.None); // 일반 사망 경로를 통해 마지막 적 제거
            } // 우두머리 전투 결과 조건 준비 종료
            Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 결과 전투로부터 실제 승리 확정 확인
            var saved = Capture(fight.Turns); // 실제 씬의 체력 저장 직전 생성 경로 실행
            Assert.AreEqual(30, saved.AllyStates[0].CurrentHealth); // 잃은 체력 59의 20%를 내린 11만 차감해 저장
            Assert.AreEqual(1, saved.AllyStates[1].CurrentHealth); // 치명 비용을 막은 한 점 생존 체력 저장
            Assert.Zero(saved.AllyStates[2].CurrentHealth); // 기존 전투불능 아군의 저장값 보존 확인
            Assert.AreEqual(30, first.CurrentBlock); // 저장용 체력 비용이 방어도를 쓰지 않음 확인
            Assert.Zero(hits); // 반격이나 공격 피해 처리 신호 미발행 확인
            Assert.AreEqual(30, Capture(fight.Turns).AllyStates[0].CurrentHealth); // 저장 재호출이 같은 서약 비용을 또 지불하지 않는지 확인
            fight.Turns.Dispose(); // 실제 전투 씬 종료와 같은 턴 관리자 연결 해제
            Assert.AreEqual(3, first.AttackPowerBonus); // 일반 강화는 두고 서약의 전투 공격력만 해제 확인
        } // 승리 저장의 체력 및 비전투 비용 확인 종료
        using (var fight = new OpeningBattle()) // 정상 전투에서 탈출한 결과의 저장 경로 재현
        { // 실제 도주 비용 정산 시작
            fight.Allies[0].ApplyPersistentHealth(77); // 실제 손실 체력 23의 내림 비용 4 준비
            Assert.IsTrue(fight.Turns.StartBattle(0) && fight.Turns.TryEscape()); // 정상 시작과 진짜 도주 결과 확정
            Assert.AreEqual(73, Capture(fight.Turns).AllyStates[0].CurrentHealth); // 도주 후 탐사로 돌아가기 전에 비용이 반영된 체력 확인
            Assert.AreEqual(100, Capture(fight.Turns).AllyStates[1].CurrentHealth); // 전 체력 아군의 영 비용과 재호출 확인
        } // 실제 도주 비용 정산 종료
        yield return new ExitPlayMode(); // 독립 검사 실행 환경 종료
    } // 실제 승리와 도주에서의 결과 정산 검사 종료

    [UnityTest] // 첫 실제 적 턴의 실명 확률 경계와 선제 라운드 검증
    public IEnumerator HandkerchiefUsesTheNativeBlindRollOnlyThroughTheFirstActualEnemyTurn() // 선제 공격에서만 적용되는 이십 퍼센트포인트 비교
    { // 실제 적 행동의 실명 저항 검사 시작
        yield return new EnterPlayMode(); // 실제 적 행동 실행과 유물 보유 모드 진입
        Acquire("DustEatenHandkerchief"); // 먼지 먹은 손수건 실제 자산 획득
        using (var fight = new OpeningBattle()) // 선제 진행 도중만 보호되는 실제 전투
        { // 순수 실명과 공격 후 실명의 두 실제 발동 방식 비교 시작
            var actor = fight.Enemies[0]; // 실제로 아군에게 약화를 가하는 적
            var ally = fight.Allies[0]; // 첫 실제 적 턴에 씌워질 아군
            int rolls = 0; // 순수 효과에 들어간 실제 난수 호출 횟수
            Assert.IsTrue(fight.Turns.StartBattle(0, true)); // 일반 아군 시작을 건너뛰는 적 선제 진입
            var resist = Action(actor, ally, EnemyActionType.ApplyStatusEffect, BattleStatusEffectType.Blind, 25, 0, () => { rolls++; return 80; }); // 확정 실명의 원래 백 퍼센트에 이십 포인트 저항 끝 경계 입력
            Assert.AreEqual(BattleStatusEffectApplyResult.BlockedByImmunity, resist.Execute().StatusApplyResult); // 첫 실제 적 턴의 대체 순수 실명 저항 확인
            Assert.AreEqual(1, rolls); // 저항 판정에 기존 입력 난수 한 번 사용 확인
            Assert.IsNull(ally.GetStatusEffect(BattleStatusEffectType.Blind)); // 실명 차단 시 아군이 새 상태 슬롯을 소모하지 않음 확인
            var hit = Action(actor, ally, EnemyActionType.Attack, BattleStatusEffectType.Blind, 1, 60, () => 45); // 기존 육십 퍼센트 실명 공격의 비교 시드
            Assert.IsFalse(hit.Execute().HasAdditionalStatus); // 첫 적 턴에는 기존 시도가 사십 퍼센트로 낮아져 실패 확인
            var fear = Action(actor, ally, EnemyActionType.ApplyStatusEffect, BattleStatusEffectType.Fear, 1, 0, () => { rolls++; return 99; }); // 실명과 구분되는 확정 공격 상태
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, fear.Execute().StatusApplyResult); // 다른 약화의 원래 부여 처리 확인
            Assert.AreEqual(1, rolls); // 실명 이외 확정 부여가 새 난수를 쓰지 않음 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn() && fight.Turns.EndPlayerTurn()); // 선제 뒤 첫 아군 턴과 두 번째 실제 적 턴 진입
            Assert.AreEqual(1, fight.Turns.CurrentRound); // 선제 이후 아직 같은 라운드 번호인 원래 흐름 확인
            Assert.IsTrue(hit.Execute().HasAdditionalStatus); // 같은 라운드 번호라도 두 번째 실제 적 턴의 일반 확률 복원 확인
            Assert.IsNotNull(ally.GetStatusEffect(BattleStatusEffectType.Blind)); // 원래 확률의 실제 피격 실명 상태 확인
        } // 순수 실명과 공격 후 실명의 두 실제 발동 방식 비교 종료
        yield return new ExitPlayMode(); // 실명 저항 검사 모드 종료
    } // 실제 적 행동의 실명 저항 검사 종료

    [UnityTest] // 생존 적 균등 추첨과 면역 재추첨 금지 확인
    public IEnumerator OrnamentUsesOneUniformOpeningChoiceWithoutRerollingAnImmuneEnemy() // 금 간 접시 장식의 실제 개막 대상 추첨 검증
    { // 개막 무작위 적 약화 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 자산과 전투 난수 입력 모드 진입
        Acquire("CrackedPlateOrnament"); // 금 간 접시 장식 실제 유물 획득
        using (var fight = new OpeningBattle()) // 생존 적 셋의 배치와 면역 비교 전투 준비
        { // 정상 단일 약화와 실제 두 적 턴 확인 시작
            fight.Enemies[2].TakeDamage(1000, BattleDamageType.None); // 세 번째 칸의 죽은 적을 무작위 후보에서 제외
            Random.InitState(108); // 전투 대상 추첨의 독립 시드 설정
            int expected = new[] { 0, 1, 3 }[Random.Range(0, 3)]; // 같은 난수에서 실제 생존 배열만 사용할 기대 위치
            var afterRoll = Random.state; // 후보 추첨 한 번 직후의 대조 난수 상태
            Random.InitState(108); // 실제 전투에서 같은 시작 추첨 시드 복구
            Assert.IsTrue(fight.Turns.StartBattle(0)); // 첫 플레이어 단계보다 앞선 실제 개막 적용
            Assert.AreEqual(afterRoll, Random.state); // 무작위 생존 적을 고르는 데 한 번만 사용했는지 확인
            Assert.AreEqual(1, fight.Enemies.Count(enemy => enemy.GetStatusEffect(BattleStatusEffectType.AttackPowerDown) != null)); // 한 적에게만 약화가 붙는지 확인
            var chosen = fight.Enemies[expected]; // 무작위 고르기에서 실제 생존 기준으로 예상한 적
            Assert.AreEqual(1, chosen.GetStatusEffect(BattleStatusEffectType.AttackPowerDown).EffectiveValue); // 원본 효과의 공격력 감소 한 점 확인
            Assert.AreEqual(2, chosen.GetStatusEffect(BattleStatusEffectType.AttackPowerDown).RemainingTurns); // 두 실제 적 턴의 시작 지속 확인
        } // 정상 단일 약화와 실제 두 적 턴 확인 종료
        using (var fight = new OpeningBattle()) // 면역 적을 뽑아 효과가 없어지는 비교 전투
        { // 면역 이후 다른 적을 다시 추천하지 않는지 확인 시작
            Random.InitState(17); // 생존 적 넷에 대해 면역으로 만들 대상 조회 시작
            int selected = Random.Range(0, fight.Enemies.Length); // 한 번 굴려 실제 선택될 적 위치 확인
            var afterRoll = Random.state; // 면역 재추첨을 확인할 원래 한 번의 난수 상태
            fight.Enemies[selected].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 무작위 선택되는 대상의 기존 면역 유지
            Random.InitState(17); // 실제 개막 순서 내의 동일 난수 재설정
            Assert.IsTrue(fight.Turns.StartBattle(0)); // 생존 후보 중 면역 적이 있는 실제 전투 시작
            Assert.AreEqual(afterRoll, Random.state); // 면역 처리 뒤 두 번째 무작위 선택 미실행 확인
            Assert.IsFalse(fight.Enemies.Any(enemy => enemy.GetStatusEffect(BattleStatusEffectType.AttackPowerDown) != null)); // 면역 적을 뽑았을 때 약화 대상 없음 확인
        } // 면역 이후 다른 적을 다시 추천하지 않는지 확인 종료
        yield return new ExitPlayMode(); // 개막 무작위 검사 모드 종료
    } // 개막 무작위 적 약화 검사 종료

    private static BattleResultData Capture(BattleTurnRuntime turns) // 실제 씬이 사용하는 최종 체력 저장 자료 구성 호출
    { // 결과 캡처 연결 시작
        var method = typeof(BattleTurnRuntime).GetMethod("CaptureBattleResult", BindingFlags.Instance | BindingFlags.NonPublic); // 공유 코드의 비공개 저장 직전 경로 조회
        Assert.IsNotNull(method); // 씬 결과 저장 메서드의 연결 확인
        return (BattleResultData)method.Invoke(turns, new object[] { Array.Empty<string>() }); // 전투 결과와 체력 스냅샷의 실제 반환
    } // 결과 캡처 연결 종료

    private static BattleEnemyAction Action(BattleUnitRuntime actor, BattleUnitRuntime target, EnemyActionType type, BattleStatusEffectType status, int amount, int chance, Func<int> roll) // 고정 난수로 실제 적 행동 실행 준비
    { // 기존 적 공격 및 상태 행동 생성 시작
        return new BattleEnemyAction(actor, target, type, BattleDamageType.Physical, amount, status, 1, 1, EnemyTargetRule.FirstLiving, "검사", 1, 1, 1, 0, chance, 20, roll); // 게임 내 기존 적 행동 원본 규칙과 테스트 난수 전달
    } // 기존 적 공격 및 상태 행동 생성 종료

    private static void Acquire(string key) // 실제 탐사 보관함의 유물 획득 경로 사용
    { // 실제 유물 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 등록된 실제 원본 유물 경로 조회
        Assert.IsNotNull(relic, key); // 이름이 연결된 정상 자산 존재 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 정상 회차에 원본 유물 보유 항목 등록
    } // 실제 유물 획득 종료

    private sealed class OpeningBattle : IDisposable // 실제 전투 시작 전에 조건을 설치하는 독립 전투 환경
    { // 시작과 종료 전투 상태 구성 시작
        private readonly CharacterData[] characters = Enumerable.Range(0, 3).Select(_ => ScriptableObject.CreateInstance<CharacterData>()).ToArray(); // 살아 있는 아군과 전투불능 비교 원본
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 실제 무작위 선택과 전투 적 생성 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 전투 덱 구성에 필요한 임시 카드 원본
        private readonly DeckData source; // 실제 런타임 덱의 원본 자료
        private readonly BattleStatusEffectController statuses; // 적과 아군의 실제 상태 종료 담당
        private readonly Random.State originalRandom = Random.state; // 독립 검사 전 원래 게임 난수 보관
        internal readonly BattleUnitRuntime[] Allies; // 실제로 참가한 아군 목록
        internal readonly BattleUnitRuntime[] Enemies; // 실제로 참가한 적 목록
        internal readonly BattleTurnRuntime Turns; // 출전과 턴 및 전투 결과를 담당하는 관리자

        internal OpeningBattle(BattleType battleType = BattleType.Normal) // 입력한 전투 종류의 실제 독립 전투 구성
        { // 테스트용 참가자와 턴 처리 구성 시작
            foreach (var character in characters) // 정확한 최대 체력과 정신력이 필요한 아군 순회
            { // 실제 검사 아군 원본 설정 시작
                JsonUtility.FromJsonOverwrite("{\"maxHealth\":100,\"initialMental\":50}", character); // 고정 최대 체력과 중립 정신력 적용
            } // 실제 검사 아군 원본 설정 종료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":100}", enemy); // 전투 전 죽일 수 있는 적 기본 체력 설정
            Allies = characters.Select(BattleUnitRuntime.CreateAlly).ToArray(); // 독립 아군 원본으로 실제 전투 유닛 생성
            Enemies = Enumerable.Range(0, 4).Select(_ => BattleUnitRuntime.CreateEnemy(enemy)).ToArray(); // 배치에 따른 선택 검증용 적 넷 생성
            source = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, characters[0]) }); // 추가 카드를 실제 덱에 연결
            var deck = BattleDeckRuntime.Create(source, Allies, 4, 260); // 전역 난수를 건드리지 않는 원래 실제 덱 구성
            Turns = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), Allies, Enemies, 0, battleType); // 실제 적과 아군의 진영 턴 및 결과 관리자
            statuses = new BattleStatusEffectController(Turns, Allies, Enemies, new BattleStatusEffectProcessor()); // 개막 후 적 턴에 기존 약화 기간이 감소하는 경로 연결
        } // 테스트용 참가자와 턴 처리 구성 종료

        public void Dispose() // 실제 검사 종료 시 연결과 임시 원본 해제
        { // 검사 원본 정리 시작
            statuses.Dispose(); // 진영별 상태 효과 이벤트 연결 해제
            Turns.Dispose(); // 전투 종료와 결과 적용 구독 해제
            Object.Destroy(source); // 테스트용 덱 원본 정리
            Object.Destroy(card); // 테스트용 카드 원본 정리
            Object.Destroy(enemy); // 테스트용 적 원본 정리
            foreach (var character in characters) // 임시 아군 원본 순회
            { // 임시 아군 원본 정리 시작
                Object.Destroy(character); // 플레이어 원본과 구분된 임시 객체 제거
            } // 임시 아군 원본 정리 종료
            Random.state = originalRandom; // 독립 검사에 사용한 전역 난수 복원
        } // 검사 원본 정리 종료
    } // 시작과 종료 전투 상태 구성 종료
} // 시작 및 종료 유물 회귀 검사 종료
