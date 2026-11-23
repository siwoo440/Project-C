using System; // 실제 개막 검사 전투의 상태 정리
using System.Collections; // 실제 Unity 플레이 모드 수명
using System.Linq; // 살아 있는 적과 검사 파티 원본 순회
using NUnit.Framework; // 피해와 상태 처리의 동작 확인
using UnityEngine; // 실제 캐릭터와 유물 원본 조회
using UnityEngine.TestTools; // 일반 게임의 플레이 환경 전환
using Fight = WinterfallSwordCombatTests.Fight; // 기존 게임 카드와 실제 교전의 공용 검사기
using Object = UnityEngine.Object; // 검사에 사용한 임시 Unity 원본 정리

public sealed class WinterfallBurningAndDamageRelicTests // 삼종 유물의 실제 피해와 개막 상태 회귀 검사
{ // 정상 전투 효과 검사 시작
    [UnityTest] // 양수 화상 대상과 원자로의 독립 배율 및 네이티브 지속 피해 검사
    public IEnumerator RavineHeartBoostsAllRealBurningEnemyDamageWithoutChangingNativeOwnership() // 실제 직접·무출처·방어 우회와 상태 해제의 경계 확인
    { // 불타는 협곡 심장편 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 객체와 유물 보유 환경 진입
        var heart = Acquire("ScorchedRavineHeart"); // 독립 출시 원본의 피해 증가량
        var reactor = Acquire("ReactorHeartShard"); // 직접 피해의 앞선 별개 원자로 배율
        Assert.AreEqual(20, heart.EffectValue); // 심장편 원본의 피해 이십 퍼센트 확인
        Assert.AreEqual(15, reactor.EffectValue); // 원자로 원본의 별개 십오 퍼센트 확인
        var detachedData = ScriptableObject.CreateInstance<EnemyData>(); // 현재 실제 교전에 참가하지 않은 적 원본
        var detached = BattleUnitRuntime.CreateEnemy(detachedData); // 타깃 자체에 붙은 화상과 비전투 상태 비교
        detached.ApplyStatusEffect(BattleStatusEffectType.Burn, 5, 2, 1); // 전투에 연결하지 않은 대상의 화상 상태
        Assert.AreEqual(20, detached.PreviewDamage(20, BattleDamageType.None).FinalDamage); // 실제 참가 조우가 없는 피해의 원래 수치 보존
        Object.Destroy(detachedData); // 실제 경기와 별개의 검사 원본 정리
        using (var fight = new Fight("THRUST")) // 실제 전투 파티와 적 및 상태 처리 환경
        { // 심장편 대상과 지속 피해를 구분한 실제 전투 시작
            var actor = fight.Allies[0]; // 실제 카드 직접 명중의 살아 있는 아군 발생자
            var burning = fight.Enemies[0]; // 활성 화상 상태인 실제 적 피격자
            burning.ApplyStatusEffect(BattleStatusEffectType.Burn, 5, 2, 1); // 직접과 고정 및 미사용 발생자 검사에 같은 실제 상태
            var preview = burning.PreviewDamage(100, BattleDamageType.Physical, actor, alliedDirectAttack: true, directCardType: CardType.Sword); // 실제 원자로 직접 보정과 심장편의 순수 원본 예고
            Assert.AreEqual(138, preview.FinalDamage); // 서로 독립인 원자로 십오와 심장편 이십의 연속 내림
            Assert.AreEqual(138, burning.TakeDamage(100, BattleDamageType.Physical, actor, alliedDirectAttack: true).AppliedDamage); // 같은 양을 실제 방어 계산과 유닛 체력 감소에서 정산 확인
            Assert.AreEqual(10, burning.TakeDamage(9, BattleDamageType.None).AppliedDamage); // 실제 발생자가 없는 기본 양수 피해의 내림 확인
            burning.GainBlock(5); // 방어도 우회 시 유지할 실제 방어 수치
            Assert.AreEqual(12, burning.TakeUnblockedDamage(10, BattleDamageType.Magical, null)); // 독립 마법 고정 피해에도 같은 화상 대상의 배율 확인
            Assert.AreEqual(5, burning.CurrentBlock); // 원래 방어도 우회와 무관한 비율 증가 확인
            Assert.AreEqual(0, burning.TakeDamage(0, BattleDamageType.None).AppliedDamage); // 원래 영 피해에 보정 없음 확인
            burning.RemoveStatusEffect(BattleStatusEffectType.Burn); // 현재 화상을 실제 해제해 남은 적 조건 비교
            Assert.AreEqual(9, burning.TakeDamage(9, BattleDamageType.None).FinalDamage); // 같은 적의 화상 상실 뒤 방어도 차감의 원래 최종 수치 확인
            var periodic = fight.Enemies[1]; // 화상과 출혈을 실제 적 턴 종료에 받을 별개 적
            periodic.ApplyStatusEffect(BattleStatusEffectType.Burn, 5, 2, 1); // 첫 양수 화상과 유지되는 화상 피해 대상
            periodic.ApplyStatusEffect(BattleStatusEffectType.Bleed, 5, 1, 1); // 죽지 않는 일반 출혈의 방어 우회 피해
            int before = periodic.CurrentHealth; // 적 종료의 두 상태 처리 전 실제 체력 보관
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 실제 적 턴 시작과 종료의 일반 상태 처리
            Assert.AreEqual(before - 12, periodic.CurrentHealth); // 기존 화상과 출혈의 각각 양수 여섯 피해 확인
        } // 심장편 대상과 지속 피해를 구분한 실제 전투 종료
        yield return new ExitPlayMode(); // 실제 전투 상태 해제
    } // 불타는 협곡 심장편 검사 종료

