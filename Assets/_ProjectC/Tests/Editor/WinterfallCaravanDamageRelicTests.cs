using System; // 검사 전투와 임시 원본 정리
using System.Collections; // 편집 모드와 실제 전투 검사 대기
using NUnit.Framework; // 실제 피해와 예고의 단언
using UnityEngine; // 유물 원본과 전투 참가자 생성
using UnityEngine.TestTools; // 플레이 모드 전환 검사
using Object = UnityEngine.Object; // 검사에서 생성한 Unity 데이터만 삭제

public sealed class WinterfallCaravanDamageRelicTests // 274일차의 실제 전투 피해 유물 회귀 검사
{ // 출혈과 화상 및 정예 조우 검사 시작
    [UnityTest] // 원래 방어도 규칙과 양쪽 진영의 지속 피해 대조
    public IEnumerator SaltLeatherAdjustsActualAllyTicksBeforeTheirOriginalDamageRoutes() // 아군 출혈과 화상의 반대 보정 확인
    { // 실제 보유와 상태 피해 검사 시작
        yield return new EnterPlayMode(); // 정상 유물 보유 환경 진입
        VerifyPeriodicTrade(); // 아군과 적의 실제 상태 처리 비교
        yield return new ExitPlayMode(); // 검사 자료 정리 후 편집 모드 복귀
    } // 실제 보유와 상태 피해 검사 종료

    [UnityTest] // 활성 화상이 있을 때만 공동 피해 계산을 증폭
    public IEnumerator BlackSunAshBoostsEligibleHitsAndRealStatusDamageByTheSameFloor() // 직접 예고와 일반 피해 및 상태 피해의 증폭 비교
    { // 적 화상 피해 회귀 시작
        yield return new EnterPlayMode(); // 원본 유물과 전투 상태 사용 환경
        VerifyAshDamage(); // 실제 활성 화상과 해제 뒤 직접 피해 비교
        yield return new ExitPlayMode(); // 원본 원상 복구와 편집 모드 전환
    } // 적 화상 피해 회귀 종료

    [UnityTest] // 정예 등급과 원래 방어도 및 자해 비용 대조
    public IEnumerator SecretLedgerChangesOnlyActualEliteAllyDamageBeforeBlock() // 정예의 실제 피해와 예고 동등성 확인
    { // 정예 피해 및 기존 비용 검사 시작
        yield return new EnterPlayMode(); // 실제 조우의 등급과 탐사 유물 보유 환경
        VerifyElitePenalty(); // 일반 및 보스와 정예에서 같은 타격 비교
        yield return new ExitPlayMode(); // 이번 검사 전투 정리 후 편집 모드
    } // 정예 피해 및 기존 비용 검사 종료

