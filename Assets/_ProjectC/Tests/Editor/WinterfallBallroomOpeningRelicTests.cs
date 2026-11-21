using System; // 테스트 전투의 실제 자원 해제
using System.Collections; // 실제 회차가 대기하는 플레이 모드 전환
using System.Linq; // 원본 카드와 생성한 실제 카드 식별
using NUnit.Framework; // 체력 및 명중과 손패 결과 검증
using UnityEngine; // 실제 Unity 카탈로그와 일시 자료 사용
using UnityEngine.TestTools; // 기존 전투 실행 환경 진입
using Object = UnityEngine.Object; // 테스트 전투에서 만든 원본만 해제

public sealed class WinterfallBallroomOpeningRelicTests // 회복·명중과 첫 실제 손패의 유물 회귀 검사
{ // 무도회 유물 실제 검사 시작
    [UnityTest] // 받는 실제 체력 회복과 상태 발생 순서 확인
    public IEnumerator EmbroideryUsesOnlyPreHealPositiveBuffsAndKeepsNativeCaps() // 기존 회복 보정과 새 버프 조건을 실제 체력으로 대조
    { // 회복 유물 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 유물 관리자와 회차 자료를 사용할 모드 진입
        VerifyHealing(); // 회복 후 생긴 각성과 실제 활성 강화의 시점 차이 확인
        yield return new ExitPlayMode(); // 독립 유물 회차 정리 뒤 편집 모드 복귀
    } // 회복 유물 플레이 모드 검사 종료