    [UnityTest] // 기존 물리 방어와 마법 저항 뒤 실제 피해 비율 및 고정 피해 종류 검사
    public IEnumerator MountainShoulderAgreesAcrossPreviewDirectAndIndependentBypass() // 기존 방어도와 일반·침식 및 적 피해의 구분 확인
    { // 산맥석상 어깨조각의 실제 계산 시작
        yield return new EnterPlayMode(); // 일반 실제 방어 계산 환경 진입
        var shoulder = Acquire("MountainStatueShoulder"); // 아군의 수신 유형 보정 원본
        Assert.AreEqual(15, shoulder.EffectValue); // 실제 물리 감소의 기록된 퍼센트 확인
        using (var fight = new Fight("THRUST")) // 기본 방어 수치와 열려 있는 실제 전투
        { // 기존 방어와 초과분을 비교할 세 아군 확인 시작
            var physical = fight.Allies[0]; // 원래 물리 방어의 실제 아군 검사자
            var magic = fight.Allies[1]; // 원래 마법 저항의 실제 아군 검사자
            var plain = fight.Allies[2]; // 방어 없는 일반과 침식 피해의 실제 아군 검사자
            var opponent = fight.Enemies[0]; // 모든 비교에 쓰는 실제 적 발생자
            const int raw = 20; // 배율 변화가 네이티브 정수에서 확인되는 양수 피해
            int afterPhysical = Math.Max(1, (int)Math.Floor(raw * 100d / (100d + physical.PhysicalDefense) * 0.85d)); // 기존 물리 방어와 이후 한 번만 적용할 배율의 실제 정수 내림
            int afterMagic = Math.Max(1, (int)Math.Floor(raw * 100d / (100d + magic.MagicalResistance) * 1.05d)); // 기존 저항 뒤 독립 오 퍼센트 증가의 실제 정수 내림
            physical.GainBlock(3); // 별개로 차감해야 할 기존 방어도 지급
            var physicalHint = physical.PreviewDamage(raw, BattleDamageType.Physical, opponent, enemyDirectAttack: true); // 기존 물리 피해와 같은 순수 아군 예고
            var physicalHit = physical.TakeDamage(raw, BattleDamageType.Physical, opponent, enemyDirectAttack: true); // 다른 수치로 계산하지 않는 실제 피격 처리
            Assert.AreEqual(afterPhysical - 3, physicalHint.FinalDamage - physicalHint.AbsorbedDamage); // 실제 배율 이후 정식 흡수 순서 확인
            Assert.AreEqual(physicalHint.AppliedDamage, physicalHit.AppliedDamage); // 물리 피격의 예고와 체력 감소가 동일한지 확인
            var magicHint = magic.PreviewDamage(raw, BattleDamageType.Magical, opponent, enemyDirectAttack: true); // 마법 저항 뒤 받는 배율의 실제 예고
            Assert.AreEqual(afterMagic, magicHint.FinalDamage); // 물리 감소와 별개의 마법 오 퍼센트 증가 확인
            Assert.AreEqual(magicHint.AppliedDamage, magic.TakeDamage(raw, BattleDamageType.Magical, opponent, enemyDirectAttack: true).AppliedDamage); // 마법 예고와 실제 체력 피해의 공유 순서
            Assert.AreEqual(7, plain.PreviewDamage(7, BattleDamageType.None).FinalDamage); // 원래 일반 및 고정 수치의 배율 제외
            Assert.AreEqual(7, plain.PreviewDamage(7, BattleDamageType.Corrosion).FinalDamage); // 독립 침식 피해의 원래 수치 보존
            Assert.AreEqual(8, plain.TakeUnblockedDamage(10, BattleDamageType.Physical, opponent)); // 방어를 우회한 명시 물리 피해의 정수 배율
            Assert.AreEqual(20, opponent.PreviewDamage(20, BattleDamageType.Physical).FinalDamage); // 적 자신의 물리 수신에는 어깨조각 미적용 확인
        } // 기존 방어와 초과분을 비교할 세 아군 확인 종료
        yield return new ExitPlayMode(); // 실제 수신 피해 대조 환경 종료
    } // 산맥석상 어깨조각의 실제 계산 종료