    private static void VerifyPeriodicTrade() // 소금가죽의 상태 틱과 방어도 실제 검사
    { // 소금가죽 틱 비교 시작
        using var relic = new OwnedRelic("SaltLeatherFragment"); // 원본 저장 카탈로그의 실제 소금가죽 보유
        using var fight = new Trial(BattleType.Normal); // 일반 조우의 두 아군과 한 적 전투 구성
        var statuses = new BattleStatusEffectProcessor(); // 기존의 정확한 턴 종료 경로
        var bleeding = fight.Ally; // 출혈만 받아 방어도를 보존할 생존 아군
        var burning = fight.Other; // 화상으로 기존 방어도를 잃는 생존 아군
        bleeding.GainBlock(3); // 출혈로 소모되지 않을 기존 방어도
        burning.GainBlock(3); // 화상 피해를 먼저 흡수하는 기존 방어도
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, bleeding.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1)); // 출혈이 보정 뒤 영이 되는 상태
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, burning.ApplyStatusEffect(BattleStatusEffectType.Burn, 3, 2, 1)); // 방어도와 정확히 같은 기존 양수 화상
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, fight.Enemy.ApplyStatusEffect(BattleStatusEffectType.Bleed, 3, 2, 1)); // 보정 범위 밖의 적 출혈
        statuses.ProcessPhaseEnd(bleeding, 1); // 아군 자신의 실제 지속 피해 처리
        statuses.ProcessPhaseEnd(burning, 1); // 기존 화상과 방어도 계산을 실제 처리
        statuses.ProcessPhaseEnd(fight.Enemy, 1); // 새 유물 효과 범위 밖의 적 상태 처리
        Assert.AreEqual(200, bleeding.CurrentHealth); // 출혈 한 점이 건강 손실 없이 소멸
        Assert.AreEqual(3, bleeding.CurrentBlock); // 기존 출혈의 방어도 우회 유지
        Assert.AreEqual(199, burning.CurrentHealth); // 화상 넷 가운데 세 점을 막은 실제 체력
        Assert.Zero(burning.CurrentBlock); // 기존 화상의 일반 방어 흡수 적용
        Assert.AreEqual(197, fight.Enemy.CurrentHealth); // 적의 출혈은 기존 세 점 손실
        Assert.AreEqual(2, burning.GetStatusEffect(BattleStatusEffectType.Burn)?.EffectiveValue); // 원래 상태 자체의 수치 감소 유지
    } // 소금가죽 틱 비교 종료

    private static void VerifyAshDamage() // 새 피해 증가와 기존 두 피해 경로 비교
    { // 화상 적의 실제 피해 비교 시작
        using var relic = new OwnedRelic("BlackSunAsh"); // 출시 유물의 실제 보유 상태
        using var fight = new Trial(BattleType.Normal); // 아군과 생존 적이 등록된 실제 전투
        var enemy = fight.Enemy; // 직접과 일반 피해를 받을 같은 실제 적
        Assert.AreEqual(25, enemy.PreviewDamage(25, BattleDamageType.Physical, fight.Ally, alliedDirectAttack: true).RawDamage); // 화상이 없는 원래 예고
        Assert.AreEqual(BattleStatusEffectApplyResult.Applied, enemy.ApplyStatusEffect(BattleStatusEffectType.Burn, 25, 2, 1, fight.Ally)); // 활성 화상 조건 생성
        enemy.GainBlock(5); // 증가 피해를 먼저 흡수할 기존 방어도
        BattleDamageResult preview = enemy.PreviewDamage(25, BattleDamageType.Physical, fight.Ally, alliedDirectAttack: true); // 아군 직접 타격의 순수 예고
        BattleDamageResult actual = enemy.TakeDamage(25, BattleDamageType.Physical, fight.Ally, alliedDirectAttack: true); // 같은 인수의 실제 아군 직격
        Assert.AreEqual(27, preview.RawDamage); // 25의 8퍼센트 증가를 내림한 수치
        Assert.AreEqual(preview.RawDamage, actual.RawDamage); // 같은 방어도 이전 원본 피해
        Assert.AreEqual(preview.AppliedDamage, actual.AppliedDamage); // 동일한 실제 체력 피해와 예고
        Assert.AreEqual(5, actual.AbsorbedDamage); // 원래 방어도는 증가 뒤에 흡수
        Assert.AreEqual(27, enemy.TakeDamage(25, BattleDamageType.None).AppliedDamage); // 카드 직격 아닌 일반 출처 없는 유효 피해
        int before = enemy.CurrentHealth; // 턴 종료 화상 실제 손실을 비교할 전 체력
        new BattleStatusEffectProcessor().ProcessPhaseEnd(enemy, 1); // 기존 화상 틱의 실제 게임 처리
        Assert.AreEqual(27, before - enemy.CurrentHealth); // 활성 상태 자체의 실제 틱에도 증가
        Assert.IsTrue(enemy.RemoveStatusEffect(BattleStatusEffectType.Burn)); // 활성 조건 제거로 같은 타격 복원
        Assert.AreEqual(25, enemy.TakeDamage(25, BattleDamageType.None).AppliedDamage); // 화상 해제 뒤 일반 피해의 원래 수치
    } // 화상 적의 실제 피해 비교 종료

    private static void VerifyElitePenalty() // 원본 조우의 정예 범위와 이미 계산된 방어 분담
    { // 정예 등급별 비교 시작
        using var relic = new OwnedRelic("CaravanSecretLedger"); // 자원과 정예 피해를 함께 가진 실제 비밀장부
        foreach (BattleType type in new[] { BattleType.Normal, BattleType.Boss, BattleType.Elite }) // 실제 조우 세 등급 순회
        { // 개별 전투의 동일 타격 비교 시작
            using var fight = new Trial(type); // 해당 등급이 명시된 실제 참가 등록
            var ally = fight.Ally; // 같은 방어를 가진 첫 피해 대상
            ally.GainBlock(5); // 피해 증가 이전 실제 방어도
            var preview = ally.PreviewDamage(40, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true); // 적 직접 피해의 순수 예고
            var actual = ally.TakeDamage(40, BattleDamageType.Physical, fight.Enemy, enemyDirectAttack: true); // 같은 적 직접 타격의 실제 적용
            int expected = type == BattleType.Elite ? 42 : 40; // 정예인 경우에만 원본 피해의 5퍼센트 증가
            Assert.AreEqual(expected, preview.RawDamage); // 방어 이전에 적용된 예상 원본 피해
            Assert.AreEqual(expected, actual.RawDamage); // 실제 방어 이전에 같은 증가
            Assert.AreEqual(preview.AppliedDamage, actual.AppliedDamage); // 기존 방어도를 포함한 예고와 실제 체력 일치
            Assert.AreEqual(expected - 5, actual.AppliedDamage); // 피해 증가 후 기존 방어도만 흡수
            Assert.AreEqual(type == BattleType.Elite ? 21 : 20, ally.TakeDamage(20, BattleDamageType.None).AppliedDamage); // 조우의 일반 피해도 동일한 범위
            Assert.AreEqual(type == BattleType.Elite ? 21 : 20, ally.TakeUnblockedDamage(20, BattleDamageType.None, fight.Enemy)); // 실제 방어 우회 피해에 한 번 증가
            int before = ally.CurrentHealth; // 카드로 지불할 순수 체력의 원래 수치
            Assert.IsTrue(ally.TrySpendHealth(20)); // 정예 사양과 별개인 원래 비용 지불
            Assert.AreEqual(20, before - ally.CurrentHealth); // 체력을 지불하는 비용은 피해 증가 제외
            if (type == BattleType.Elite) // 비밀장부가 발동한 원본 피해의 실제 분담 확인
            { // 새 공격 없이 이동하는 이미 조정된 피해 확인 시작
                int shared = ally.TakeUnblockedDamage(20, BattleDamageType.None, fight.Enemy, fromEnemyDirectAttack: true, sharesResolvedDamage: true); // 실제 받은 피해 조각과 같은 정산 경로
                Assert.AreEqual(20, shared); // 최초 피해에서 반영된 보정의 중복 적용 방지
                fight.Turn.Dispose(); // 실제 전투 무효화와 이전 유물 연결 제거
                Assert.AreEqual(20, ally.PreviewDamage(20, BattleDamageType.None).RawDamage); // 해제된 조우의 유물 피해 증가 배제
            } // 새 공격 없이 이동하는 이미 조정된 피해 확인 종료
        } // 개별 전투의 동일 타격 비교 종료
    } // 정예 등급별 비교 종료

    private sealed class OwnedRelic : IDisposable // 실제 보유 API를 쓰는 검사 유물 관리
    { // 검사 유물 수명 시작
        private readonly RelicRunManager inventory; // 실제 탐사 유물 보유 관리자
        private readonly RelicData relic; // 출시 폴더의 구체적인 유물 데이터

        internal OwnedRelic(string name) // 검사에서 필요한 실제 보유 추가
        { // 원본 조회와 실제 보유 시작
            inventory = RelicRunManager.EnsureInstance(); // 동일한 실제 게임 보유 경로
            relic = Resources.Load<RelicData>("Relics/" + name); // 출시되는 원본 유물 리소스
            Assert.IsNotNull(relic); // 유물 데이터가 실제으로 로드되는 조건
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic)); // 실제 보유에서 효과 검색 가능 확인
        } // 원본 조회와 실제 보유 종료

        public void Dispose() => inventory.TryRemove(relic.RelicId); // 이번 검사에서 획득한 유물 되돌림
    } // 검사 유물 수명 종료

    private sealed class Trial : IDisposable // 자체 참가자와 실제 조우 등급의 테스트 고정물
    { // 현재 검사용 전투의 실제 구성 시작
        private readonly CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 검사에서만 쓸 생존 아군 원본
        private readonly CharacterData otherCharacter = ScriptableObject.CreateInstance<CharacterData>(); // 덱의 소유자 검색에서 첫 아군과 구분할 독립 원본
        private readonly EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 실제 같은 방어력이 있는 적 원본
        private readonly CardData card = ScriptableObject.CreateInstance<CardData>(); // 실제 전투 전개에 필요한 덱 원본
        private readonly DeckData deckData; // 생성한 뒤 삭제할 실제 임시 덱
        internal BattleUnitRuntime Ally { get; } // 피해를 맞는 첫 실제 아군
        internal BattleUnitRuntime Other { get; } // 화상을 맞는 둘째 실제 아군
        internal BattleUnitRuntime Enemy { get; } // 조우에 등록하는 실제 적
        internal BattleTurnRuntime Turn { get; } // 등록과 해제에 사용하는 실제 전투 관리자

        internal Trial(BattleType type) // 실제 등급과 신규 전투 참가자 구성
        { // 실제 참가자의 독립 상태 준비 시작
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", character); // 비교에 불필요한 기본 저항과 충분한 체력 지정
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", otherCharacter); // 동일 수치를 가진 두 번째 별도 캐릭터 원본의 실제 체력
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":200,\"physicalDefense\":0,\"magicalResistance\":0}", enemyData); // 양호한 생존 범위와 같은 적 방어력
            Ally = BattleUnitRuntime.CreateAlly(character); // 첫 실제 아군 구성
            Other = BattleUnitRuntime.CreateAlly(otherCharacter); // 고유 소유자 키를 가진 다른 상태의 독립 아군
            Enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 실제 적과 현재 조우 등급
            var allies = new[] { Ally, Other }; // 실제 전투의 아군 목록
            deckData = DeckData.CreateRuntime(new[] { new DeckCardEntry(card, character) }); // 일반 소유자와 덱 한 장의 원본 연결
            var deck = BattleDeckRuntime.Create(deckData, allies, 3, 274); // 기존 전투 자료와 실제 덱 구성
            Turn = new BattleTurnRuntime(deck, new BattleActionPointRuntime(3), allies, new[] { Enemy }, 0, type); // 이전 승리와 연결 없이 실제 등급 등록
            Assert.IsTrue(Turn.StartBattle(0)); // 실제 전투의 원본 유물과 초기 상태 적용
        } // 실제 참가자의 독립 상태 준비 종료

        public void Dispose() // 실제 전투 연결과 임시 원본 제거
        { // 이 검사에서 사용한 자료 정리 시작
            Turn.Dispose(); // 현재 전투 구독과 유물 대상 연결 해제
            Object.Destroy(deckData); // 원본 검사 카드를 참조한 임시 덱 제거
            Object.Destroy(card); // 현재 검사에서 만든 임시 카드 제거
            Object.Destroy(character); // 실제 검사에 필요한 임시 아군 데이터 제거
            Object.Destroy(otherCharacter); // 독립 두 번째 아군의 임시 원본 제거
            Object.Destroy(enemyData); // 실제 검사에 필요한 임시 적 데이터 제거
        } // 이 검사에서 사용한 자료 정리 종료
    } // 현재 검사용 전투의 실제 구성 종료
} // 출혈과 화상 및 정예 조우 검사 종료