    private static void VerifyHealing() // 실제 회복을 받는 생존자와 기존 제한 직접 검증
    { // 받는 회복 수치 검사 시작
        Acquire("GoldEmbroideryScrap"); // 처음부터 보유한 실제 금실 자수 조각
        using var fight = new OpeningBattle(3); // 기존 체력 및 정신력 상태가 작동하는 독립 전투
        var ally = fight.Allies[1]; // 실제 체력이 남은 생존 회복 수신자
        Assert.IsTrue(ally.ApplyPersistentHealth(10)); // 충분한 회복 공간을 갖는 실제 체력 기준 준비
        Assert.IsTrue(ally.ApplyPersistentMental(99)); // 바로 다음 정상 회복에서 각성이 발생할 정신력 준비
        Assert.AreEqual(20, ally.RestoreHealth(20)); // 회복 후 생긴 상태가 이번 회복을 소급해 높이지 않음 확인
        Assert.AreEqual(BattleMentalState.Awakening, ally.MentalState); // 기존 회복 정신력 변화로 각성이 실제로 발생 확인
        Assert.IsTrue(ally.ApplyPersistentHealth(10)); // 이미 각성이 있는 다음 실제 회복에 공간 준비
        Assert.AreEqual(23, ally.RestoreHealth(20)); // 기존 긍정 상태가 있던 실제 회복의 15% 증가 확인
        Assert.IsTrue(ally.ApplyPersistentMental(0)); // 활성 붕괴 상태만 있는 부정 정신력 대조 준비
        ally.ApplyStatusEffect(BattleStatusEffectType.AttackPowerDown, 1, 2, 1); // 추가 부정 상태만 있는 회복 수신
        Assert.IsTrue(ally.ApplyPersistentHealth(10)); // 다시 같은 체력 공간으로 기존 비교값 고정
        Assert.AreEqual(20, ally.RestoreHealth(20)); // 붕괴와 약화만 있는 실제 회복에 증가 없음 확인
        Assert.IsTrue(ally.ApplyPersistentMental(50)); // 보호 정신 상태가 없는 일반 대상 준비
        ally.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 4, 1, 1); // 회복 전에 만료한 긍정 상태 비교
        ally.GetStatusEffect(BattleStatusEffectType.AttackPowerUp).AdvanceDuration(); // 실제 상태 인스턴스의 만료 처리
        Assert.IsTrue(ally.ApplyPersistentHealth(10)); // 만료된 상태를 유지한 상태에서 충분한 회복량 준비
        Assert.AreEqual(20, ally.RestoreHealth(20)); // 만료된 강화만 남으면 유물 적용 없음 확인
        ally.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedUp, 20, 2, 1); // 기존 받는 회복 증가를 후속 유물 보정과 합산
        ally.ApplyStatusEffect(BattleStatusEffectType.HealingReceivedDown, 25, 2, 1); // 원래 있는 받는 회복 감소도 같은 계산에 포함
        ally.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 1, 2, 1); // 긍정 상태 둘 이상에서도 한 번만 증가 확인
        Assert.IsTrue(ally.ApplyPersistentHealth(10)); // 일반 회복이 기존 상한에 막히지 않을 체력 준비
        int reported = 0; // 기존 회복 신호가 보고한 최종 실제 양
        ally.HealthRestored += (_, applied) => reported += applied; // 전투 상태가 받는 원래 실제 체력 회복 신호 구독
        Assert.AreEqual(41, ally.RestoreHealth(40)); // 40과 받은 증가·감소 및 15%를 함께 계산한 실제 내림값 확인
        Assert.AreEqual(41, reported); // 중복 회복 없이 한 번의 원래 실제 치료량만 전달 확인
        Assert.IsTrue(ally.ApplyPersistentHealth(ally.MaxHealth - 2)); // 실제 증가량보다 작은 남은 체력 준비
        Assert.AreEqual(2, ally.RestoreHealth(100)); // 유물 효과가 최대 체력 상한을 넘지 않음 확인
        Assert.Zero(ally.RestoreHealth(100)); // 최대 체력에서 실제 추가 회복 없음 확인
        var enemy = fight.Enemies[0]; // 같은 긍정 효과를 받지만 아군 유물이 대상에서 제외할 적
        enemy.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 2, 2, 1); // 적에게도 유효한 실제 활성 강화 부여
        enemy.TakeDamage(30, BattleDamageType.None); // 적의 회복에도 충분한 실제 피해 적용
        Assert.AreEqual(20, enemy.RestoreHealth(20)); // 적의 실제 받는 회복은 기존 수치만 적용
    } // 받는 회복 수치 검사 종료

    [UnityTest] // 공격자의 활성 중독과 실제 명중률 경계 확인
    public IEnumerator WineChangesOnlyLivingPoisonedEnemyAccuracyBeforeTheRoll() // 명중률 표시와 실제 굴림의 동일한 값 검증
    { // 포도주 얼룩 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 보유 유물과 공격자 상태를 사용할 환경 진입
        VerifyAccuracy(); // 중독과 회피에 따른 실제 백분율 경계 검증
        yield return new ExitPlayMode(); // 독립 전투 상태와 유물 정리 뒤 편집 모드 복귀
    } // 포도주 얼룩 플레이 모드 검사 종료

    private static void VerifyAccuracy() // 중독된 적의 실제 사전 명중 계산 확인
    { // 포도주 얼룩의 명중 경계 검증 시작
        Acquire("RedWineStain"); // 실제 붉은 포도주 얼룩 원본 보유
        using var fight = new OpeningBattle(3); // 두 진영 유닛의 원래 상태 기능을 쓰는 독립 전투
        var enemy = fight.Enemies[0]; // 상대에게 명중을 굴리는 실제 적
        var ally = fight.Allies[1]; // 기존 회피를 지닌 실제 아군 목표
        ally.ApplyStatusEffect(BattleStatusEffectType.Evasion, 20, 2, 1); // 대상의 기존 회피율 준비
        enemy.ApplyStatusEffect(BattleStatusEffectType.Blind, 10, 2, 1); // 적 자체의 기존 명중률 감소 준비
        enemy.ApplyStatusEffect(BattleStatusEffectType.Disorientation, 4, 2, 1); // 별개 기존 명중률 감소 준비
        Assert.AreEqual(66, BattleHitChance.GetPercent(enemy, ally)); // 서로 다른 기존 요인을 포함한 중독 이전 기준
        enemy.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 1, 1); // 실제 행동 전 남아 있는 적 중독
        Assert.AreEqual(61, BattleHitChance.GetPercent(enemy, ally)); // 기존 요인 합계에서 정확한 오 퍼센트포인트 추가 감소 확인
        Assert.AreEqual(81, BattleHitChance.GetPercent(enemy)); // 대상을 생략해도 공격자 중독 보정 유지 확인
        Assert.IsTrue(BattleHitChance.Hits(enemy, 60, ally)); // 실제 판정의 감소된 최대 성공 경계 확인
        Assert.IsFalse(BattleHitChance.Hits(enemy, 61, ally)); // 실제 판정의 첫 실패 경계 확인
        var poison = enemy.GetStatusEffect(BattleStatusEffectType.Poison); // 원래 적 중독의 실제 상태 인스턴스
        poison.AdvanceDuration(); // 현재 목록에 남아도 활성은 끝난 실제 중독 준비
        Assert.AreEqual(66, BattleHitChance.GetPercent(enemy, ally)); // 만료된 중독에서 명중률 정상 복원 확인
        ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 2, 2, 1); // 반대 진영의 실제 활성 중독 준비
        Assert.AreEqual(100, BattleHitChance.GetPercent(ally, enemy)); // 중독 아군의 명중률 건드림 없음 확인
        enemy.TakeDamage(1000, BattleDamageType.None); // 실제 죽은 적은 공격할 수 없는 상태 확인 준비
        Assert.AreEqual(0, BattleHitChance.GetPercent(enemy, ally)); // 전투불능 공격의 기존 확률 영 유지 확인
    } // 포도주 얼룩의 명중 경계 검증 종료

    [UnityTest] // 전투 첫 실제 손패와 임시 카드의 기본 전투 수명 검사
    public IEnumerator ButtonCreatesOneFreeThrustAfterAmbushAndHonorsHandCapacity() // 원본 찌르기의 무료 전투용 복사 및 종료 정리 검증
    { // 귀족 단추 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 덱과 유물 회차를 쓰는 환경 진입
        VerifyOpeningCard(); // 선제 턴과 손패 상한 및 원래 저장 덱 유지를 확인
        yield return new ExitPlayMode(); // 독립 전투 정리 뒤 편집 모드 복귀
    } // 귀족 단추 플레이 모드 검사 종료

    private static void VerifyOpeningCard() // 실제 첫 아군 손패 및 일회용 전투 카드 상태 검증
    { // 임시 찌르기 검증 시작
        Acquire("NobleBlackButton"); // 현재 회차의 실제 귀족의 검은 단추 보유
        using (var fight = new OpeningBattle(3)) // 두 번째 아군부터 생존하고 손패에 여유가 있는 승부
        { // 선제 뒤 정상 지급 및 실제 사용 확인 시작
            Assert.IsTrue(fight.Allies[0].ApplyPersistentHealth(0)); // 먼저 배치됐지만 쓰러진 아군은 카드 소유자에서 제외
            Assert.IsTrue(fight.Turns.StartBattle(1, true)); // 적이 실제로 먼저 행동하는 원래 전투 시작
            Assert.Zero(fight.Deck.HandCount); // 선제 적의 턴에 임시 카드가 미리 만들어지지 않음 확인
            int firstDrawCount = 0; // 임시 카드를 넣기 전 정상 첫 손패의 실제 수
            fight.Deck.StateChanged += () => // 이미 만든 카드의 정상 손패 도착 순서 구독
            { // 첫 드로우 당시 손패 등록 범위 확인 시작
                if (firstDrawCount == 0 && fight.Deck.HandCount > 0) // 첫 실제 일반 드로우 알림 확인
                { // 초기 일반 손패 수 보관 시작
                    firstDrawCount = fight.Deck.HandCount; // 원본 카드의 먼저 도착한 실제 개수 기록
                } // 초기 일반 손패 수 보관 종료
            }; // 첫 드로우 당시 손패 등록 범위 확인 종료
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 적 선제 뒤 실제 첫 아군 턴과 드로우 실행
            Assert.AreEqual(1, firstDrawCount); // 원래 드로우 한 장이 임시 카드보다 먼저 들어온 결과 확인
            Assert.AreEqual(2, fight.Deck.HandCount); // 정상 한 장에 임시 원본 한 장만 더한 실제 손패 확인
            var generated = fight.Deck.Hand.Single(card => card.ApCost == 0); // 원래 유료 찌르기와 구별되는 실제 생성 카드
            Assert.AreSame(fight.Thrust, generated.SourceData); // 룰이나 효과가 바뀌지 않은 실제 찌르기 원본 사용 확인
            Assert.AreSame(fight.Allies[1], generated.OwnerUnit); // 현재 첫 생존 아군에게 원본 카드 소유권 전달 확인
            Assert.AreEqual(0, generated.UpgradeLevel); // 기존 회차의 카드 강화와 무관한 표준 원본 단계 확인
            Assert.AreEqual("0", generated.CostLabel); // 정상 손패에 무료 비용을 렌더링하는 원본 표시 확인
            Assert.AreEqual(7, generated.EffectValue); // 즉시 쓸 수 있는 실제 표준 찌르기 피해 수치 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 쓰기 전에 실제 턴 종료 및 원래 손패 버림
            CollectionAssert.Contains(fight.Deck.DiscardPile, generated); // 미사용 무료 찌르기의 전투 중 재순환 확인
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 원래 버린 더미를 재사용하는 두 번째 정상 아군 턴
            CollectionAssert.Contains(fight.Deck.Hand, generated); // 생성된 동일 카드를 실제 손패로 다시 뽑았는지 확인
            Assert.Zero(generated.ApCost); // 턴별 임시 할인 정리가 지나도 생성 카드의 무료 비용 유지 확인
            Assert.AreEqual(2, fight.Deck.CardCount); // 두 번째 턴에는 새로운 사본을 만들지 않음을 확인
            Assert.IsTrue(fight.Deck.TryPlayAndDraw(generated, 0, out int drawn)); // 정상 카드 사용 이동을 통한 일회용 효과 확인
            Assert.Zero(drawn); // 기본 찌르기 자체에 없는 별도 드로우가 생기지 않는지 확인
            CollectionAssert.Contains(fight.Deck.ExcludedPile, generated); // 실제 사용 카드가 재순환 불가한 제외 더미로 이동 확인
            Assert.IsTrue(fight.Turns.TryEscape()); // 기존 실제 전투 종료 경로로 나가기
            Assert.AreEqual(1, fight.Deck.CardCount); // 전투 종료 직후 원본 회차 카드만 남도록 임시 사본 정리 확인
            CollectionAssert.DoesNotContain(fight.Deck.ExcludedPile, generated); // 제외 더미에 남은 실제 임시 사본까지 제거 확인
            Assert.AreEqual(1, fight.Source.Cards.Count); // 저장할 원본 덱에는 전투 동안 추가가 없음을 확인
        } // 선제 뒤 정상 지급 및 실제 사용 확인 종료
        using (var full = new OpeningBattle(1)) // 기본 시작 카드로 이미 손패 상한에 도달하는 전투
        { // 상한에 막힌 실제 유물 행동 검증 시작
            Assert.IsTrue(full.Turns.StartBattle(1)); // 실제 일반 손패를 한 장으로 정상 채운 전투 시작
            Assert.AreEqual(1, full.Deck.HandCount); // 임시 카드가 기존 정상 손패를 밀어내지 않는지 확인
            Assert.AreEqual(1, full.Deck.CardCount); // 손패 상한에서 숨은 임시 사본도 생성하지 않음 확인
        } // 상한에 막힌 실제 유물 행동 검증 종료
        using var empty = new OpeningBattle(3); // 기본 드로우를 요청하지 않은 실제 시작을 대조할 독립 전투
        Assert.IsTrue(empty.Turns.StartBattle(0)); // 기본 카드 드로우가 영인 정상 전투 시작
        Assert.AreEqual(1, empty.Deck.HandCount); // 실제 손패 드로우가 없어도 유물 카드 한 장 생성 확인
        Assert.Zero(empty.Turns.LastDrawnCardCount); // 생성 효과는 기존 정상 드로우 수에 포함하지 않음 확인
    } // 임시 찌르기 검증 종료

    private static void Acquire(string key) // 원본 리소스의 실제 유물 획득
    { // 보유 유물 준비 시작
        var relic = Resources.Load<RelicData>("Relics/" + key); // 시트에서 연결한 실제 유물 원본 읽기
        Assert.IsNotNull(relic, key); // 실제 리소스 및 메타데이터 연결 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 정상 회차 상태에 원본 유물 등록
    } // 보유 유물 준비 종료

    private sealed class OpeningBattle : IDisposable // 실제 턴과 손패 및 두 진영의 공통 테스트 환경
    { // 실제 전투 구성 시작
        private readonly CharacterData[] characters = Enumerable.Range(0, 2).Select(_ => ScriptableObject.CreateInstance<CharacterData>()).ToArray(); // 첫 생존 아군을 확인할 별개 원본 생성
        private readonly EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>(); // 독립 전투에 사용할 실제 적 원본
        private readonly BattleStatusEffectController statuses; // 원래 진영별 상태 수명을 처리하는 관리자
        internal readonly BattleUnitRuntime[] Allies; // 현재 두 생존 가능 아군의 실제 체력과 상태
        internal readonly BattleUnitRuntime[] Enemies; // 실제 적의 명중 및 회복 대상
        internal readonly DeckData Source; // 실제 유지해야 할 원본 덱
        internal readonly CardData Thrust; // 게임이 가지고 있는 실제 기본 찌르기 카드 원본
        internal readonly BattleTurnRuntime Turns; // 개시와 선제 및 손패 정리를 사용한 실제 전투 관리자
        internal readonly BattleDeckRuntime Deck; // 현재 개별 카드의 실제 전투 더미와 손패

        internal OpeningBattle(int capacity) // 실제 전투를 다른 손패 상한으로 준비
        { // 실제 상대 및 전투 원본 준비 시작
            foreach (var character in characters) // 두 아군의 다양한 회복 검증용 정상 원본 설정
            { // 개별 기본 체력과 정신력 설정 시작
                JsonUtility.FromJsonOverwrite("{\"maxHealth\":100,\"initialMental\":50}", character); // 회복 배율을 정확히 검증할 넉넉한 체력 자료 적용
            } // 개별 기본 체력과 정신력 설정 종료
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":100,\"initialMental\":50}", enemy); // 실제 적 정산에 사용할 일반 초기 수치 적용
            var catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 전투가 원본을 찾는 동일 실제 카탈로그
            Assert.IsNotNull(catalog); // 같은 원본 출처에 연결되었는지 확인
            Thrust = catalog.Cards.Single(card => card.CardId == "CRD_SHEET_THRUST"); // 실제 원본 찌르기 카드 확보
            Allies = characters.Select(BattleUnitRuntime.CreateAlly).ToArray(); // 실제 전투 아군 두 명 생성
            Enemies = new[] { BattleUnitRuntime.CreateEnemy(enemy) }; // 현재 전투의 살아 있는 적 등록
            Source = DeckData.CreateRuntime(new[] { new DeckCardEntry(Thrust, characters[1]) }); // 저장 원본의 유료 찌르기 한 장만 구성
            Deck = BattleDeckRuntime.Create(Source, Allies, capacity, 264); // 정상 본인 소유자와 손패 상한의 실제 전투 덱
            Turns = new BattleTurnRuntime(Deck, new BattleActionPointRuntime(3), Allies, Enemies, 2, BattleType.Normal); // 다음 아군 턴에 기존 두 카드의 순환과 일반 도주 경로 연결
            statuses = new BattleStatusEffectController(Turns, Allies, Enemies, new BattleStatusEffectProcessor()); // 실제 턴 기반 상태 정산 연결
        } // 실제 상대 및 전투 원본 준비 종료

        public void Dispose() // 실제 연결 및 이번 검사 자체 원본만 정리
        { // 테스트 전투 자원 정리 시작
            statuses.Dispose(); // 기존 상태 적용과 만료 이벤트 해제
            Turns.Dispose(); // 실제 전투 이벤트와 전투 한정 자원 해제
            Object.Destroy(Source); // 이번 검사에서 만든 원본 덱 제거
            Object.Destroy(enemy); // 임시 적 원본 정리
            foreach (var character in characters) // 이번 검사용 아군 자료만 순회
            { // 생성 아군 원본 정리 시작
                Object.Destroy(character); // 전투 외부의 원본 캐릭터에 영향 없는 실제 정리
            } // 생성 아군 원본 정리 종료
        } // 테스트 전투 자원 정리 종료
    } // 실제 전투 구성 종료
} // 무도회 유물 실제 검사 종료