    [UnityTest] // 실제 적 선제의 예고 생성 이전에 적 두 차례와 선두 생존 아군 한 차례 화상 검사
    public IEnumerator ThroneBurnRunsBeforeFirstEnemyIntentAndUsesTwoEnemyTurnsWithOneAllyPenalty() // 생존, 면역, 선택 대가 및 일반 화상의 독립 수명 확인
    { // 용암왕 왕좌파편 실제 개막 검사 시작
        yield return new EnterPlayMode(); // 실제 기존 전투 상태와 개막 콜백 환경 준비
        var throne = Acquire("LavaKingThroneShard"); // 배포하는 실제 용암왕 개막 유물
        Assert.AreEqual(1, throne.EffectValue); // 실제 두 적 차례의 원본 화상 한 점
        Assert.AreEqual(2, throne.StatusDuration); // 원본에 기록한 자기 차례 두 번
        using (var trial = new OpeningTrial()) // 개막 전 실제 아군과 적 및 면역을 갖춘 비교 전투
        { // 실제 적 선제와 화상 상태 비교 시작
            var team = trial.Allies; // 첫 아군 전투불능과 다음 생존자 대가 비교
            var enemies = trial.Enemies; // 면역 및 일반 화상에서 서로 다른 네 적 준비
            team[0].TakeDamage(500, BattleDamageType.None); // 선두에서 이미 전투불능인 실제 아군 제외
            enemies[2].ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 3, 1); // 원래 화상 면역 대상의 실제 상태
            enemies[3].TakeDamage(500, BattleDamageType.None); // 기존 전투불능에 적용하지 않는지 확인할 적
            int enemyHealth = enemies[0].CurrentHealth; // 활성 화상 두 번을 비교할 원본 적 체력
            int allyHealth = team[1].CurrentHealth; // 선두 생존 대가를 확인할 실제 아군 체력
            bool readyAtIntent = false; // 첫 적 행동 예고 준비의 상태 결과
            Assert.IsTrue(trial.Turn.StartBattle(0, enemyFirst: true, openingEffects: () => readyAtIntent = enemies[0].GetStatusEffect(BattleStatusEffectType.Burn)?.RemainingTurns == 2)); // 실제 선제 예고의 기존 콜백과 상태 선행 확인
            Assert.IsTrue(readyAtIntent); // 최초 적 예고 만들기 전에 화상 상태 적용 확인
            Assert.AreEqual(1, enemies[0].GetStatusEffect(BattleStatusEffectType.Burn).EffectiveValue); // 두 적 차례 동안 첫 표시 수치 한 점
            Assert.AreEqual(1, enemies[1].GetStatusEffect(BattleStatusEffectType.Burn).EffectiveValue); // 순서와 무관한 모든 실제 생존 적 지급
            Assert.IsNull(enemies[2].GetStatusEffect(BattleStatusEffectType.Burn)); // 일반 약화 면역에 막힌 적의 화상 여부 확인
            Assert.IsNull(enemies[3].GetStatusEffect(BattleStatusEffectType.Burn)); // 기존 전투불능의 개막 상태 미적용
            Assert.IsNull(team[0].GetStatusEffect(BattleStatusEffectType.Burn)); // 전투불능 선두 아군의 미적용 확인
            Assert.AreEqual(1, team[1].GetStatusEffect(BattleStatusEffectType.Burn).RemainingTurns); // 첫 살아 있는 실제 아군만 한 차례 대가
            Assert.IsNull(team[2].GetStatusEffect(BattleStatusEffectType.Burn)); // 후순위 생존 아군의 실제 불이익 미적용
            Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 첫 적 턴 종료의 원래 화상 피해 적용
            Assert.AreEqual(enemyHealth - 1, enemies[0].CurrentHealth); // 첫 적 턴 종료의 실제 한 점 피해 확인
            Assert.AreEqual(1, enemies[0].GetStatusEffect(BattleStatusEffectType.Burn).RemainingTurns); // 원래 차례 중 마지막 한 적 턴의 유지
            Assert.AreEqual(1, enemies[0].GetStatusEffect(BattleStatusEffectType.Burn).EffectiveValue); // 두 번째 실제 한 점을 유지하는 개막 유물 수명
            Assert.AreEqual(allyHealth, team[1].CurrentHealth); // 적 턴 종료는 아군 화상 피해 시점에 영향 없음 확인
            Assert.IsTrue(trial.Turn.EndPlayerTurn()); // 첫 실제 아군 턴 종료의 정상 화상 처리
            Assert.AreEqual(allyHealth - 1, team[1].CurrentHealth); // 첫 실제 아군의 원래 한 점 화상 피해
            Assert.IsNull(team[1].GetStatusEffect(BattleStatusEffectType.Burn)); // 원래 한 아군 턴으로 끝나는 불이익 확인
            Assert.IsTrue(trial.Turn.CompleteEnemyTurn()); // 두 번째 적 턴의 실제 종료
            Assert.AreEqual(enemyHealth - 2, enemies[0].CurrentHealth); // 두 적 차례 각 한 점의 독립 피해 확인
            Assert.IsNull(enemies[0].GetStatusEffect(BattleStatusEffectType.Burn)); // 두 번째 적 턴 종료의 실제 화상 소진
        } // 실제 적 선제와 화상 상태 비교 종료
        yield return new ExitPlayMode(); // 용암왕 실제 선제 검사 환경 종료
    } // 용암왕 왕좌파편 실제 개막 검사 종료

    private static RelicData Acquire(string file) // 출시 유물의 원래 보유 방식으로 직접 획득
    { // 게임 데이터와 실제 회차 연결 시작
        var relic = Resources.Load<RelicData>("Relics/" + file); // 실제 배포 리소스의 원본 조회
        Assert.IsNotNull(relic, file); // 실제 유물 데이터 등록 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 현재 회차의 네이티브 보유 효과 등록
        return relic; // 실제 획득한 원본 수치 반환
    } // 게임 데이터와 실제 회차 연결 종료

    private sealed class OpeningTrial : IDisposable // 개막 전에 실제 적과 아군 상태를 준비할 독립 검사 전투
    { // 원래 양 진영과 상태 처리 초기화 시작
        private readonly CharacterData[] actors = new CharacterData[3]; // 별개 카드 소유자를 보관할 원본 아군
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 반복 화상 검사용 방어 없는 적 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 독립 일반 교전의 카드 덱 원본 항목
        private readonly DeckData source; // 실제 출전을 준비할 전투 덱
        private readonly BattleStatusEffectController statusController; // 정상 차례마다 실제로 등록된 상태 처리기
        internal readonly BattleUnitRuntime[] Allies; // 네이티브 아군의 생존과 상태 비교
        internal readonly BattleUnitRuntime[] Enemies; // 네이티브 적의 개별 상태 비교
        internal readonly BattleTurnRuntime Turn; // 실제 일반 조우의 진영 시작과 종료

        internal OpeningTrial() // 실제 원본을 쓰는 지역 상태 개막 구성
        { // 검사 참가자와 전투 시작 전 준비
            for (int index = 0; index < actors.Length; index++) // 서로 다른 실제 아군 원본 슬롯 생성
            { // 검사용 원본 캐릭터 생성 시작
                actors[index] = ScriptableObject.CreateInstance<CharacterData>(); // 진영 상태가 서로 다른 아군 원본
                JsonUtility.FromJsonOverwrite($"{{\"maxHealth\":100,\"characterId\":\"day288_opening_{index}\"}}", actors[index]); // 실제 체력과 고유 카드 소유자 지정
            } // 검사용 원본 캐릭터 생성 종료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":100}", enemy); // 두 실제 화상 틱을 받는 적 체력 초기화
            Allies = actors.Select(BattleUnitRuntime.CreateAlly).ToArray(); // 실제 출전 아군의 독립 현재 체력 생성
            Enemies = Enumerable.Range(0, 4).Select(_ => BattleUnitRuntime.CreateEnemy(enemy)).ToArray(); // 총 네 적의 독립 상태와 체력
            source = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, actors[1]) }); // 살아 있는 두 번째 소유자의 실제 덱 준비
            var deck = BattleDeckRuntime.Create(source, Allies, 6, 288); // 원래 게임의 실제 덱과 전투 손패
            Turn = new BattleTurnRuntime(deck, new BattleActionPointRuntime(5), Allies, Enemies, 0, BattleType.Normal); // 손패 대가 없는 실제 전투와 아군 상태 등록
            statusController = new BattleStatusEffectController(Turn, Allies, Enemies, new BattleStatusEffectProcessor()); // 원래 실제 진영 턴의 화상과 면역 처리 연결
        } // 검사 참가자와 전투 시작 전 준비 종료

        public void Dispose() // 실제 상태 구독과 검사용 데이터 정리
        { // 독립 전투 원본 해제 시작
            statusController.Dispose(); // 원래 차례의 상태 처리 구독 제거
            Turn.Dispose(); // 각 실제 전투 유닛에 남은 교전 연결 해제
            Object.Destroy(source); // 현재 경기의 실제 임시 덱 삭제
            Object.Destroy(card); // 독립 덱의 임시 카드 원본 정리
            Object.Destroy(enemy); // 독립 적의 원본 자료 정리
            foreach (var actor in actors) // 원래 서로 다른 카드 소유 원본 정리
            { // 개별 아군 원본 정리 시작
                Object.Destroy(actor); // 이번 검사에 사용한 실제 원본 제거
            } // 개별 아군 원본 정리 종료
        } // 독립 전투 원본 해제 종료
    } // 원래 양 진영과 상태 처리 초기화 종료
} // 정상 전투 효과 검사 종료
